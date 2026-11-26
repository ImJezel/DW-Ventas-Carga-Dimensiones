using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace VentasDW.Carga.Infraestructura;

/// <summary>Lee un CSV completo a una lista tipada con CsvHelper.</summary>
public static class LectorCsv
{
    public static async Task<List<T>> LeerAsync<T>(string rutaRelativa, CancellationToken ct)
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, rutaRelativa);
        if (!File.Exists(ruta)) throw new FileNotFoundException("No se encontró el archivo de origen", ruta);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            HeaderValidated = null
        };
        using var lector = new StreamReader(ruta, System.Text.Encoding.UTF8);
        using var csv = new CsvReader(lector, config);
        var filas = new List<T>();
        await foreach (var fila in csv.GetRecordsAsync<T>(ct)) filas.Add(fila);
        return filas;
    }
}
