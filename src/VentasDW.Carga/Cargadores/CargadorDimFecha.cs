using System.Data;
using System.Globalization;
using Microsoft.Extensions.Options;
using VentasDW.Carga.Infraestructura;
using VentasDW.Carga.Modelos;

namespace VentasDW.Carga.Cargadores;

/// <summary>Genera el calendario entre FechaInicio y FechaFin; no depende de ningún archivo.</summary>
public sealed class CargadorDimFecha(BaseDatos bd, IOptions<OpcionesCarga> op) : ICargadorDimension
{
    private static readonly CultureInfo Es = new("es-DO");
    public string Dimension => "DimFecha";
    public int Orden => 1;

    public async Task<ResultadoCarga> CargarAsync(CancellationToken ct)
    {
        var t = new DataTable();
        foreach (var (n, tipo) in new[] { ("FechaKey", typeof(int)), ("Fecha", typeof(DateTime)), ("Anio", typeof(short)), ("Trimestre", typeof(byte)),
                                          ("Mes", typeof(byte)), ("NombreMes", typeof(string)), ("Dia", typeof(byte)), ("DiaSemana", typeof(byte)),
                                          ("NombreDia", typeof(string)), ("EsFinDeSemana", typeof(bool)) })
            t.Columns.Add(n, tipo);

        for (var d = op.Value.FechaInicio.Date; d <= op.Value.FechaFin.Date; d = d.AddDays(1))
        {
            var diaSemana = d.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)d.DayOfWeek; // 1 = lunes
            t.Rows.Add(d.Year * 10000 + d.Month * 100 + d.Day, d, (short)d.Year, (byte)((d.Month - 1) / 3 + 1), (byte)d.Month,
                Es.TextInfo.ToTitleCase(d.ToString("MMMM", Es)), (byte)d.Day, (byte)diaSemana,
                Es.TextInfo.ToTitleCase(d.ToString("dddd", Es)), diaSemana >= 6);
        }

        await using var cn = await bd.AbrirAsync(ct);
        await using var tx = (Microsoft.Data.SqlClient.SqlTransaction)await cn.BeginTransactionAsync(ct);
        await BaseDatos.CargarStagingAsync(cn, tx, "stg.Fecha", t, op.Value.TamanoLote, ct);
        var (ins, act) = await BaseDatos.EjecutarMergeAsync(cn, tx, """
            MERGE dw.DimFecha AS d
            USING stg.Fecha AS s ON d.FechaKey = s.FechaKey
            WHEN NOT MATCHED BY TARGET THEN
                INSERT (FechaKey, Fecha, Anio, Trimestre, Mes, NombreMes, Dia, DiaSemana, NombreDia, EsFinDeSemana)
                VALUES (s.FechaKey, s.Fecha, s.Anio, s.Trimestre, s.Mes, s.NombreMes, s.Dia, s.DiaSemana, s.NombreDia, s.EsFinDeSemana)
            OUTPUT $action INTO @acciones;
            """, ct);
        await tx.CommitAsync(ct);
        return new ResultadoCarga(Dimension, t.Rows.Count, ins, act, 0);
    }
}
