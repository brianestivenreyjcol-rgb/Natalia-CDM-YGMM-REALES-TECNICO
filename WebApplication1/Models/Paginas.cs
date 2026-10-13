namespace WebApplication1.Models;

public enum Pagina { Inicio, Dashboard, Kpi, NoSolucion, Rell24, Rell72 }

/// <summary>Pestañas del informe, en el orden de la barra.</summary>
public static class Navegacion
{
    public sealed record Enlace(Pagina Pagina, string Texto, string Ruta);

    public static readonly IReadOnlyList<Enlace> Enlaces =
    [
        new(Pagina.Dashboard, "Dashboard", "/dashboard"),
        new(Pagina.Kpi, "KPIs integrados", "/kpi"),
        new(Pagina.NoSolucion, "No solución", "/no-solucion"),
        new(Pagina.Rell24, "Rellamada 24H", "/rellamada-24h"),
        new(Pagina.Rell72, "Rellamada 72H", "/rellamada-72h"),
    ];
}

public sealed record OpcionFiltro(string Valor, string Texto, bool Marcada);

public sealed record Desplegable(string Campo, string Etiqueta, IReadOnlyList<OpcionFiltro> Opciones)
{
    public int Marcadas => Opciones.Count(o => o.Marcada);

    public string Resumen => Marcadas switch
    {
        0 => "Todos",
        1 => Opciones.First(o => o.Marcada).Texto,
        var n => $"{n} seleccionados",
    };
}

/// <summary>Atajo de fechas del filtro (mes en curso, mes anterior…).</summary>
public sealed record Atajo(string Texto, string Url, bool Activo);

/// <summary>
/// Un dato de la tira de indicadores: la cifra, su variación frente al periodo anterior
/// («vs ago ▼ 4,4 %») y si es el indicador principal de la página (va destacado).
/// </summary>
public sealed record TarjetaIndicador(
    Indicador Indicador,
    double? Valor,
    double? Variacion,
    string ComparacionCorta,
    string Comparacion,
    bool Destacada);

/// <summary>Cabecera común: título, miga actual y pestaña activa (con los filtros para los enlaces).</summary>
public sealed record Cromo(string Titulo, string Miga, Pagina Pagina, string Consulta)
{
    public string Enlace(string ruta) => ruta + Consulta;
}

/// <summary>Lo común a todas las páginas del informe: cabecera, filtros e indicadores.</summary>
public sealed class MarcoPagina
{
    public required Pagina Pagina { get; init; }
    public required string Titulo { get; init; }

    /// <summary>Texto de la miga actual y de la pestaña («Dashboard», «KPIs»…).</summary>
    public required string Miga { get; init; }

    /// <summary>Ruta de esta página (/no-solucion…), destino del formulario de filtros.</summary>
    public required string Ruta { get; init; }

    /// <summary>Filtros actuales como cadena de consulta («?site=Bogotá…» o vacía).</summary>
    public required string Consulta { get; init; }

    public required DateOnly Desde { get; init; }
    public required DateOnly Hasta { get; init; }
    public required DateOnly DesdeDefecto { get; init; }
    public required DateOnly HastaDefecto { get; init; }
    public required DateOnly PrimerDia { get; init; }
    public required DateOnly UltimoDia { get; init; }

    public required IReadOnlyList<Desplegable> Desplegables { get; init; }
    public required IReadOnlyList<Atajo> Atajos { get; init; }
    public required IReadOnlyList<TarjetaIndicador> Indicadores { get; init; }
    public required DateTime Actualizado { get; init; }

    /// <summary>Enlace a otra página conservando los filtros.</summary>
    public string Enlace(string ruta) => ruta + Consulta;

    public Cromo Cromo => new(Titulo, Miga, Pagina, Consulta);

    /// <summary>Cuántos filtros hay puestos (para el raíl del panel plegado): valores marcados y fechas cambiadas.</summary>
    public int FiltrosActivos =>
        Desplegables.Sum(d => d.Marcadas) + (Desde != DesdeDefecto || Hasta != HastaDefecto ? 1 : 0);
}

public sealed class ModeloDashboard
{
    public required MarcoPagina Marco { get; init; }
    public required GraficoLineas NoSolucionSemana { get; init; }
    public required GraficoColumnas NoSolucionMarca { get; init; }
    public required GraficoBarras NoSolucionServicio { get; init; }
    public required GraficoLineas Rell72Semana { get; init; }
    public required GraficoColumnas Rell72Marca { get; init; }
    public required GraficoBarras Rell72Servicio { get; init; }
    public required GraficoLineas TmoSemana { get; init; }
    public required GraficoLineas AtendidasDia { get; init; }
}

public sealed record FilaDia(DateOnly Fecha, Acumulado Datos);

public sealed class ModeloKpi
{
    public required MarcoPagina Marco { get; init; }
    public required GraficoColumnas AtendidasMarca { get; init; }
    public required GraficoColumnas TmoMarca { get; init; }
    public required GraficoBarras AtendidasSupervisor { get; init; }
    public required GraficoLineas Evolucion { get; init; }
    public required IReadOnlyList<FilaDia> Dias { get; init; }
    public required Acumulado Total { get; init; }
}

/// <summary>Páginas No solución, Rellamada 24H y Rellamada 72H: mismas gráficas, otro indicador.</summary>
public sealed class ModeloIndicador
{
    public required MarcoPagina Marco { get; init; }
    public required Indicador Indicador { get; init; }
    public required GraficoColumnas PorMarca { get; init; }
    public required GraficoBarras PorServicio { get; init; }
    public required GraficoBarras PorDetallado { get; init; }
    public required GraficoLineas PorSemana { get; init; }
    public required GraficoLineas PorDia { get; init; }
}

public sealed class ModeloPortada
{
    public DateTime? Actualizado { get; init; }
    public DateOnly? UltimoDia { get; init; }
    public bool Cargando { get; init; }
    public string? Error { get; init; }
}

/// <summary>Página de estado: datos cargados y prueba de la conexión a BigQuery.</summary>
public sealed class ModeloEstado
{
    public required string ConexionBigQuery { get; init; }
    public required bool Cargando { get; init; }
    public DateTime? InicioCarga { get; init; }
    public DateTime? UltimoIntento { get; init; }
    public string? UltimoError { get; init; }
    public DateTime? Actualizado { get; init; }
    public int Filas { get; init; }
    public DateOnly? PrimerDia { get; init; }
    public DateOnly? UltimoDia { get; init; }
    public TimeSpan? Duracion { get; init; }
    public int DiasHistorico { get; init; }
    public int MinutosActualizacion { get; init; }
    public (bool Correcta, string Mensaje, long Milisegundos)? PruebaBigQuery { get; init; }
}

/// <summary>Se enseña mientras llega la primera carga de BigQuery o si falló.</summary>
public sealed class ModeloPreparando
{
    public bool Cargando { get; init; }
    public string? Error { get; init; }
    public DateTime? InicioCarga { get; init; }
    public string Volver { get; init; } = "/";
}
