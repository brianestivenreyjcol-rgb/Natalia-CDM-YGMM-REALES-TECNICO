using System.Globalization;
using WebApplication1.Models;

namespace WebApplication1.Infraestructura;

/// <summary>
/// Cifras y fechas en es-ES: dos decimales, miles con punto y «%» separado con un espacio fino
/// que no se parte (U+202F): «25,41 %», «91.133».
/// </summary>
public static class Formato
{
    public static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-ES");
    private const string EspacioFino = " ";
    private const string SinDato = "–";

    public static string Porcentaje(double? valor, int decimales = 2) =>
        valor is { } v ? v.ToString("N" + decimales, Es) + EspacioFino + "%" : SinDato;

    public static string Entero(double? valor) => valor is { } v ? v.ToString("N0", Es) : SinDato;

    public static string Decimal(double? valor, int decimales = 2) => valor is { } v ? v.ToString("N" + decimales, Es) : SinDato;

    public static string Valor(double? valor, TipoValor tipo) => tipo switch
    {
        TipoValor.Porcentaje => Porcentaje(valor),
        TipoValor.Segundos => Decimal(valor),
        _ => Entero(valor),
    };

    /// <summary>Cifra corta para gráficas con muchas columnas («27,8 %», «735»); la completa va en la ficha.</summary>
    public static string ValorCorto(double? valor, TipoValor tipo) => tipo switch
    {
        TipoValor.Porcentaje => Porcentaje(valor, 1),
        _ => Entero(valor),
    };

    /// <summary>Marca del eje Y: sin decimales salvo que el paso sea menor que 1.</summary>
    public static string Eje(double valor, TipoValor tipo, double paso)
    {
        var decimales = paso < 1 ? 1 : 0;
        return tipo == TipoValor.Porcentaje ? Porcentaje(valor, decimales) : valor.ToString("N" + decimales, Es);
    }

    /// <summary>Variación relativa frente al periodo anterior: «▼ 4,4 %» (la flecha dice si sube o baja).</summary>
    public static string Variacion(double? variacion)
    {
        if (variacion is not { } v) return SinDato;
        var flecha = v > 0.05 ? "▲" : v < -0.05 ? "▼" : "=";
        return $"{flecha} {Math.Abs(v).ToString("N1", Es)}{EspacioFino}%";
    }

    /// <summary>Clase de color de la variación (como en el portal): mejor (verde), peor (rojo) o neutro (gris).</summary>
    public static string ClaseVariacion(double? variacion, Sentido sentido)
    {
        if (variacion is not { } v || Math.Abs(v) < 0.05 || sentido == Sentido.Neutro) return "neutro";
        var sube = v > 0;
        return (sentido == Sentido.MasEsMejor) == sube ? "mejor" : "peor";
    }

    /// <summary>Coordenada de gráfico (SVG o CSS) con punto decimal, sin depender de la cultura.</summary>
    public static string Coord(double valor) => Math.Round(valor, 3).ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>Valor crudo para data-valor (ordenar tablas en el navegador).</summary>
    public static string? Crudo(double? valor) => valor?.ToString("R", CultureInfo.InvariantCulture);

    public static string Iso(DateOnly fecha) => fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>«1 sept 2026».</summary>
    public static string Fecha(DateOnly fecha) => $"{fecha.Day} {Mes(fecha.Month)} {fecha.Year}";

    /// <summary>«1 sept – 30 sept 2026» o «28 ago 2026 – 3 sept 2026».</summary>
    public static string Rango(DateOnly desde, DateOnly hasta) =>
        desde.Year == hasta.Year
            ? $"{desde.Day} {Mes(desde.Month)} – {Fecha(hasta)}"
            : $"{Fecha(desde)} – {Fecha(hasta)}";

    /// <summary>Con qué se compara, en corto, para la tira de indicadores: «vs ago», «vs jul–ago», «vs 1–7 sept».</summary>
    public static string ComparacionCorta(DateOnly desde, DateOnly hasta)
    {
        var mesesEnteros = desde.Day == 1 && hasta.AddDays(1).Day == 1;
        var mismoMes = desde.Year == hasta.Year && desde.Month == hasta.Month;
        if (mesesEnteros) return mismoMes ? $"vs {Mes(desde.Month)}" : $"vs {Mes(desde.Month)}–{Mes(hasta.Month)}";
        return mismoMes
            ? $"vs {desde.Day}–{hasta.Day} {Mes(hasta.Month)}"
            : $"vs {desde.Day} {Mes(desde.Month)}–{hasta.Day} {Mes(hasta.Month)}";
    }

    /// <summary>«01/09/2026».</summary>
    public static string FechaCorta(DateOnly fecha) => fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    /// <summary>«lun».</summary>
    public static string DiaSemana(DateOnly fecha) => Es.DateTimeFormat.GetAbbreviatedDayName(fecha.DayOfWeek).TrimEnd('.');

    /// <summary>«lun 1 sept 2026».</summary>
    public static string FechaLarga(DateOnly fecha) => $"{DiaSemana(fecha)} {Fecha(fecha)}";

    /// <summary>«7 oct 2026, 20:41:02».</summary>
    public static string FechaHora(DateTime momento) =>
        $"{Fecha(DateOnly.FromDateTime(momento))}, {momento.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}";

    private static string Mes(int mes) => Es.DateTimeFormat.GetAbbreviatedMonthName(mes).TrimEnd('.');
}
