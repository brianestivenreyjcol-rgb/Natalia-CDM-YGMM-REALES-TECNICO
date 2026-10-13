namespace WebApplication1.Models;

/// <summary>Una serie de una gráfica: su nombre, la clase que fija su color (--serie) y un valor por categoría.</summary>
public sealed record SerieGrafico(string Nombre, string Clase, IReadOnlyList<double?> Valores);

public abstract class Grafico
{
    /// <summary>Identificador único en la página (lo usan los degradados del SVG).</summary>
    public required string Id { get; init; }
    public required string Titulo { get; init; }
    public required TipoValor Tipo { get; init; }

    /// <summary>Etiquetas del eje de categorías (semanas, días, sites, servicios…).</summary>
    public required IReadOnlyList<string> Categorias { get; init; }
    public required IReadOnlyList<SerieGrafico> Series { get; init; }

    /// <summary>Cabecera de la ficha al pasar el ratón; si no hay, la de la categoría.</summary>
    public IReadOnlyList<string>? Fichas { get; init; }

    /// <summary>Clase de la tarjeta para colocarla en la rejilla de la página.</summary>
    public string? Area { get; init; }

    /// <summary>Nota en gris bajo el título de la tarjeta (qué se mide y cómo).</summary>
    public string? Nota { get; init; }

    /// <summary>Frase de lectura al pie de la tarjeta («Medellín va 2,71 puntos por encima…»).</summary>
    public string? Lectura { get; set; }

    public bool Vacio => Categorias.Count == 0 || Series.All(s => s.Valores.All(v => v is null));
}

/// <summary>
/// Líneas suavizadas con degradado y el valor de cada punto en pastilla. Si lleva volumen, las
/// atendidas de cada periodo van como columnas grises detrás, en el mismo plano (patrón del portal).
/// </summary>
public sealed class GraficoLineas : Grafico
{
    /// <summary>El eje Y empieza en 0 (volúmenes); si no, se ajusta al rango de los datos.</summary>
    public bool DesdeCero { get; init; }
    public bool Alto { get; init; }

    /// <summary>Atendidas de cada periodo (columnas grises detrás), o null si la gráfica no lleva volumen.</summary>
    public IReadOnlyList<double?>? Volumen { get; init; }
}

/// <summary>Columnas agrupadas: una columna por serie en cada categoría.</summary>
public sealed class GraficoColumnas : Grafico;

/// <summary>Barras horizontales agrupadas, con la cifra al final de cada barra.</summary>
public sealed class GraficoBarras : Grafico
{
    public bool Gruesas { get; init; }
}
