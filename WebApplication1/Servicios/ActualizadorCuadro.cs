using Microsoft.Extensions.Options;

namespace WebApplication1.Servicios;

/// <summary>
/// Tarea en segundo plano: carga los datos de BigQuery al arrancar la web y los refresca cada
/// «MinutosActualizacion». Si una carga falla, lo vuelve a intentar al minuto si todavía no hay
/// datos, o a los 5 minutos si ya los había.
/// </summary>
public sealed class ActualizadorCuadro(AlmacenCuadro almacen, IOptions<OpcionesCuadro> opciones) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Yield();
        while (!ct.IsCancellationRequested)
        {
            // El controlador ODBC es síncrono: la carga va en un hilo aparte para no frenar el arranque
            await Task.Run(() => almacen.CargarAsync(ct), ct);

            var espera = almacen.UltimoError is null
                ? TimeSpan.FromMinutes(Math.Max(1, opciones.Value.MinutosActualizacion))
                : TimeSpan.FromMinutes(almacen.Actual is null ? 1 : 5);
            try
            {
                await almacen.EsperarAsync(espera, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
