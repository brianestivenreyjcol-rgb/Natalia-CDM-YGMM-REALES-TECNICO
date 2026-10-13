namespace WebApplication1.Models;

/// <summary>
/// Filtros que llegan en la URL (?desde=2026-09-01&amp;hasta=2026-09-30&amp;site=Bogotá&amp;semana=36…).
/// Cada lista admite varios valores; vacía significa «Todos».
/// </summary>
public sealed class Filtros
{
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public List<string> Site { get; set; } = [];
    public List<string> Supervisor { get; set; } = [];
    public List<string> Sector { get; set; } = [];
    public List<string> Semana { get; set; } = [];
    public List<string> Agente { get; set; } = [];
}
