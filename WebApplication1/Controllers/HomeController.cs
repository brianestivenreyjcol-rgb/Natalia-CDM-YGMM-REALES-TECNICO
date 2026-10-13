using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Servicios;

namespace WebApplication1.Controllers;

public class HomeController(AlmacenCuadro almacen) : Controller
{
    /// <summary>Portada del cuadro de mando, con los cinco informes (también en /cuadro).</summary>
    public IActionResult Index() => View("Cuadro", Portada());

    [HttpGet("/cuadro")]
    public IActionResult Cuadro() => View(Portada());

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    private ModeloPortada Portada() => new()
    {
        Actualizado = almacen.Actual?.Actualizado,
        UltimoDia = almacen.Actual?.UltimoDia,
        Cargando = almacen.Cargando,
        Error = almacen.UltimoError,
    };
}
