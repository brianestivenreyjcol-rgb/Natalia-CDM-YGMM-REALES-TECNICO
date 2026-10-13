using Microsoft.AspNetCore.Html;

namespace WebApplication1.Infraestructura;

/// <summary>Iconos de trazo (24 × 24, color del texto).</summary>
public static class Iconos
{
    private static readonly Dictionary<string, string> Trazos = new()
    {
        ["usuarios"] = """<circle cx="9" cy="8" r="3.2"/><path d="M3.5 19.5c.6-3.3 2.8-5.2 5.5-5.2s4.9 1.9 5.5 5.2"/><circle cx="16.6" cy="9" r="2.6"/><path d="M15.8 14.3c2.5-.2 4.3 1.5 4.8 4.4"/>""",
        ["reloj"] = """<circle cx="12" cy="12" r="8.5"/><path d="M12 7.5V12l3 2"/>""",
        ["telefono"] = """<path d="M5.2 4.5h3l1.5 3.9-2 1.3a11 11 0 0 0 6.6 6.6l1.3-2 3.9 1.5v3a1.5 1.5 0 0 1-1.6 1.5A16 16 0 0 1 3.7 6.1a1.5 1.5 0 0 1 1.5-1.6z"/>""",
        ["telefono-x"] = """<path d="M5.2 4.5h3l1.5 3.9-2 1.3a11 11 0 0 0 6.6 6.6l1.3-2 3.9 1.5v3a1.5 1.5 0 0 1-1.6 1.5A16 16 0 0 1 3.7 6.1a1.5 1.5 0 0 1 1.5-1.6z"/><path d="M15 3.5l4.5 4.5M19.5 3.5L15 8"/>""",
        ["telefono-rellamada"] = """<path d="M5.2 4.5h3l1.5 3.9-2 1.3a11 11 0 0 0 6.6 6.6l1.3-2 3.9 1.5v3a1.5 1.5 0 0 1-1.6 1.5A16 16 0 0 1 3.7 6.1a1.5 1.5 0 0 1 1.5-1.6z"/><path d="M14.3 6.3a3.6 3.6 0 1 0 1.1-2.6"/><path d="M15.6 1.8l-.3 2 2 .3"/>""",
        ["tablero"] = """<rect x="3.5" y="4" width="17" height="11.5" rx="1.5"/><path d="M12 15.5V19M8.5 21l3.5-2 3.5 2"/><path d="M7 12l3-3 2.5 2 4-4"/>""",
        ["kpi"] = """<path d="M4 19.5h16"/><path d="M5 15.5l4-4.5 3.5 2.5L19 7"/><path d="M15 7h4v4"/>""",
        ["filtro-x"] = """<path d="M3.5 5h15l-5.5 6.5v5.5l-4 2v-7.5z"/><path d="M16.5 15l4 4M20.5 15l-4 4"/>""",
        ["descarga"] = """<path d="M12 4v11"/><path d="M7.5 10.5L12 15l4.5-4.5"/><path d="M5 19.5h14"/>""",
        ["tema"] = """<circle cx="12" cy="12" r="8"/><path d="M12 4a8 8 0 0 0 0 16z" fill="currentColor" stroke="none"/>""",
        ["base-datos"] = """<ellipse cx="12" cy="6" rx="7" ry="2.8"/><path d="M5 6v12c0 1.5 3.1 2.8 7 2.8s7-1.3 7-2.8V6"/><path d="M5 12c0 1.5 3.1 2.8 7 2.8s7-1.3 7-2.8"/>""",
        ["refrescar"] = """<path d="M19.5 12a7.5 7.5 0 1 1-2.2-5.3"/><path d="M19.5 4.5v4h-4"/>""",
        ["flecha"] = """<path d="M5 12h14M13 6l6 6-6 6"/>""",
        ["imprimir"] = """<path d="M7 9V4h10v5"/><rect x="4" y="9" width="16" height="7" rx="1.5"/><path d="M7 14h10v6H7z"/>""",
        ["presentar"] = """<rect x="3.5" y="4.5" width="17" height="11" rx="1.5"/><path d="M12 15.5v4M8.5 19.5h7"/>""",
        ["plegar"] = """<rect x="3.5" y="4.5" width="17" height="15" rx="2"/><path d="M9 4.5v15"/><path d="M15.5 10l-2 2 2 2"/>""",
        ["filtro"] = """<path d="M4 5h16l-6 7v6l-4 2v-8z"/>""",
        ["cerrar"] = """<path d="M6 6l12 12M18 6L6 18"/>""",
        ["limpieza"] = """<ellipse cx="10" cy="5.8" rx="6.5" ry="2.6"/><path d="M3.5 5.8v10.4c0 1.4 2.9 2.6 6.5 2.6"/><path d="M16.5 5.8V10"/><path d="M3.5 11c0 1.4 2.9 2.6 6.5 2.6"/><path d="M13.5 17.2l2.4 2.4 4.6-5"/>""",
        ["volver"] = """<path d="M19 12H5M11 6l-6 6 6 6"/>""",
        ["duplicado"] = """<rect x="8" y="8" width="12" height="12" rx="2"/><path d="M16 8V5.5A1.5 1.5 0 0 0 14.5 4h-9A1.5 1.5 0 0 0 4 5.5v9A1.5 1.5 0 0 0 5.5 16H8"/>""",
        // Orígenes de «Elegir fuente»: BigQuery (nube), SQL Server (servidor), ODBC (conector) y Excel o CSV (hoja)
        ["nube"] = """<path d="M7 18.5h10a4 4 0 0 0 .6-7.95A5.5 5.5 0 0 0 7 9.5a4.5 4.5 0 0 0 0 9z"/><path d="M10 14.5h4M12 12.5v4"/>""",
        ["servidor"] = """<rect x="4" y="4" width="16" height="6.5" rx="1.5"/><rect x="4" y="13.5" width="16" height="6.5" rx="1.5"/><path d="M7.5 7.25h.01M7.5 16.75h.01M11 7.25h5.5M11 16.75h5.5"/>""",
        ["conector"] = """<path d="M9 3.5v4M15 3.5v4"/><path d="M6.5 7.5h11v3a5.5 5.5 0 0 1-11 0z"/><path d="M12 16v4.5"/>""",
        ["hoja"] = """<rect x="4" y="3.5" width="16" height="17" rx="2"/><path d="M4 9h16M4 14.5h16M10 3.5v17"/>""",
    };

