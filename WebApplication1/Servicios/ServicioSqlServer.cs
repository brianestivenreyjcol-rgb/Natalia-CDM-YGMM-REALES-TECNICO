using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace WebApplication1.Servicios;

/// <summary>
/// Conexión a SQL Server con Microsoft.Data.SqlClient. La cadena sale de ConnectionStrings:SqlServer.
/// Las consultas van siempre con parámetros (nunca concatenando valores en el texto).
/// </summary>
public sealed class ServicioSqlServer(IConfiguration configuracion, ILogger<ServicioSqlServer> registro)
{
    private readonly string _cadena = configuracion.GetConnectionString("SqlServer") ?? "";

    public bool Configurado => !string.IsNullOrWhiteSpace(_cadena);

    public string Descripcion => Cadenas.SinSecretos(_cadena);

    /// <summary>Ejecuta una consulta y lee cada fila con la función que devuelve <paramref name="preparar"/>.</summary>
    public async Task<List<T>> ConsultarAsync<T>(
        string sql,
        Func<SqlDataReader, Func<T>> preparar,
        IEnumerable<SqlParameter>? parametros = null,
        CancellationToken ct = default)
    {
        await using var conexion = await AbrirAsync(ct);
        await using var comando = Comando(conexion, sql, parametros);
        await using var lector = await comando.ExecuteReaderAsync(ct);
        var leerFila = preparar(lector);
        var filas = new List<T>();
        while (await lector.ReadAsync(ct)) filas.Add(leerFila());
        return filas;
    }

    /// <summary>Ejecuta un INSERT, UPDATE, DELETE o procedimiento y devuelve las filas afectadas.</summary>
    public async Task<int> EjecutarAsync(string sql, IEnumerable<SqlParameter>? parametros = null, CancellationToken ct = default)
    {
        await using var conexion = await AbrirAsync(ct);
        await using var comando = Comando(conexion, sql, parametros);
        return await comando.ExecuteNonQueryAsync(ct);
    }

    /// <summary>Devuelve el primer valor de la primera fila.</summary>
    public async Task<object?> EscalarAsync(string sql, IEnumerable<SqlParameter>? parametros = null, CancellationToken ct = default)
    {
        await using var conexion = await AbrirAsync(ct);
        await using var comando = Comando(conexion, sql, parametros);
        var valor = await comando.ExecuteScalarAsync(ct);
        return valor is DBNull ? null : valor;
    }

    /// <summary>Abre la conexión y pide la versión del servidor.</summary>
    public async Task<PruebaConexion> ProbarAsync(CancellationToken ct)
    {
        if (!Configurado) return new PruebaConexion(false, "Sin configurar (ConnectionStrings:SqlServer).", 0);
        var reloj = Stopwatch.StartNew();
        try
        {
            var constructor = new SqlConnectionStringBuilder(_cadena);
            constructor.ConnectTimeout = Math.Min(constructor.ConnectTimeout, 10);
            await using var conexion = new SqlConnection(constructor.ConnectionString);
            await conexion.OpenAsync(ct);
            return new PruebaConexion(true, $"Conectado a {conexion.DataSource} · {conexion.Database} (SQL Server {conexion.ServerVersion}).", reloj.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            registro.LogWarning(ex, "No se pudo conectar con SQL Server");
            return new PruebaConexion(false, ex.Message, reloj.ElapsedMilliseconds);
        }
    }

    private async Task<SqlConnection> AbrirAsync(CancellationToken ct)
    {
        if (!Configurado) throw new InvalidOperationException("Falta la cadena de conexión «SqlServer» en appsettings.json.");
        var conexion = new SqlConnection(_cadena);
        await conexion.OpenAsync(ct);
        return conexion;
    }

    private static SqlCommand Comando(SqlConnection conexion, string sql, IEnumerable<SqlParameter>? parametros)
    {
        var comando = conexion.CreateCommand();
        comando.CommandText = sql;
        comando.CommandTimeout = 120;
        if (parametros is not null) comando.Parameters.AddRange(parametros.ToArray());
        return comando;
    }
}

/// <summary>Utilidades para enseñar cadenas de conexión sin contraseñas ni tokens.</summary>
public static class Cadenas
{
    private static readonly string[] Secretos = ["password", "pwd", "token", "secret", "key", "refreshtoken"];

    public static string SinSecretos(string cadena)
    {
        if (string.IsNullOrWhiteSpace(cadena)) return "(sin configurar)";
        var partes = cadena.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(parte =>
        {
            var igual = parte.IndexOf('=');
            if (igual < 0) return parte;
            var clave = parte[..igual].Trim();
            return Secretos.Any(s => clave.Contains(s, StringComparison.OrdinalIgnoreCase)) ? $"{clave}=****" : parte.Trim();
        });
        return string.Join("; ", partes);
    }
}
