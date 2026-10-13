using System.Globalization;
using Microsoft.AspNetCore.Http.Extensions;
using WebApplication1.Infraestructura;
using WebApplication1.Models;

namespace WebApplication1.Servicios;

/// <summary>
/// Arma cada página del cuadro de mando a partir de los datos en memoria (<see cref="AlmacenCuadro"/>):
/// aplica los filtros, calcula los indicadores con su variación y prepara las gráficas con su nota y
/// su frase de lectura. Devuelve null mientras no haya datos cargados.
/// </summary>
public sealed class ServicioTablero(AlmacenCuadro almacen)
{
    private const string Medellin = "Medellín";
    private const string Bogota = "Bogotá";
    private const string Otros = "Otros";
    private static readonly string[] OrdenSites = [Medellin, Bogota];
    private static readonly string[] OrdenMarcas = ["YOIGO", "MASMOVIL"];
    private static readonly StringComparer Alfabetico = StringComparer.Create(Formato.Es, ignoreCase: true);
    private static readonly string[] TextosSemana = Enumerable.Range(0, 54).Select(n => n.ToString(CultureInfo.InvariantCulture)).ToArray();

    // ───────────────────────── Páginas ─────────────────────────

    public ModeloDashboard? Dashboard(Filtros filtros, string ruta)
    {
        if (Preparar(filtros, Pagina.Dashboard, "Dashboard principal", "Dashboard", Indicador.NoSolucion, ruta) is not { } c) return null;
        return new ModeloDashboard
        {
            Marco = c.Marco,
            NoSolucionSemana = LineasPorSite(c, "ns-semana", "No solución por semana", Indicador.NoSolucion, porDia: false),
            NoSolucionMarca = ColumnasSiteMarca(c, "ns-marca", "No solución por marca", Indicador.NoSolucion),
            NoSolucionServicio = BarrasPorSite(c, "ns-servicio", "No solución por servicio", Indicador.NoSolucion, f => f.Servicio, 8, "servicio"),
            Rell72Semana = LineasPorSite(c, "r72-semana", "Rellamada 72H por semana", Indicador.Rell72, porDia: false),
            Rell72Marca = ColumnasSiteMarca(c, "r72-marca", "Rellamada 72H por marca", Indicador.Rell72),
            Rell72Servicio = BarrasPorSite(c, "r72-servicio", "Rellamada 72H por servicio", Indicador.Rell72, f => f.Servicio, 8, "servicio"),
            TmoSemana = LineasPorSite(c, "tmo-semana", "TMO por semana", Indicador.Tmo, porDia: false),
            AtendidasDia = LineasPorSite(c, "atendidas-dia", "Atendidas por día", Indicador.Atendidas, porDia: true, desdeCero: true, area: "ancho-2"),
        };
    }