    public static HtmlString Svg(string nombre, string clase = "icono") =>
        new($"""<svg class="{clase}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false">{Trazos.GetValueOrDefault(nombre, "")}</svg>""");

    /// <summary>
    /// Rellamada con su plazo: el teléfono abajo a la izquierda y, arriba a la derecha, la flecha de
    /// vuelta (abierta arriba, girando hacia la izquierda) con el número dentro («24», «72»).
    /// Todo cabe en el cuadro de 24 × 24, así que dentro de un círculo no se sale.
    /// </summary>
    public static HtmlString Rellamada(string numero, string clase = "icono") =>
        new($"""<svg class="{clase} icono-rellamada" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path transform="translate(0.6 5.6) scale(0.78)" stroke-width="2.05" d="M5.2 4.5h3l1.5 3.9-2 1.3a11 11 0 0 0 6.6 6.6l1.3-2 3.9 1.5v3a1.5 1.5 0 0 1-1.6 1.5A16 16 0 0 1 3.7 6.1a1.5 1.5 0 0 1 1.5-1.6z"/><path d="M12.55 4.15A5.3 5.3 0 1 0 20.05 4.15"/><path d="M20.05 6.6V4.15h2.45"/><text class="icono-numero" x="16.3" y="8.05" text-anchor="middle" dominant-baseline="central">{System.Net.WebUtility.HtmlEncode(numero)}</text></svg>""");
}
