using System.Data.Common;
using System.Data.Odbc;
using System.Diagnostics;

namespace WebApplication1.Servicios;

/// <summary>
/// Conexión a BigQuery por ODBC (Simba ODBC Driver for Google BigQuery).
/// La cadena sale de ConnectionStrings:BigQuery, p. ej. «DSN=BQCOL» (el DSN de usuario de este equipo).
/// </summary>
public sealed class ServicioBigQuery(IConfiguration configuracion, ILogger<ServicioBigQuery> registro)
{
    private readonly string _cadena = configuracion.GetConnectionString("BigQuery") ?? "";

    public bool Configurado => !string.IsNullOrWhiteSpace(_cadena);

    /// <summary>Cadena de conexión sin secretos, para enseñarla en la página de estado.</summary>
    public string Descripcion => Cadenas.SinSecretos(_cadena);

    /// <summary>
    /// Ejecuta la consulta y lee cada fila. <paramref name="preparar"/> recibe el lector una vez
    /// (para resolver las columnas por nombre) y devuelve la función que lee la fila actual.
    /// </summary>
    public async Task<List<T>> ConsultarAsync<T>(string sql, Func<DbDataReader, Func<T?>> preparar, int segundosEspera, CancellationToken ct)
        where T : class
    {
        if (!Configurado) throw new InvalidOperationException("Falta la cadena de conexión «BigQuery» en appsettings.json.");

        for (var intento = 1; ; intento++)
        {
            var reloj = Stopwatch.StartNew();
            try
            {
                await using var conexion = new OdbcConnection(_cadena);
                await conexion.OpenAsync(ct);
                await using var comando = conexion.CreateCommand();
                comando.CommandText = sql;
                comando.CommandTimeout = segundosEspera;

                await using var lector = await comando.ExecuteReaderAsync(ct);
                var leerFila = preparar(lector);
                var filas = new List<T>();
                while (await lector.ReadAsync(ct))
                {
                    if (leerFila() is { } fila) filas.Add(fila);
                }

                registro.LogInformation("BigQuery: {Filas} filas en {Segundos:N1} s", filas.Count, reloj.Elapsed.TotalSeconds);
                return filas;
            }
            catch (OdbcException ex) when (intento < 3 && EsDeRed(ex))
            {
                // Cortes del proxy («Failure when receiving data from the peer», «Storage API Error»):
                // se repite la consulta; BigQuery la sirve de su caché sin volver a facturarla.
                registro.LogWarning("BigQuery: fallo de red (intento {Intento}), se reintenta: {Error}", intento, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(5 * intento), ct);
            }
        }
    }

    /// <summary>
    /// Ejecuta la consulta y entrega el lector a <paramref name="procesar"/> para ir leyendo fila a fila
    /// (descargas grandes). Un corte de red se reintenta solo antes de empezar a leer: una vez que
    /// <paramref name="procesar"/> ha escrito algo, ya no se puede repetir.
    /// </summary>
    public async Task LeerAsync(string sql, int segundosEspera, Func<DbDataReader, Task> procesar, CancellationToken ct)
    {
        if (!Configurado) throw new InvalidOperationException("Falta la cadena de conexión «BigQuery» en appsettings.json.");

        for (var intento = 1; ; intento++)
        {
            var reloj = Stopwatch.StartNew();
            var conexion = new OdbcConnection(_cadena);
            OdbcCommand? comando = null;
            DbDataReader lector;
            try
            {
                await conexion.OpenAsync(ct);
                comando = conexion.CreateCommand();
                comando.CommandText = sql;
                comando.CommandTimeout = segundosEspera;
                lector = await comando.ExecuteReaderAsync(ct);
            }
            catch (OdbcException ex) when (intento < 3 && EsDeRed(ex))
            {
                comando?.Dispose();
                await conexion.DisposeAsync();
                registro.LogWarning("BigQuery: fallo de red (intento {Intento}), se reintenta: {Error}", intento, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(5 * intento), ct);
                continue;
            }
            catch
            {
                comando?.Dispose();
                await conexion.DisposeAsync();
                throw;
            }

            await using (conexion)
            using (comando)
            await using (lector)
            {
                await procesar(lector);
            }
            registro.LogInformation("BigQuery: lectura terminada en {Segundos:N1} s", reloj.Elapsed.TotalSeconds);
            return;
        }
    }

    /// <summary>Un error de sintaxis o de permisos (SQLSTATE 42xxx / 28xxx) no se arregla reintentando.</summary>
    private static bool EsDeRed(OdbcException ex) =>
        !ex.Errors.Cast<OdbcError>().Any(e => e.SQLState.StartsWith("42", StringComparison.Ordinal) || e.SQLState.StartsWith("28", StringComparison.Ordinal));

    /// <summary>Abre la conexión y lanza un SELECT 1.</summary>
    public async Task<PruebaConexion> ProbarAsync(CancellationToken ct)
    {
        if (!Configurado) return new PruebaConexion(false, "Sin configurar (ConnectionStrings:BigQuery).", 0);
        var reloj = Stopwatch.StartNew();
        try
        {
            // El controlador ODBC es síncrono: se ejecuta fuera del hilo de la petición
            return await Task.Run(async () =>
            {
                await using var conexion = new OdbcConnection(_cadena);
                await conexion.OpenAsync(ct);
                await using var comando = conexion.CreateCommand();
                comando.CommandText = "SELECT 1";
                comando.CommandTimeout = 60;
                await comando.ExecuteScalarAsync(ct);
                return new PruebaConexion(true, $"Conectado ({conexion.Driver}).", reloj.ElapsedMilliseconds);
            }, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            registro.LogWarning(ex, "No se pudo conectar con BigQuery");
            return new PruebaConexion(false, ex.Message, reloj.ElapsedMilliseconds);
        }
    }
}
