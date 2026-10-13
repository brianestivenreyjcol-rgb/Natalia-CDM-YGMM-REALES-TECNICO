using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebApplication1.Models;
using WebApplication1.Servicios;

namespace WebApplication1.Controllers;

/// <summary>Estado de los datos y prueba de la conexión a BigQuery (ODBC).</summary>
public sealed class EstadoController(
    AlmacenCuadro almacen,
    ServicioBigQuery bigQuery,
    IOptions<OpcionesCuadro> opciones) : Controller
{
    [HttpGet("/estado")]
    public async Task<IActionResult> Index(bool probar = false, CancellationToken ct = default)
    {
        var prueba = probar ? await bigQuery.ProbarAsync(ct) : null;
        var datos = almacen.Actual;
        return View(new ModeloEstado
        {
            ConexionBigQuery = bigQuery.Descripcion,
            Cargando = almacen.Cargando,
            InicioCarga = almacen.InicioCarga,
            UltimoIntento = almacen.UltimoIntento,
            UltimoError = almacen.UltimoError,
            Actualizado = datos?.Actualizado,
            Filas = datos?.Filas.Count ?? 0,
            PrimerDia = datos?.PrimerDia,
            UltimoDia = datos?.UltimoDia,
            Duracion = datos?.Duracion,
            DiasHistorico = opciones.Value.DiasHistorico,
            MinutosActualizacion = opciones.Value.MinutosActualizacion,
            PruebaBigQuery = prueba is null ? null : (prueba.Correcta, prueba.Mensaje, prueba.Milisegundos),
        });
    }

    /// <summary>Vuelve a traer los datos de BigQuery ahora, sin esperar a la próxima vuelta.</summary>
    [HttpPost("/estado/actualizar")]
    [ValidateAntiForgeryToken]
    public IActionResult Actualizar(string? volver)
    {
        almacen.PedirActualizacion();
        return LocalRedirect(Url.IsLocalUrl(volver) ? volver! : "/estado");
    }
}