    public ModeloKpi? Kpi(Filtros filtros, string ruta)
    {
        if (Preparar(filtros, Pagina.Kpi, "KPIs integrados", "KPIs integrados", Indicador.Atendidas, ruta) is not { } c) return null;
        var semanas = Periodos(c, porDia: false);
        var categorias = semanas.Select(s => (s.Clave, s.Etiqueta)).ToList();
        var fichas = semanas.Select(s => s.Ficha).ToList();

        // Cada indicador con su pareja de tonos de la paleta: la rellamada en morado e índigo, la no solución en magenta y lila
        var evolucion = new List<DefinicionSerie>();
        foreach (var (marca, clase) in new[] { ("YOIGO", "serie-a"), ("MASMOVIL", "serie-c") })
            if (c.Marcas.Contains(marca)) evolucion.Add(new($"Rell 24H ({marca})", clase, f => f.Marca == marca, Indicador.Rell24));
        foreach (var (marca, clase) in new[] { ("YOIGO", "serie-b"), ("MASMOVIL", "serie-d") })
            if (c.Marcas.Contains(marca)) evolucion.Add(new($"No solución ({marca})", clase, f => f.Marca == marca, Indicador.NoSolucion));

        var atendidasMarca = Columnas(c, "atendidas-marca", "Atendidas por marca y semana", Indicador.Atendidas,
            categorias, f => f.Lunes, c.SeriesMarca, f => f.Marca, "area-atendidas", fichas);
        var tmoMarca = Columnas(c, "tmo-marca", "TMO por marca y semana", Indicador.Tmo,
            categorias, f => f.Lunes, c.SeriesMarca, f => f.Marca, "area-tmo", fichas);
        var supervisor = Barras(c, "atendidas-supervisor", "Atendidas por supervisor", Indicador.Atendidas,
            f => f.Supervisor, 20, c.SeriesMarca, f => f.Marca, ordenarPorVolumen: true, gruesas: false, "area-supervisor", "supervisor");
        // Cuatro líneas con su pastilla en cada punto: sin volumen detrás y con más alto, para que las pastillas no se pisen
        var evolucionKpi = Lineas(c, "evolucion", "Evolución de rellamada 24H y no solución", porDia: false, evolucion,
            TipoValor.Porcentaje, area: "area-evolucion", conVolumen: false, nota: "Por semana y marca", alto: true);

        return new ModeloKpi
        {
            Marco = c.Marco,
            AtendidasMarca = atendidasMarca,
            TmoMarca = tmoMarca,
            AtendidasSupervisor = supervisor,
            Evolucion = evolucionKpi,
            Dias = c.Filas.Agrupar(f => f.Fecha).OrderBy(g => g.Key).Select(g => new FilaDia(g.Key, g.Value)).ToList(),
            Total = Acumulado.De(c.Filas),
        };
    }

    /// <summary>No solución, Rellamada 24H y Rellamada 72H comparten página con otro indicador.</summary>
    public ModeloIndicador? PaginaIndicador(Indicador indicador, Filtros filtros, string ruta)
    {
        var pagina = indicador switch
        {
            Indicador.Rell24 => Pagina.Rell24,
            Indicador.Rell72 => Pagina.Rell72,
            _ => Pagina.NoSolucion,
        };
        var nombre = indicador.NombreCorto();
        if (Preparar(filtros, pagina, nombre, nombre, indicador, ruta) is not { } c) return null;
        return new ModeloIndicador
        {
            Marco = c.Marco,
            Indicador = indicador,
            PorMarca = ColumnasSiteMarca(c, "marca", $"{nombre} por marca", indicador, "area-marca"),
            PorServicio = BarrasPorSite(c, "servicio", $"{nombre} por servicio", indicador, f => f.Servicio, 8, "servicio", area: "area-servicio"),
            PorDetallado = BarrasPorSite(c, "detallado", $"{nombre} por detallado", indicador, f => f.Detallado, 10, "detallado", gruesas: true, area: "area-detallado"),
            PorSemana = LineasPorSite(c, "semana", $"{nombre} por semana", indicador, porDia: false, area: "area-semana"),
            PorDia = LineasPorSite(c, "dia", $"{nombre} por día", indicador, porDia: true, area: "area-dia"),
        };
    }

    /// <summary>
    /// Fechas que se están viendo con estos filtros (las mismas que en pantalla), para el Exportable.
    /// Null mientras no haya datos cargados.
    /// </summary>
    public (DateOnly Desde, DateOnly Hasta)? Rango(Filtros filtros)
    {
        if (almacen.Actual is not { } datos) return null;
        var (desde, hasta, _, _) = ResolverFechas(datos, filtros);
        return (desde, hasta);
    }

    /// <summary>Por defecto, el mes del último día con datos; nunca fuera de los días cargados.</summary>
    private static (DateOnly Desde, DateOnly Hasta, DateOnly DesdeDefecto, DateOnly HastaDefecto) ResolverFechas(InstantaneaCuadro datos, Filtros filtros)
    {
        var hastaDefecto = datos.UltimoDia;
        var desdeDefecto = Mayor(new DateOnly(hastaDefecto.Year, hastaDefecto.Month, 1), datos.PrimerDia);
        var desde = Limitar(filtros.Desde ?? desdeDefecto, datos);
        var hasta = Limitar(filtros.Hasta ?? hastaDefecto, datos);
        if (desde > hasta) (desde, hasta) = (hasta, desde);
        return (desde, hasta, desdeDefecto, hastaDefecto);
    }

