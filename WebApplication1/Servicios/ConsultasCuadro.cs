using System.Globalization;

namespace WebApplication1.Servicios;

/// <summary>Lee las consultas de la carpeta Consultas (se copian junto a la web al compilar) y les pone sus datos.</summary>
public static class ConsultasCuadro
{
    private static string Leer(string nombre) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Consultas", nombre));

    private static string Detalle() => Leer("Detalle.sql").Trim().TrimEnd(';');

    private static string Iso(DateOnly fecha) => fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Resumen diario (Agregado.sql) sobre la consulta de detalle (Detalle.sql) desde la fecha dada.</summary>
    public static string ResumenDiario(DateOnly desde) =>
        Leer("Agregado.sql")
            .Replace("{{DETALLE}}", Detalle())
            .Replace("{{DESDE}}", Iso(desde));

    /// <summary>
    /// Detalle por conversación (Exportable.sql) entre dos fechas (la segunda no incluida).
    /// <paramref name="condiciones"/> son líneas «AND …» ya escapadas (las arma ServicioExportable).
    /// </summary>
    public static string Exportable(DateOnly desde, DateOnly hastaSinIncluir, string condiciones) =>
        Leer("Exportable.sql")
            .Replace("{{DETALLE}}", Detalle())
            .Replace("{{DESDE}}", Iso(desde))
            .Replace("{{HASTA}}", Iso(hastaSinIncluir))
            .Replace("{{FILTROS}}", condiciones);
}
