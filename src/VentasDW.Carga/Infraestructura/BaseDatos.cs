using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using VentasDW.Carga.Modelos;

namespace VentasDW.Carga.Infraestructura;

/// <summary>Acceso al data warehouse: vaciado de staging, inserción masiva y MERGE.</summary>
public sealed class BaseDatos(IConfiguration config)
{
    private readonly string _cadena = config.GetConnectionString("DataWarehouse")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:DataWarehouse");

    public async Task<SqlConnection> AbrirAsync(CancellationToken ct)
    {
        var cn = new SqlConnection(_cadena);
        await cn.OpenAsync(ct);
        return cn;
    }

    /// <summary>Vacía la tabla de staging y la llena con SqlBulkCopy.</summary>
    public static async Task CargarStagingAsync(SqlConnection cn, SqlTransaction tx, string tabla, DataTable datos, int lote, CancellationToken ct)
    {
        await using (var truncar = new SqlCommand($"TRUNCATE TABLE {tabla};", cn, tx))
            await truncar.ExecuteNonQueryAsync(ct);

        using var bulk = new SqlBulkCopy(cn, SqlBulkCopyOptions.Default, tx) { DestinationTableName = tabla, BatchSize = lote };
        foreach (DataColumn c in datos.Columns) bulk.ColumnMappings.Add(c.ColumnName, c.ColumnName);
        await bulk.WriteToServerAsync(datos, ct);
    }

    /// <summary>
    /// Ejecuta un MERGE que termina con OUTPUT $action INTO @acciones y
    /// devuelve cuántas filas se insertaron y cuántas se actualizaron.
    /// </summary>
    public static async Task<(int Insertadas, int Actualizadas)> EjecutarMergeAsync(SqlConnection cn, SqlTransaction tx, string merge, CancellationToken ct)
    {
        var sql = $"""
            DECLARE @acciones TABLE (Accion NVARCHAR(10));
            {merge}
            SELECT SUM(CASE WHEN Accion = 'INSERT' THEN 1 ELSE 0 END),
                   SUM(CASE WHEN Accion = 'UPDATE' THEN 1 ELSE 0 END)
            FROM @acciones;
            """;
        await using var cmd = new SqlCommand(sql, cn, tx);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        await r.ReadAsync(ct);
        return (r.IsDBNull(0) ? 0 : r.GetInt32(0), r.IsDBNull(1) ? 0 : r.GetInt32(1));
    }

    public async Task RegistrarBitacoraAsync(ResultadoCarga res, DateTime inicio, string estado, string? mensaje, CancellationToken ct)
    {
        await using var cn = await AbrirAsync(ct);
        await using var cmd = new SqlCommand("""
            INSERT INTO dw.BitacoraCarga (Dimension, Leidas, Insertadas, Actualizadas, Descartadas, Inicio, Fin, Estado, Mensaje)
            VALUES (@d, @l, @i, @a, @x, @ini, SYSDATETIME(), @e, @m);
            """, cn);
        cmd.Parameters.AddWithValue("@d", res.Dimension);
        cmd.Parameters.AddWithValue("@l", res.Leidas);
        cmd.Parameters.AddWithValue("@i", res.Insertadas);
        cmd.Parameters.AddWithValue("@a", res.Actualizadas);
        cmd.Parameters.AddWithValue("@x", res.Descartadas);
        cmd.Parameters.AddWithValue("@ini", inicio);
        cmd.Parameters.AddWithValue("@e", estado);
        cmd.Parameters.AddWithValue("@m", (object?)mensaje ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
