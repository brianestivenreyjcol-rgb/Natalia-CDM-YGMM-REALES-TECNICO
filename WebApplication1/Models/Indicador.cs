namespace WebApplication1.Models;

public enum Indicador { Atendidas, Tmo, NoSolucion, Rell24, Rell72, Transferencia }

public enum TipoValor { Entero, Segundos, Porcentaje }

/// <summary>Cómo se lee la variación frente al periodo anterior.</summary>
public enum Sentido { Neutro, MasEsMejor, MenosEsMejor }

/// <summary>Nombres, formato y fórmula de cada indicador, en un solo sitio.</summary>
public static class Indicadores
{
    public static string Nombre(this Indicador indicador) => indicador switch
    {
        Indicador.Atendidas => "Atendidas",
        Indicador.Tmo => "TMO (s)",
        Indicador.NoSolucion => "% No solución",
        Indicador.Rell24 => "% Rell 24H",
        Indicador.Rell72 => "% Rell 72H",
        Indicador.Transferencia => "% Transferencia",
        _ => indicador.ToString(),
    };

    /// <summary>Nombre sin «%» para títulos de página y gráficas.</summary>
    public static string NombreCorto(this Indicador indicador) => indicador switch
    {
        Indicador.NoSolucion => "No solución",
        Indicador.Rell24 => "Rellamada 24H",
        Indicador.Rell72 => "Rellamada 72H",
        Indicador.Tmo => "TMO",
        _ => indicador.Nombre(),
    };

    public static TipoValor Tipo(this Indicador indicador) => indicador switch
    {
        Indicador.Atendidas => TipoValor.Entero,
        Indicador.Tmo => TipoValor.Segundos,
        _ => TipoValor.Porcentaje,
    };

    public static Sentido Sentido(this Indicador indicador) =>
        indicador == Indicador.Atendidas ? Models.Sentido.Neutro : Models.Sentido.MenosEsMejor;

    /// <summary>Qué mide, en pocas palabras, para la nota gris bajo el título de las tarjetas.</summary>
    public static string NotaCorta(this Indicador indicador) => indicador switch
    {
        Indicador.Atendidas => "llamadas entrantes atendidas",
        Indicador.Tmo => "segundos de AHT + ACW por atendida",
        Indicador.NoSolucion => "sobre las llamadas con el dato informado",
        Indicador.Rell24 => "rellamadas en 24 h sobre las atendidas",
        Indicador.Rell72 => "rellamadas en 72 h sobre las entrantes no transferidas",
        Indicador.Transferencia => "transferidas sobre las atendidas",
        _ => "",
    };

    public static string Formula(this Indicador indicador) => indicador switch
    {
        Indicador.Atendidas => "Llamadas entrantes atendidas (ATN).",
        Indicador.Tmo => "Segundos de atención (AHT de las entrantes + ACW de las salientes) entre las atendidas.",
        Indicador.NoSolucion => "Llamadas con no_solucion_kpi = 1 entre las llamadas con no_solucion_kpi informado.",
        Indicador.Rell24 => "Llamadas con rellamada al servicio en 24 h (redial_attended_service_24h_kpi) entre las atendidas.",
        Indicador.Rell72 => "Entrantes no transferidas con rellamada a operaciones en 72 h (Rell_72H) entre las entrantes no transferidas (Q_rell).",
        Indicador.Transferencia => "Atendidas transferidas entre las atendidas.",
        _ => "",
    };
}
