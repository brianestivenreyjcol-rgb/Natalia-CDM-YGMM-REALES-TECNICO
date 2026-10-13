using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using WebApplication1.Infraestructura;
using WebApplication1.Models;

namespace WebApplication1.Servicios;

/// <summary>
/// El Exportable: el detalle por conversación (Consultas/Exportable.sql) con las fechas y los filtros de
/// la pantalla, en CSV para Excel. Se consulta a BigQuery en el momento (los datos en memoria están
/// resumidos y no tienen las conversaciones) y se escribe en la respuesta según van llegando las filas.
/// </summary>
public sealed class ServicioExportable(ServicioBigQuery bigQuery, IOptions<OpcionesCuadro> opciones)
{
    /// <summary>Nombres de columna de Exportable.sql → cabecera del CSV (los demás cambian «_» por espacio).</summary>
    private static readonly Dictionary<string, string> Cabeceras = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ID_conversacion"] = "ID conversación",
        ["Hora_sesion"] = "Hora sesión Schaman",
        ["Login_agente"] = "Login agente (Genesys)",
        ["Marca_Schaman"] = "Marca Schaman",
        ["Direccion"] = "Dirección",
        ["Paso_agente"] = "Paso del agente",
        ["Tipo_desconexion"] = "Tipo de desconexión",
        ["ID_cliente"] = "ID cliente",
        ["Ultima_historia"] = "Última historia",
        ["Motivo_consulta"] = "Motivo de consulta",
        ["Q_rell"] = "Q rell",
        ["AHT"] = "AHT (s)",
        ["ACW"] = "ACW saliente (s)",
        ["Tiempo_conversacion"] = "Tiempo conversación (s)",
        ["Tiempo_espera"] = "Tiempo espera (s)",
        ["Tiempo_ACW"] = "Tiempo ACW (s)",
        ["Tiempo_timbrado"] = "Tiempo timbrado (s)",
        ["No_solucion"] = "No solución",
        ["Rell_24H_kpi"] = "Rell 24H",
        ["Rell_1H_Dep"] = "Rell 1H departamento",
        ["Rell_24H_Dep"] = "Rell 24H departamento",
        ["Llamada_atendida"] = "Llamada atendida (KPI)",
    };

    public static string NombreArchivo(DateOnly desde, DateOnly hasta) =>
        $"cuadro-mando-ygmm-detalle_{Formato.Iso(desde)}_{Formato.Iso(hasta)}.csv";

    public async Task EscribirCsvAsync(Filtros filtros, DateOnly desde, DateOnly hasta, Stream destino, CancellationToken ct)
    {
        var sql = ConsultasCuadro.Exportable(desde, hasta.AddDays(1), Condiciones(filtros));
        await bigQuery.LeerAsync(sql, opciones.Value.SegundosEsperaConsulta, async lector =>
        {
            // UTF-8 con BOM para que Excel lea bien las tildes; la respuesta no se cierra al terminar
            await using var escritor = new StreamWriter(destino, new UTF8Encoding(true), 64 * 1024, leaveOpen: true);
            var columnas = Enumerable.Range(0, lector.FieldCount).Select(lector.GetName).ToList();
            await escritor.WriteLineAsync(string.Join(Csv.Separador,
                columnas.Select(c => Csv.Texto(Cabeceras.GetValueOrDefault(c, c.Replace('_', ' '))))));

            var valores = new object[lector.FieldCount];
            var linea = new StringBuilder(512);
            while (await lector.ReadAsync(ct))
            {
                lector.GetValues(valores);
                linea.Clear();
                for (var i = 0; i < valores.Length; i++)
                {
                    if (i > 0) linea.Append(Csv.Separador);
                    linea.Append(Csv.Celda(valores[i]));
                }
                await escritor.WriteLineAsync(linea, ct);
            }
        }, ct);
    }

    /// <summary>
    /// Filtros de la pantalla como condiciones «AND … IN (…)». Los textos van escapados como literales
    /// de BigQuery y las semanas solo admiten números, así que nada de la URL llega tal cual a la consulta.
    /// Las expresiones normalizan igual que Agregado.sql (vacío → «Sin dato»).
    /// </summary>
    private static string Condiciones(Filtros f)
    {
        var sql = new StringBuilder();
        void En(string expresion, IEnumerable<string> valores)
        {
            var lista = valores.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => Literal(v.Trim())).Distinct().ToList();
            if (lista.Count > 0) sql.Append("\n  AND ").Append(expresion).Append(" IN (").Append(string.Join(", ", lista)).Append(')');
        }

        En("IFNULL(NULLIF(TRIM(Site), ''), 'Sin dato')", f.Site);
        En("IFNULL(NULLIF(TRIM(Super), ''), 'Sin dato')", f.Supervisor);
        En("IFNULL(NULLIF(TRIM(Sector), ''), 'Sin dato')", f.Sector);
        En("IFNULL(NULLIF(TRIM(COALESCE(Personal, CAST(agente AS STRING), '')), ''), 'Sin dato')", f.Agente);

        var semanas = f.Semana
            .Select(s => int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : 0)
            .Where(n => n is >= 1 and <= 53)
            .Distinct()
            .ToList();
        if (semanas.Count > 0) sql.Append("\n  AND EXTRACT(ISOWEEK FROM Fecha_dt) IN (").Append(string.Join(", ", semanas)).Append(')');

        return sql.ToString();
    }

    /// <summary>Literal de texto de BigQuery con las barras, comillas y saltos de línea escapados.</summary>
    private static string Literal(string valor) =>
        "'" + valor.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "\\r").Replace("\n", "\\n") + "'";
}
