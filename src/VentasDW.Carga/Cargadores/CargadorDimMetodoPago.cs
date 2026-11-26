using Microsoft.Data.SqlClient;
using VentasDW.Carga.Infraestructura;
using VentasDW.Carga.Modelos;

namespace VentasDW.Carga.Cargadores;

/// <summary>Dimensión estática: los métodos de pago permitidos en la base transaccional (CHECK de la tabla pagos).</summary>
public sealed class CargadorDimMetodoPago(BaseDatos bd) : ICargadorDimension
{
    public string Dimension => "DimMetodoPago";
    public int Orden => 5;

    public async Task<ResultadoCarga> CargarAsync(CancellationToken ct)
    {
        await using var cn = await bd.AbrirAsync(ct);
        await using var tx = (SqlTransaction)await cn.BeginTransactionAsync(ct);
        var (ins, act) = await BaseDatos.EjecutarMergeAsync(cn, tx, """
            MERGE dw.DimMetodoPago AS d
            USING (VALUES (1, 'EFECTIVO', 'Pago en efectivo'),
                          (2, 'TARJETA', 'Tarjeta de crédito o débito'),
                          (3, 'TRANSFERENCIA', 'Transferencia bancaria')) AS s (MetodoPagoKey, Codigo, Descripcion)
                ON d.MetodoPagoKey = s.MetodoPagoKey
            WHEN MATCHED AND d.Descripcion <> s.Descripcion THEN UPDATE SET d.Descripcion = s.Descripcion
            WHEN NOT MATCHED BY TARGET THEN INSERT (MetodoPagoKey, Codigo, Descripcion) VALUES (s.MetodoPagoKey, s.Codigo, s.Descripcion)
            OUTPUT $action INTO @acciones;
            """, ct);
        await tx.CommitAsync(ct);
        return new ResultadoCarga(Dimension, 3, ins, act, 0);
    }
}
