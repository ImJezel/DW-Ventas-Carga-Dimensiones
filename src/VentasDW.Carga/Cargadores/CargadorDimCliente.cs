using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VentasDW.Carga.Infraestructura;
using VentasDW.Carga.Modelos;

namespace VentasDW.Carga.Cargadores;

/// <summary>
/// Carga DimCliente desde clients.csv: limpia nombre y correo, descarta filas inválidas
/// o duplicadas y aplica SCD tipo 1 (sobrescribe cambios) con MERGE.
/// </summary>
public sealed partial class CargadorDimCliente(BaseDatos bd, IOptions<OpcionesCarga> op, ILogger<CargadorDimCliente> log) : ICargadorDimension
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex CorreoValido();

    public string Dimension => "DimCliente";
    public int Orden => 2;

    public async Task<ResultadoCarga> CargarAsync(CancellationToken ct)
    {
        var filas = await LectorCsv.LeerAsync<ClienteCsv>(op.Value.ArchivoClientes, ct);
        var t = new DataTable();
        t.Columns.Add("IdCliente", typeof(int));
        t.Columns.Add("Nombre", typeof(string));
        t.Columns.Add("Email", typeof(string));
        t.Columns.Add("DominioEmail", typeof(string));

        var vistos = new HashSet<int>();
        var descartadas = 0;
        foreach (var f in filas)
        {
            var nombre = f.Nombre.Trim();
            if (f.IdCliente <= 0 || nombre.Length == 0 || !vistos.Add(f.IdCliente))
            {
                descartadas++;
                continue;
            }
            var correo = f.Email?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(correo) || !CorreoValido().IsMatch(correo)) correo = null;
            t.Rows.Add(f.IdCliente, nombre, (object?)correo ?? DBNull.Value,
                correo is null ? DBNull.Value : correo[(correo.IndexOf('@') + 1)..]);
        }
        if (descartadas > 0) log.LogWarning("DimCliente: {N} filas descartadas por id inválido, nombre vacío o duplicado", descartadas);

        await using var cn = await bd.AbrirAsync(ct);
        await using var tx = (SqlTransaction)await cn.BeginTransactionAsync(ct);
        await BaseDatos.CargarStagingAsync(cn, tx, "stg.Cliente", t, op.Value.TamanoLote, ct);
        var (ins, act) = await BaseDatos.EjecutarMergeAsync(cn, tx, """
            MERGE dw.DimCliente AS d
            USING stg.Cliente AS s ON d.IdClienteOrigen = s.IdCliente
            WHEN MATCHED AND (d.Nombre <> s.Nombre OR ISNULL(d.Email, '') <> ISNULL(s.Email, '')) THEN
                UPDATE SET d.Nombre = s.Nombre, d.Email = s.Email, d.DominioEmail = s.DominioEmail,
                           d.FechaActualizacion = SYSDATETIME()
            WHEN NOT MATCHED BY TARGET THEN
                INSERT (IdClienteOrigen, Nombre, Email, DominioEmail) VALUES (s.IdCliente, s.Nombre, s.Email, s.DominioEmail)
            OUTPUT $action INTO @acciones;
            """, ct);
        await tx.CommitAsync(ct);
        return new ResultadoCarga(Dimension, filas.Count, ins, act, descartadas);
    }
}