    // ───────────────────────── Filtros y contexto ─────────────────────────

    private sealed class Contexto
    {
        public required DateOnly Desde { get; init; }
        public required DateOnly Hasta { get; init; }

        /// <summary>Filas del periodo con todos los filtros aplicados.</summary>
        public required List<FilaCuadro> Filas { get; init; }
        public required List<string> Sites { get; init; }
        public required List<string> Marcas { get; init; }
        public MarcoPagina Marco { get; set; } = null!;

        public List<DefinicionSerie> SeriesSite(Indicador indicador) =>
            Sites.Select(s => new DefinicionSerie(s, ClaseSite(s), f => f.Site == s, indicador)).ToList();

        public IReadOnlyList<(string Valor, string Nombre, string Clase)> SeriesSiteClaves =>
            Sites.Select(s => (s, s, ClaseSite(s))).ToList();

        public IReadOnlyList<(string Valor, string Nombre, string Clase)> SeriesMarca =>
            Marcas.Select(m => (m, m, ClaseMarca(m))).ToList();
    }

    /// <summary>Un desplegable del panel: su selección y los valores que existen con el resto de filtros.</summary>
    private sealed class CampoFiltro(string clave, string etiqueta, Func<FilaCuadro, string> valor, IEnumerable<string> seleccion)
    {
        public string Clave { get; } = clave;
        public string Etiqueta { get; } = etiqueta;
        public HashSet<string> Seleccion { get; } =
            new(seleccion.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()), StringComparer.Ordinal);

        /// <summary>Valores disponibles, cada uno con una fila de ejemplo (para ordenar las semanas).</summary>
        public Dictionary<string, FilaCuadro> Vistos { get; } = new(StringComparer.Ordinal);

