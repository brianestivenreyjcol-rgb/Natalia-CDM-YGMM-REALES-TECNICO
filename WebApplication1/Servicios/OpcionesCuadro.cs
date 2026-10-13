namespace WebApplication1.Servicios;

/// <summary>Sección «CuadroMando» de appsettings.json.</summary>
public sealed class OpcionesCuadro
{
    public const string Seccion = "CuadroMando";

    /// <summary>Días hacia atrás que se traen de BigQuery (incluye el periodo anterior para comparar).</summary>
    public int DiasHistorico { get; set; } = 100;

    /// <summary>Cada cuánto se vuelven a traer los datos.</summary>
    public int MinutosActualizacion { get; set; } = 60;

    /// <summary>Tiempo máximo de la consulta en BigQuery.</summary>
    public int SegundosEsperaConsulta { get; set; } = 600;
}

/// <summary>Resultado de probar una conexión desde la página de estado.</summary>
public sealed record PruebaConexion(bool Correcta, string Mensaje, long Milisegundos);
