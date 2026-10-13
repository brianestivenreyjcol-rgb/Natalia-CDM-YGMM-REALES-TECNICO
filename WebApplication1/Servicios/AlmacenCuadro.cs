using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Options;
using WebApplication1.Models;

namespace WebApplication1.Servicios;

/// <summary>Foto de los datos traídos de BigQuery, ordenada por fecha. No cambia una vez creada.</summary>
public sealed class InstantaneaCuadro
{
    public InstantaneaCuadro(List<FilaCuadro> filas, DateTime actualizado, TimeSpan duracion, DateOnly desdeConsulta)
    {
        filas.Sort((a, b) => a.Fecha.CompareTo(b.Fecha));
        Filas = filas;
        Actualizado = actualizado;
        Duracion = duracion;
        PrimerDia = filas.Count > 0 ? filas[0].Fecha : desdeConsulta;
        UltimoDia = filas.Count > 0 ? filas[^1].Fecha : DateOnly.FromDateTime(actualizado);
    }

    public IReadOnlyList<FilaCuadro> Filas { get; }
    public DateTime Actualizado { get; }
    public TimeSpan Duracion { get; }
    public DateOnly PrimerDia { get; }
    public DateOnly UltimoDia { get; }

    /// <summary>Filas entre dos fechas (incluidas), sin recorrer las demás.</summary>
    public IEnumerable<FilaCuadro> EntreFechas(DateOnly desde, DateOnly hasta)
    {
        int bajo = 0, alto = Filas.Count;
        while (bajo < alto)
        {
            var medio = (bajo + alto) / 2;
            if (Filas[medio].Fecha < desde) bajo = medio + 1; else alto = medio;
        }
        for (var i = bajo; i < Filas.Count && Filas[i].Fecha <= hasta; i++) yield return Filas[i];
    }
}

/// <summary>
/// Guarda en memoria el resumen diario de BigQuery para que las páginas filtren y calculen
/// sin consultar la base en cada visita. Lo refresca <see cref="ActualizadorCuadro"/>.
/// </summary>
public sealed class AlmacenCuadro(ServicioBigQuery bigQuery, IOptions<OpcionesCuadro> opciones, ILogger<AlmacenCuadro> registro)
{
    private InstantaneaCuadro? _actual;
    private readonly SemaphoreSlim _cargando = new(1, 1);
    private readonly SemaphoreSlim _peticion = new(0, 1);

    public InstantaneaCuadro? Actual => Volatile.Read(ref _actual);
    public bool Cargando => _cargando.CurrentCount == 0;
    public DateTime? InicioCarga { get; private set; }
    public DateTime? UltimoIntento { get; private set; }
    public string? UltimoError { get; private set; }

    /// <summary>Trae los datos de BigQuery. Si ya hay una carga en curso, no lanza otra.</summary>
    public async Task CargarAsync(CancellationToken ct)
    {
        if (!await _cargando.WaitAsync(0, ct)) return;
        try
        {
            InicioCarga = DateTime.Now;
            var desde = DateOnly.FromDateTime(DateTime.Today).AddDays(-opciones.Value.DiasHistorico);
            var reloj = Stopwatch.StartNew();
            var filas = await bigQuery.ConsultarAsync(
                ConsultasCuadro.ResumenDiario(desde), PrepararLector, opciones.Value.SegundosEsperaConsulta, ct);
            Volatile.Write(ref _actual, new InstantaneaCuadro(filas, DateTime.Now, reloj.Elapsed, desde));
            UltimoError = null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            UltimoError = ex.Message;
            registro.LogError(ex, "No se pudo cargar el cuadro de mando desde BigQuery");
        }
        finally
        {
            UltimoIntento = DateTime.Now;
            InicioCarga = null;
            _cargando.Release();
        }
    }

    /// <summary>Pide al actualizador que vuelva a cargar ya, sin esperar a su próxima vuelta.</summary>
    public void PedirActualizacion()
    {
        if (_peticion.CurrentCount == 0) _peticion.Release();
    }

    /// <summary>Espera el tiempo indicado o hasta que alguien pida una actualización.</summary>
    public Task EsperarAsync(TimeSpan espera, CancellationToken ct) => _peticion.WaitAsync(espera, ct);

    /// <summary>Resuelve las columnas de Agregado.sql una vez y devuelve el lector de cada fila.</summary>
    private static Func<FilaCuadro?> PrepararLector(DbDataReader lector)
    {
        int Columna(string nombre) => lector.GetOrdinal(nombre);
        int fecha = Columna("Fecha"), site = Columna("Site"), sector = Columna("Sector"), supervisor = Columna("Supervisor"),
            agente = Columna("Agente"), marca = Columna("Marca"), servicio = Columna("Servicio"), detallado = Columna("Detallado"),
            filasDetalle = Columna("Filas"),
            atendidas = Columna("Atendidas"), qRell = Columna("Q_rell"), segundos = Columna("Segundos_atencion"),
            noSolucion = Columna("No_solucion"), baseNoSolucion = Columna("Base_no_solucion"),
            rell24 = Columna("Rell_24h"), rell72 = Columna("Rell_72h"),
            transferencias = Columna("Transferencias");

        // Los textos se repiten muchísimo: se guarda una sola copia de cada uno
        var textos = new Dictionary<string, string>(StringComparer.Ordinal);
        string Texto(int i)
        {
            var valor = lector.IsDBNull(i) ? "" : Convert.ToString(lector.GetValue(i), CultureInfo.InvariantCulture)?.Trim() ?? "";
            if (valor.Length == 0) valor = "Sin dato";
            if (!textos.TryGetValue(valor, out var unico)) textos[valor] = unico = valor;
            return unico;
        }
        long Entero(int i) => lector.IsDBNull(i) ? 0 : Convert.ToInt64(lector.GetValue(i), CultureInfo.InvariantCulture);
        double Real(int i) => lector.IsDBNull(i) ? 0 : Convert.ToDouble(lector.GetValue(i), CultureInfo.InvariantCulture);

        return () =>
        {
            if (lector.IsDBNull(fecha)) return null;
            var texto = Convert.ToString(lector.GetValue(fecha), CultureInfo.InvariantCulture)?.Trim() ?? "";
            if (!DateOnly.TryParseExact(texto, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia)) return null;
            return new FilaCuadro
            {
                Fecha = dia,
                Semana = ISOWeek.GetWeekOfYear(dia.ToDateTime(TimeOnly.MinValue)),
                Lunes = dia.AddDays(-(((int)dia.DayOfWeek + 6) % 7)),
                Site = Texto(site),
                Sector = Texto(sector),
                Supervisor = Texto(supervisor),
                Agente = Texto(agente),
                Marca = Texto(marca).ToUpperInvariant(),
                Servicio = Texto(servicio),
                Detallado = Texto(detallado),
                Filas = Entero(filasDetalle),
                Atendidas = Entero(atendidas),
                QRell = Entero(qRell),
                SegundosAtencion = Real(segundos),
                NoSolucion = Entero(noSolucion),
                BaseNoSolucion = Entero(baseNoSolucion),
                Rell24 = Entero(rell24),
                Rell72 = Entero(rell72),
                Transferencias = Entero(transferencias),
            };
        };
    }
}
