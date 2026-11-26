using VentasDW.Carga.Modelos;

namespace VentasDW.Carga.Cargadores;

/// <summary>Contrato común de cada dimensión. Orden define la secuencia (Categoría antes que Producto).</summary>
public interface ICargadorDimension
{
    string Dimension { get; }
    int Orden { get; }
    Task<ResultadoCarga> CargarAsync(CancellationToken ct);
}
