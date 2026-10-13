using Microsoft.AspNetCore.Mvc;
using WebApplication1.Infraestructura;
using WebApplication1.Models;
using WebApplication1.Servicios;

namespace WebApplication1.Controllers;

/// <summary>Páginas del cuadro de mando. Mientras no haya datos de BigQuery, enseña «Preparando los datos».</summary>
public sealed class CuadroController(ServicioTablero tablero, ServicioExportable exportable, AlmacenCuadro almacen) : Controller
{
    [HttpGet("/dashboard")]
    public IActionResult Dashboard([FromQuery] Filtros filtros) =>
        Pintar(tablero.Dashboard(filtros, Ruta), "Dashboard");

    [HttpGet("/kpi")]
    public IActionResult Kpi([FromQuery] Filtros filtros) =>
        Pintar(tablero.Kpi(filtros, Ruta), "Kpi");

    [HttpGet("/no-solucion")]
    public IActionResult NoSolucion([FromQuery] Filtros filtros) =>
        Pintar(tablero.PaginaIndicador(Indicador.NoSolucion, filtros, Ruta), "Indicador");

    [HttpGet("/rellamada-24h")]
    public IActionResult Rellamada24([FromQuery] Filtros filtros) =>
        Pintar(tablero.PaginaIndicador(Indicador.Rell24, filtros, Ruta), "Indicador");

    [HttpGet("/rellamada-72h")]
    public IActionResult Rellamada72([FromQuery] Filtros filtros) =>
        Pintar(tablero.PaginaIndicador(Indicador.Rell72, filtros, Ruta), "Indicador");

    /// <summary>Detalle por conversación de BigQuery con las fechas y filtros de la pantalla, en CSV para Excel.</summary>
    [HttpGet("/exportable")]
    public IActionResult Exportable([FromQuery] Filtros filtros)
    {
        if (tablero.Rango(filtros) is not { } rango) return Preparando();
        return new ResultadoDescarga(
            ServicioExportable.NombreArchivo(rango.Desde, rango.Hasta),
            "text/csv; charset=utf-8",
            (destino, ct) => exportable.EscribirCsvAsync(filtros, rango.Desde, rango.Hasta, destino, ct));
    }

    private string Ruta => Request.Path.Value ?? "/";

    private IActionResult Pintar(object? modelo, string vista) => modelo is null ? Preparando() : View(vista, modelo);

    private ViewResult Preparando() => View("Preparando", new ModeloPreparando
    {
        Cargando = almacen.Cargando || almacen.UltimoIntento is null,
        Error = almacen.UltimoError,
        InicioCarga = almacen.InicioCarga,
        Volver = Request.Path + Request.QueryString,
    });
}
