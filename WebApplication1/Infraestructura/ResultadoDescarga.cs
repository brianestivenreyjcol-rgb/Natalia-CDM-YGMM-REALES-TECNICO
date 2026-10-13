using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace WebApplication1.Infraestructura;

/// <summary>
/// Descarga que se va escribiendo en la respuesta a medida que llegan las filas, sin guardar el
/// archivo entero en memoria (el detalle de un mes son decenas de megas).
/// </summary>
public sealed class ResultadoDescarga(string nombreArchivo, string tipo, Func<Stream, CancellationToken, Task> escribir) : IActionResult
{
    public async Task ExecuteResultAsync(ActionContext contexto)
    {
        var respuesta = contexto.HttpContext.Response;
        respuesta.ContentType = tipo;
        var disposicion = new ContentDispositionHeaderValue("attachment");
        disposicion.SetHttpFileName(nombreArchivo);
        respuesta.Headers.ContentDisposition = disposicion.ToString();
        await escribir(respuesta.Body, contexto.HttpContext.RequestAborted);
    }
}
