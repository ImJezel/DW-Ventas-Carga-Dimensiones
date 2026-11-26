using System.Diagnostics;
using VentasDW.Carga.Cargadores;
using VentasDW.Carga.Infraestructura;
using VentasDW.Carga.Modelos;

namespace VentasDW.Carga;

/// <summary>Ejecuta los cargadores en orden, registra cada resultado en la bitácora y detiene el servicio al terminar.</summary>
public sealed class Worker(IEnumerable<ICargadorDimension> cargadores, BaseDatos bd, IHostApplicationLifetime vida, ILogger<Worker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var total = Stopwatch.StartNew();
        log.LogInformation("=== Carga de dimensiones DW_Ventas_Jezel ===");
        foreach (var c in cargadores.OrderBy(c => c.Orden))
        {
            var inicio = DateTime.Now;
            var reloj = Stopwatch.StartNew();
            try
            {
                var r = await c.CargarAsync(ct);
                await bd.RegistrarBitacoraAsync(r, inicio, "OK", null, ct);
                log.LogInformation("{Dim,-14} leídas {L,5} | insertadas {I,5} | actualizadas {A,4} | descartadas {X,3} | {Ms} ms",
                    r.Dimension, r.Leidas, r.Insertadas, r.Actualizadas, r.Descartadas, reloj.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Falló la carga de {Dim}", c.Dimension);
                await bd.RegistrarBitacoraAsync(new ResultadoCarga(c.Dimension, 0, 0, 0, 0), inicio, "ERROR", ex.Message[..Math.Min(400, ex.Message.Length)], ct);
                Environment.ExitCode = 1;
                break; // las dimensiones siguientes dependen de las anteriores
            }
        }
        log.LogInformation("Carga terminada en {Ms} ms", total.ElapsedMilliseconds);
        vida.StopApplication();
    }
}
