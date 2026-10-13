using System.Globalization;

namespace WebApplication1.Infraestructura;

/// <summary>Celdas de CSV para Excel en español: separador «;», coma decimal y fechas dd/MM/yyyy.</summary>
public static class Csv
{
    public const char Separador = ';';

    /// <summary>Texto entre comillas si lleva separador, comillas o saltos de línea.</summary>
    public static string Texto(string texto) =>
        texto.IndexOfAny([Separador, '"', '\n', '\r']) >= 0 ? $"\"{texto.Replace("\"", "\"\"")}\"" : texto;

    /// <summary>Cualquier valor que devuelva la base de datos, escrito como lo espera Excel.</summary>
    public static string Celda(object? valor) => valor switch
    {
        null or DBNull => "",
        string s => Texto(s),
        bool b => b ? "1" : "0",
        double d => d.ToString("0.###", Formato.Es),
        float f => f.ToString("0.###", Formato.Es),
        decimal m => m.ToString("0.###", Formato.Es),
        DateTime t => t.TimeOfDay == TimeSpan.Zero
            ? t.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
            : t.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
        DateTimeOffset t => t.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
        IFormattable numero => numero.ToString(null, CultureInfo.InvariantCulture),
        _ => Texto(Convert.ToString(valor, CultureInfo.InvariantCulture) ?? ""),
    };
}
