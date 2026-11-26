using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using VentasDW.Carga.Infraestructura;
using VentasDW.Carga.Modelos;

namespace VentasDW.Carga.Cargadores;

/// <summary>Carga primero DimCategoria (valores distintos) y luego DimProducto con su clave de categoría.</summary>
public sealed class CargadorDimCategoria(BaseDatos bd, IOptions<OpcionesCarga> op) : ICargadorDimension
{
    public string Dimension => "DimCategoria";
    public int Orden => 3;

    public async Task<ResultadoCarga> CargarAsync(CancellationToken ct)
    {
        var productos = await LectorCsv.LeerAsync<ProductoCsv>(op.Value.ArchivoProductos, ct);
        var categorias = productos.Select(p => p.Categoria.Trim()).Where(c => c.Length > 0)
                                  .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        await using var cn = await bd.AbrirAsync(ct);
        await using var tx = (SqlTransaction)await cn.BeginTransactionAsync(ct);
        var valores = string.Join(",", categorias.Select((_, i) => $"(@c{i})"));
        await using var cmd = new SqlCommand($"""
            DECLARE @acciones TABLE (Accion NVARCHAR(10));
            MERGE dw.DimCategoria AS d
            USING (VALUES {valores}) AS s (Nombre) ON d.Nombre = s.Nombre
            WHEN NOT MATCHED BY TARGET THEN INSERT (Nombre) VALUES (s.Nombre)
            OUTPUT $action INTO @acciones;
            SELECT COUNT(*) FROM @acciones;
            """, cn, tx);
        for (var i = 0; i < categorias.Count; i++) cmd.Parameters.AddWithValue($"@c{i}", categorias[i]);
        var insertadas = (int)(await cmd.ExecuteScalarAsync(ct))!;
        await tx.CommitAsync(ct);
        return new ResultadoCarga(Dimension, categorias.Count, insertadas, 0, 0);
    }
}

public sealed class CargadorDimProducto(BaseDatos bd, IOptions<OpcionesCarga> op) : ICargadorDimension
{
    public string Dimension => "DimProducto";
    public int Orden => 4;

    public async Task<ResultadoCarga> CargarAsync(CancellationToken ct)
    {
        var filas = await LectorCsv.LeerAsync<ProductoCsv>(op.Value.ArchivoProductos, ct);
        var t = new DataTable();
        t.Columns.Add("IdProducto", typeof(int));
        t.Columns.Add("Nombre", typeof(string));
        t.Columns.Add("Categoria", typeof(string));

        var vistos = new HashSet<int>();
        var descartadas = 0;
        foreach (var f in filas)
        {
            if (f.IdProducto <= 0 || string.IsNullOrWhiteSpace(f.Nombre) || !vistos.Add(f.IdProducto)) { descartadas++; continue; }
            t.Rows.Add(f.IdProducto, f.Nombre.Trim(), f.Categoria.Trim());
        }

        await using var cn = await bd.AbrirAsync(ct);
        await using var tx = (SqlTransaction)await cn.BeginTransactionAsync(ct);
        await BaseDatos.CargarStagingAsync(cn, tx, "stg.Producto", t, op.Value.TamanoLote, ct);
        // Lookup de la categoría; si no existe se asigna el miembro desconocido (-1).
        var (ins, act) = await BaseDatos.EjecutarMergeAsync(cn, tx, """
            MERGE dw.DimProducto AS d
            USING (
                SELECT s.IdProducto, s.Nombre, ISNULL(c.CategoriaKey, -1) AS CategoriaKey
                FROM stg.Producto s LEFT JOIN dw.DimCategoria c ON c.Nombre = s.Categoria
            ) AS s ON d.IdProductoOrigen = s.IdProducto
            WHEN MATCHED AND (d.Nombre <> s.Nombre OR d.CategoriaKey <> s.CategoriaKey) THEN
                UPDATE SET d.Nombre = s.Nombre, d.CategoriaKey = s.CategoriaKey, d.FechaActualizacion = SYSDATETIME()
            WHEN NOT MATCHED BY TARGET THEN
                INSERT (IdProductoOrigen, Nombre, CategoriaKey) VALUES (s.IdProducto, s.Nombre, s.CategoriaKey)
            OUTPUT $action INTO @acciones;
            """, ct);
        await tx.CommitAsync(ct);
        return new ResultadoCarga(Dimension, filas.Count, ins, act, descartadas);
    }
}
