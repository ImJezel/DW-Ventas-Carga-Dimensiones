using CsvHelper.Configuration.Attributes;

namespace VentasDW.Carga.Modelos;

/// <summary>Fila de clients.csv.</summary>
public sealed class ClienteCsv
{
    public int IdCliente { get; set; }
    public string Nombre { get; set; } = "";
    public string? Email { get; set; }
}

/// <summary>Fila de products.csv. La columna del archivo se llama "Categoría".</summary>
public sealed class ProductoCsv
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = "";
    [Name("Categoría")]
    public string Categoria { get; set; } = "";
}

/// <summary>Resultado de cargar una dimensión; se guarda en dw.BitacoraCarga.</summary>
public sealed record ResultadoCarga(string Dimension, int Leidas, int Insertadas, int Actualizadas, int Descartadas);

public sealed class OpcionesCarga
{
    public string ArchivoClientes { get; set; } = "Datos/clients.csv";
    public string ArchivoProductos { get; set; } = "Datos/products.csv";
    public DateTime FechaInicio { get; set; } = new(2024, 1, 1);
    public DateTime FechaFin { get; set; } = new(2026, 12, 31);
    public int TamanoLote { get; set; } = 5000;
}