        public bool Cumple(FilaCuadro f) => Seleccion.Count == 0 || Seleccion.Contains(valor(f));
        public void Anotar(FilaCuadro f) => Vistos.TryAdd(valor(f), f);
    }

    private static List<CampoFiltro> Campos(Filtros f) =>
    [
        new("site", "Site", x => x.Site, f.Site),
        new("supervisor", "Supervisor", x => x.Supervisor, f.Supervisor),
        new("sector", "Sector", x => x.Sector, f.Sector),
        new("semana", "Semana", x => TextosSemana[x.Semana], f.Semana),
        new("agente", "Agente", x => x.Agente, f.Agente),
    ];

    private Contexto? Preparar(Filtros filtros, Pagina pagina, string titulo, string miga, Indicador principal, string ruta)
    {
        var datos = almacen.Actual;
        if (datos is null) return null;

        var (desde, hasta, desdeDefecto, hastaDefecto) = ResolverFechas(datos, filtros);

        // Una sola pasada: una fila que cumple todo entra en el periodo; si falla un solo filtro,
        // su valor sigue siendo una opción de ese desplegable (filtros en cascada)
        var campos = Campos(filtros);
        var filas = new List<FilaCuadro>();
        foreach (var fila in datos.EntreFechas(desde, hasta))
        {
            CampoFiltro? fallo = null;
            var descartada = false;
            foreach (var campo in campos)
            {
                if (campo.Cumple(fila)) continue;
                if (fallo is null) { fallo = campo; continue; }
                descartada = true;
                break;
            }
            if (descartada) continue;
            if (fallo is not null) { fallo.Anotar(fila); continue; }
            filas.Add(fila);
            foreach (var campo in campos) campo.Anotar(fila);
        }

        // Periodo anterior (ver PeriodoAnterior), con los mismos filtros salvo la semana
        var (antDesde, antHasta) = PeriodoAnterior(desde, hasta);
        Acumulado? anterior = null;
        if (antDesde >= datos.PrimerDia)
        {
            var sinSemana = campos.Where(c => c.Clave != "semana").ToList();
            anterior = Acumulado.De(datos.EntreFechas(antDesde, antHasta).Where(f => sinSemana.All(c => c.Cumple(f))));
            if (anterior.Atendidas == 0) anterior = null;
        }
        var actual = Acumulado.De(filas);

        var contexto = new Contexto
        {
            Desde = desde,
            Hasta = hasta,
            Filas = filas,
            Sites = Ordenar(filas.Select(f => f.Site).Distinct(), OrdenSites),
            Marcas = Ordenar(filas.Select(f => f.Marca).Distinct(), OrdenMarcas),
        };

        string ConsultaCon(DateOnly d, DateOnly h) => Consulta(filtros, d, h, desdeDefecto, hastaDefecto);
        TarjetaIndicador Tarjeta(Indicador i) => CrearTarjeta(i, i == principal, actual, anterior, antDesde, antHasta);

        contexto.Marco = new MarcoPagina
        {
            Pagina = pagina,
            Titulo = titulo,
            Miga = miga,
            Ruta = ruta,
            Consulta = ConsultaCon(desde, hasta),
            Desde = desde,
            Hasta = hasta,
            DesdeDefecto = desdeDefecto,
            HastaDefecto = hastaDefecto,
            PrimerDia = datos.PrimerDia,
            UltimoDia = datos.UltimoDia,
            Desplegables = campos.Select(CrearDesplegable).ToList(),
            Atajos = CrearAtajos(datos, desde, hasta, (d, h) => ruta + ConsultaCon(d, h)),
            Indicadores =
            [
                Tarjeta(Indicador.Atendidas),
                Tarjeta(Indicador.Tmo),
                Tarjeta(Indicador.NoSolucion),
                Tarjeta(Indicador.Rell24),
                Tarjeta(Indicador.Rell72),
            ],
            Actualizado = datos.Actualizado,
        };
        return contexto;
    }

    /// <summary>
    /// Con qué se compara la variación de las tarjetas, como en el informe de referencia:
    /// meses completos → los mismos meses anteriores (septiembre frente a agosto);
    /// un mes desde el día 1 → los mismos días del mes anterior (1–7 oct frente a 1–7 sept);
    /// cualquier otro rango → los mismos días inmediatamente anteriores.
    /// </summary>
    private static (DateOnly Desde, DateOnly Hasta) PeriodoAnterior(DateOnly desde, DateOnly hasta)
    {
        if (desde.Day == 1)
        {
            if (hasta.AddDays(1).Day == 1)
            {
                var meses = (hasta.Year - desde.Year) * 12 + hasta.Month - desde.Month + 1;
                return (desde.AddMonths(-meses), desde.AddDays(-1));
            }
            if (desde.Year == hasta.Year && desde.Month == hasta.Month)
                return (desde.AddMonths(-1), hasta.AddMonths(-1));
        }
        var dias = hasta.DayNumber - desde.DayNumber + 1;
        return (desde.AddDays(-dias), desde.AddDays(-1));
    }

    private static TarjetaIndicador CrearTarjeta(Indicador indicador, bool destacada, Acumulado actual, Acumulado? anterior, DateOnly antDesde, DateOnly antHasta)
    {
        var valor = actual.Valor(indicador);
        var previo = anterior?.Valor(indicador);
        double? variacion = valor is { } v && previo is { } p && p != 0 ? (v - p) / p * 100 : null;
        var (corta, larga) = anterior is null
            ? ("sin periodo anterior", "No hay datos del periodo anterior para comparar.")
            : (Formato.ComparacionCorta(antDesde, antHasta),
               $"Frente al periodo anterior ({Formato.Rango(antDesde, antHasta)}): {Formato.Valor(previo, indicador.Tipo())}");
        return new TarjetaIndicador(indicador, valor, variacion, corta, larga, destacada);
    }

    private static Desplegable CrearDesplegable(CampoFiltro campo)
    {
        var valores = campo.Vistos.Keys.Union(campo.Seleccion);
        var ordenados = campo.Clave == "semana"
            ? valores.OrderBy(v => campo.Vistos.TryGetValue(v, out var f) ? f.Lunes : DateOnly.MaxValue)
            : valores.OrderBy(v => v == "Sin dato" ? 1 : 0).ThenBy(v => v, Alfabetico);
        var opciones = ordenados
            .Select(v => new OpcionFiltro(v, campo.Clave == "semana" ? $"Semana {v}" : v, campo.Seleccion.Contains(v)))
            .ToList();
        return new Desplegable(campo.Clave, campo.Etiqueta, opciones);
    }

    private static List<Atajo> CrearAtajos(InstantaneaCuadro datos, DateOnly desde, DateOnly hasta, Func<DateOnly, DateOnly, string> url)
    {
        var ultimo = datos.UltimoDia;
        var primero = datos.PrimerDia;
        var inicioMes = new DateOnly(ultimo.Year, ultimo.Month, 1);
        (string Texto, DateOnly Desde, DateOnly Hasta)[] rangos =
        [
            ("Mes en curso", Mayor(inicioMes, primero), ultimo),
            ("Mes anterior", Mayor(inicioMes.AddMonths(-1), primero), inicioMes.AddDays(-1)),
            ("7 días", Mayor(ultimo.AddDays(-6), primero), ultimo),
            ("30 días", Mayor(ultimo.AddDays(-29), primero), ultimo),
        ];
        return rangos
            .Where(r => r.Desde <= r.Hasta)
            .Select(r => new Atajo(r.Texto, url(r.Desde, r.Hasta), r.Desde == desde && r.Hasta == hasta))
            .ToList();
    }

    /// <summary>Filtros como cadena de consulta; las fechas por defecto no se escriben (un enlace guardado sigue al último día).</summary>
    private static string Consulta(Filtros f, DateOnly desde, DateOnly hasta, DateOnly desdeDefecto, DateOnly hastaDefecto)
    {
        var consulta = new QueryBuilder();
        if (desde != desdeDefecto || hasta != hastaDefecto)
        {
            consulta.Add("desde", Formato.Iso(desde));
            consulta.Add("hasta", Formato.Iso(hasta));
        }
        foreach (var (clave, valores) in new[] { ("site", f.Site), ("supervisor", f.Supervisor), ("sector", f.Sector), ("semana", f.Semana), ("agente", f.Agente) })
            foreach (var valor in valores.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct())
                consulta.Add(clave, valor);
        return consulta.ToString();
    }

    // ───────────────────────── Gráficas ─────────────────────────

    private sealed record DefinicionSerie(string Nombre, string Clase, Func<FilaCuadro, bool> Incluye, Indicador Indicador);

    private sealed record Periodo(DateOnly Clave, string Etiqueta, string Ficha);

    /// <summary>Periodos del eje X: cada día del rango o cada semana con datos.</summary>
    private static List<Periodo> Periodos(Contexto c, bool porDia)
    {
        if (porDia)
        {
            var mismoMes = c.Desde.Year == c.Hasta.Year && c.Desde.Month == c.Hasta.Month;
            var dias = new List<Periodo>();
            for (var d = c.Desde; d <= c.Hasta; d = d.AddDays(1))
            {
                var etiqueta = mismoMes ? d.Day.ToString(CultureInfo.InvariantCulture) : d.ToString("dd/MM", CultureInfo.InvariantCulture);
                dias.Add(new Periodo(d, etiqueta, Formato.FechaLarga(d)));
            }
            return dias;
        }

        return c.Filas
            .Select(f => (f.Lunes, f.Semana))
            .Distinct()
            .OrderBy(s => s.Lunes)
            .Select(s => new Periodo(s.Lunes, $"Semana {s.Semana}",
                $"Semana {s.Semana} · {Formato.Rango(Mayor(s.Lunes, c.Desde), Menor(s.Lunes.AddDays(6), c.Hasta))}"))
            .ToList();
    }

    /// <summary>
    /// Líneas por periodo. Con <paramref name="conVolumen"/>, las atendidas de cada periodo (de todas las
    /// series) van como columnas grises detrás. La frase de lectura compara el último periodo (semanas) o
    /// señala el día más alto y el más bajo (días).
    /// </summary>
    private static GraficoLineas Lineas(Contexto c, string id, string titulo, bool porDia, IReadOnlyList<DefinicionSerie> series,
        TipoValor tipo, bool desdeCero = false, string? area = null, bool conVolumen = true, string? nota = null, bool alto = false)
    {
        var periodos = Periodos(c, porDia);
        var indice = new Dictionary<DateOnly, int>();
        for (var i = 0; i < periodos.Count; i++) indice[periodos[i].Clave] = i;

        var acumulados = series.Select(_ => new Acumulado?[periodos.Count]).ToList();
        var volumen = new double?[periodos.Count];
        foreach (var fila in c.Filas)
        {
            if (!indice.TryGetValue(porDia ? fila.Fecha : fila.Lunes, out var i)) continue;
            volumen[i] = (volumen[i] ?? 0) + fila.Atendidas;
            for (var s = 0; s < series.Count; s++)
                if (series[s].Incluye(fila)) (acumulados[s][i] ??= new Acumulado()).Sumar(fila);
        }

        var grafico = new GraficoLineas
        {
            Id = id,
            Titulo = titulo,
            Tipo = tipo,
            Area = area,
            DesdeCero = desdeCero,
            Alto = porDia || alto,
            Nota = nota,
            Volumen = conVolumen ? volumen : null,
            Categorias = periodos.Select(p => p.Etiqueta).ToList(),
            Fichas = periodos.Select(p => p.Ficha).ToList(),
            Series = series
                .Select((s, k) => new SerieGrafico(s.Nombre, s.Clase, acumulados[k].Select(a => a?.Valor(s.Indicador)).ToList()))
                .ToList(),
        };
        grafico.Lectura = porDia
            ? Lecturas.Extremos(grafico, i => periodos[i].Ficha, "día", serieDelante: false)
            : Lecturas.UltimoPeriodo(grafico);
        return grafico;
    }

    private static GraficoLineas LineasPorSite(Contexto c, string id, string titulo, Indicador indicador, bool porDia,
        bool desdeCero = false, string? area = null)
    {
        // Las atendidas ya son el volumen: esa gráfica no lleva columnas grises detrás
        var conVolumen = indicador != Indicador.Atendidas;
        var nota = $"{(porDia ? "Por día" : "Por semana")} y site · {indicador.NotaCorta()}{(conVolumen ? " · en gris, las atendidas" : "")}";
        return Lineas(c, id, titulo, porDia, c.SeriesSite(indicador), indicador.Tipo(), desdeCero, area, conVolumen, nota);
    }

    private static GraficoColumnas Columnas<TCategoria>(Contexto c, string id, string titulo, Indicador indicador,
        IReadOnlyList<(TCategoria Clave, string Etiqueta)> categorias, Func<FilaCuadro, TCategoria> categoriaDe,
        IReadOnlyList<(string Valor, string Nombre, string Clase)> series, Func<FilaCuadro, string> serieDe,
        string? area = null, IReadOnlyList<string>? fichas = null)
        where TCategoria : notnull
    {
        var grupos = c.Filas.Agrupar(f => (categoriaDe(f), serieDe(f)));
        var grafico = new GraficoColumnas
        {
            Id = id,
            Titulo = titulo,
            Tipo = indicador.Tipo(),
            Area = area,
            Fichas = fichas,
            Nota = $"Por marca · {indicador.NotaCorta()}",
            Categorias = categorias.Select(k => k.Etiqueta).ToList(),
            Series = series.Select(s => new SerieGrafico(s.Nombre, s.Clase,
                categorias.Select(k => grupos.TryGetValue((k.Clave, s.Valor), out var a) ? a.Valor(indicador) : null).ToList())).ToList(),
        };
        grafico.Lectura = Lecturas.Extremos(grafico, i => grafico.Categorias[i], "", serieDelante: true);
        return grafico;
    }

    /// <summary>Columnas por site, una por marca (Medellín / Bogotá × YOIGO / MASMOVIL).</summary>
    private static GraficoColumnas ColumnasSiteMarca(Contexto c, string id, string titulo, Indicador indicador, string? area = null) =>
        Columnas(c, id, titulo, indicador, c.Sites.Select(s => (s, s)).ToList(), f => f.Site, c.SeriesMarca, f => f.Marca, area);

    /// <summary>
    /// Barras horizontales por una dimensión (servicio, detallado, supervisor). Se quedan las
    /// <paramref name="maximo"/> categorías con más llamadas; el resto se suma en «Otros», que va al final.
    /// </summary>
    private static GraficoBarras Barras(Contexto c, string id, string titulo, Indicador indicador, Func<FilaCuadro, string> dimension,
        int maximo, IReadOnlyList<(string Valor, string Nombre, string Clase)> series, Func<FilaCuadro, string> serieDe,
        bool ordenarPorVolumen, bool gruesas, string? area, string nombreDimension)
    {
        var porCategoria = c.Filas.Agrupar(dimension);
        var principales = porCategoria.OrderByDescending(g => g.Value.Atendidas).Take(maximo).Select(g => g.Key).ToHashSet();
        var hayOtros = porCategoria.Count > principales.Count;
        string Categoria(FilaCuadro f)
        {
            var d = dimension(f);
            return principales.Contains(d) ? d : Otros;
        }

        var totales = c.Filas.Agrupar(Categoria);
        var orden = totales
            .OrderBy(g => hayOtros && g.Key == Otros ? 1 : 0)
            .ThenByDescending(g => ordenarPorVolumen ? g.Value.Atendidas : g.Value.Valor(indicador) ?? double.MinValue)
            .Select(g => g.Key)
            .ToList();
        var grupos = c.Filas.Agrupar(f => (Categoria(f), serieDe(f)));

        var grafico = new GraficoBarras
        {
            Id = id,
            Titulo = titulo,
            Tipo = indicador.Tipo(),
            Area = area,
            Gruesas = gruesas,
            Nota = $"Por {nombreDimension} · {indicador.NotaCorta()}{(hayOtros ? $" · los de menos volumen, en «{Otros}»" : "")}",
            Categorias = orden,
            Fichas = orden.Select(k => $"{k} · {Formato.Valor(totales[k].Valor(indicador), indicador.Tipo())} en total").ToList(),
            Series = series.Select(s => new SerieGrafico(s.Nombre, s.Clase,
                orden.Select(k => grupos.TryGetValue((k, s.Valor), out var a) ? a.Valor(indicador) : null).ToList())).ToList(),
        };

        grafico.Lectura = ordenarPorVolumen
            ? Lecturas.Reparto(orden, totales, c.Filas.Sum(f => f.Atendidas))
            : Lecturas.Extremos(grafico, i => grafico.Categorias[i], nombreDimension, serieDelante: false, excluir: Otros);
        return grafico;
    }

    private static GraficoBarras BarrasPorSite(Contexto c, string id, string titulo, Indicador indicador, Func<FilaCuadro, string> dimension,
        int maximo, string nombreDimension, bool gruesas = false, string? area = null) =>
        Barras(c, id, titulo, indicador, dimension, maximo, c.SeriesSiteClaves, f => f.Site, ordenarPorVolumen: false, gruesas, area, nombreDimension);

    // ───────────────────────── Frases de lectura (pie de tarjeta) ─────────────────────────

    private static class Lecturas
    {
        /// <summary>Diferencia con su unidad: «2,71 puntos», «106,15 s», «1.234 llamadas».</summary>
        private static string Diferencia(double d, TipoValor tipo) => tipo switch
        {
            TipoValor.Porcentaje => $"{Formato.Decimal(Math.Abs(d))} puntos",
            TipoValor.Segundos => $"{Formato.Decimal(Math.Abs(d))} s",
            _ => $"{Formato.Entero(Math.Abs(d))} llamadas",
        };

        /// <summary>El último periodo con datos; con dos series, cuál va por encima y por cuánto.</summary>
        public static string? UltimoPeriodo(GraficoLineas g)
        {
            for (var i = g.Categorias.Count - 1; i >= 0; i--)
            {
                var conValor = g.Series.Where(s => s.Valores[i] is not null).ToList();
                if (conValor.Count == 0) continue;
                var periodo = g.Categorias[i];
                if (conValor.Count == 2)
                {
                    double va = conValor[0].Valores[i]!.Value, vb = conValor[1].Valores[i]!.Value;
                    var (mayor, menor) = va >= vb ? (conValor[0], conValor[1]) : (conValor[1], conValor[0]);
                    if (Math.Abs(va - vb) < 0.005)
                        return $"En la {periodo.ToLowerInvariant()}, {conValor[0].Nombre} y {conValor[1].Nombre} quedan igual ({Formato.Valor(va, g.Tipo)}).";
                    return $"En la {periodo.ToLowerInvariant()}, {conValor[0].Nombre} marca {Formato.Valor(va, g.Tipo)} y {conValor[1].Nombre} " +
                           $"{Formato.Valor(vb, g.Tipo)}: {mayor.Nombre} va {Diferencia(va - vb, g.Tipo)} por encima.";
                }
                return $"En la {periodo.ToLowerInvariant()}: " +
                       string.Join(" · ", conValor.Select(s => $"{s.Nombre} {Formato.Valor(s.Valores[i], g.Tipo)}")) + ".";
            }
            return null;
        }

        /// <summary>
        /// El valor más alto y el más bajo de la gráfica, con su serie y su categoría: «YOIGO en Medellín» (la serie
        /// delante, en las columnas por marca) o «MYFIJO en Medellín» (la categoría delante, en barras y días).
        /// </summary>
        public static string? Extremos(Grafico g, Func<int, string> nombreCategoria, string queEs, bool serieDelante, string? excluir = null)
        {
            var celdas = (
                from s in g.Series
                from i in Enumerable.Range(0, g.Categorias.Count)
                where s.Valores[i] is not null && g.Categorias[i] != excluir
                select (Serie: s.Nombre, Categoria: nombreCategoria(i), Valor: s.Valores[i]!.Value)).ToList();
            if (celdas.Count < 2) return null;
            var maximo = celdas.MaxBy(c => c.Valor);
            var minimo = celdas.MinBy(c => c.Valor);
            string Donde((string Serie, string Categoria, double Valor) c) =>
                serieDelante ? $"{c.Serie} en {c.Categoria}" : $"{c.Categoria} en {c.Serie}";
            var (alto, bajo) = string.IsNullOrEmpty(queEs) ? ("Lo más alto", "lo más bajo") : ($"El {queEs} más alto", "el más bajo");
            return $"{alto}: {Donde(maximo)} ({Formato.Valor(maximo.Valor, g.Tipo)}); " +
                   $"{bajo}, {Donde(minimo)} ({Formato.Valor(minimo.Valor, g.Tipo)}).";
        }

        /// <summary>Cuánto del total reúne la primera categoría (supervisores ordenados por llamadas).</summary>
        public static string? Reparto(IReadOnlyList<string> orden, IReadOnlyDictionary<string, Acumulado> totales, long total)
        {
            if (orden.Count == 0 || total <= 0) return null;
            var primero = orden[0];
            var suyas = totales[primero].Atendidas;
            return $"{primero} reúne {Formato.Entero(suyas)} atendidas, el {Formato.Porcentaje(100.0 * suyas / total, 1)} del total.";
        }
    }

    // ───────────────────────── Utilidades ─────────────────────────

    /// <summary>Los sites toman los dos primeros tonos de la paleta de la marca: Medellín morado y Bogotá magenta.</summary>
    private static string ClaseSite(string site) => site switch
    {
        Medellin => "serie-a",
        Bogota => "serie-b",
        _ => "serie-c",
    };

    /// <summary>Cuando cada serie es una marca, cada una lleva el color de su marca (guía, 2.2).</summary>
    private static string ClaseMarca(string marca) => marca switch
    {
        "YOIGO" => "serie-yoigo",
        "MASMOVIL" => "serie-masmovil",
        _ => "serie-e",
    };

    private static List<string> Ordenar(IEnumerable<string> valores, string[] orden) =>
        valores
            .OrderBy(v => Array.IndexOf(orden, v) is var i && i >= 0 ? i : int.MaxValue)
            .ThenBy(v => v, Alfabetico)
            .ToList();

    private static DateOnly Limitar(DateOnly fecha, InstantaneaCuadro datos) =>
        fecha < datos.PrimerDia ? datos.PrimerDia : fecha > datos.UltimoDia ? datos.UltimoDia : fecha;

    private static DateOnly Mayor(DateOnly a, DateOnly b) => a > b ? a : b;
    private static DateOnly Menor(DateOnly a, DateOnly b) => a < b ? a : b;
}
