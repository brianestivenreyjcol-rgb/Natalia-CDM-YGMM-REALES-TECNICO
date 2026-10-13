using System.Text;
using WebApplication1.Models;

namespace WebApplication1.Infraestructura;

/// <summary>Escala del eje Y con marcas redondas.</summary>
public readonly record struct Escala(double Min, double Max, double Paso)
{
    public IEnumerable<double> Marcas()
    {
        for (var v = Min; v <= Max + Paso / 2; v += Paso) yield return Math.Round(v, 6);
    }

    /// <summary>Posición del valor entre 0 (abajo) y 1 (arriba).</summary>
    public double Fraccion(double valor) => Max > Min ? (valor - Min) / (Max - Min) : 0.5;

    /// <summary>
    /// Desde cero (volúmenes) o ajustada al rango de los datos con margen para las pastillas
    /// de los puntos más altos y más bajos.
    /// </summary>
    public static Escala Calcular(IReadOnlyCollection<double> valores, bool desdeCero, bool porcentaje, int marcas = 4)
    {
        if (valores.Count == 0) return new Escala(0, 1, 0.25);
        double minimo = valores.Min(), maximo = valores.Max();
        double bajo, alto;
        if (desdeCero)
        {
            bajo = 0;
            alto = maximo <= 0 ? 1 : maximo * 1.15;
        }
        else
        {
            var margen = Math.Max((maximo - minimo) * 0.2, Math.Max(Math.Abs(maximo) * 0.03, 0.5));
            bajo = minimo - margen;
            alto = maximo + margen;
        }

        var paso = Redondo((alto - bajo) / marcas);
        var min = Math.Floor(bajo / paso) * paso;
        var max = Math.Ceiling(alto / paso) * paso;
        if (min < 0 && minimo >= 0) min = 0;
        if (porcentaje && max > 100 && maximo <= 100) max = Math.Ceiling(100 / paso) * paso;
        return new Escala(min, max, paso);
    }

    private static double Redondo(double valor)
    {
        if (valor <= 0) return 1;
        var potencia = Math.Pow(10, Math.Floor(Math.Log10(valor)));
        var f = valor / potencia;
        var n = f <= 1 ? 1 : f <= 2 ? 2 : f <= 2.5 ? 2.5 : f <= 5 ? 5 : 10;
        return n * potencia;
    }
}

/// <summary>Trazos SVG y reglas de rotulado de las gráficas (coordenadas de 0 a 1000).</summary>
public static class Graficas
{
    /// <summary>Tramos seguidos de valores sin hueco (un null corta la línea).</summary>
    public static IEnumerable<List<(int Indice, double Valor)>> Tramos(IReadOnlyList<double?> valores)
    {
        var tramo = new List<(int, double)>();
        for (var i = 0; i < valores.Count; i++)
        {
            if (valores[i] is { } v) { tramo.Add((i, v)); continue; }
            if (tramo.Count > 0) { yield return tramo; tramo = []; }
        }
        if (tramo.Count > 0) yield return tramo;
    }

    /// <summary>Línea suavizada (Catmull-Rom a Bézier) que no se pasa del valor de sus extremos.</summary>
    public static string Suave(IReadOnlyList<(double X, double Y)> p)
    {
        if (p.Count == 0) return "";
        var sb = new StringBuilder();
        sb.Append('M').Append(Formato.Coord(p[0].X)).Append(',').Append(Formato.Coord(p[0].Y));
        for (var i = 0; i < p.Count - 1; i++)
        {
            var p0 = p[Math.Max(i - 1, 0)];
            var p1 = p[i];
            var p2 = p[i + 1];
            var p3 = p[Math.Min(i + 2, p.Count - 1)];
            double bajo = Math.Min(p1.Y, p2.Y), alto = Math.Max(p1.Y, p2.Y);
            var c1x = p1.X + (p2.X - p0.X) / 6;
            var c1y = Math.Clamp(p1.Y + (p2.Y - p0.Y) / 6, bajo, alto);
            var c2x = p2.X - (p3.X - p1.X) / 6;
            var c2y = Math.Clamp(p2.Y - (p3.Y - p1.Y) / 6, bajo, alto);
            sb.Append(" C").Append(Formato.Coord(c1x)).Append(',').Append(Formato.Coord(c1y))
              .Append(' ').Append(Formato.Coord(c2x)).Append(',').Append(Formato.Coord(c2y))
              .Append(' ').Append(Formato.Coord(p2.X)).Append(',').Append(Formato.Coord(p2.Y));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Área bajo la línea suavizada, cerrada contra el suelo de la línea (<paramref name="suelo"/>, de 0 a 1000:
    /// con volumen, la línea ocupa la parte de arriba del plano y el degradado no baja hasta las columnas).
    /// </summary>
    public static string Area(IReadOnlyList<(double X, double Y)> p, double suelo = 1000) =>
        p.Count < 2 ? "" : $"{Suave(p)} L{Formato.Coord(p[^1].X)},{Formato.Coord(suelo)} L{Formato.Coord(p[0].X)},{Formato.Coord(suelo)} Z";

    /// <summary>
    /// Qué puntos llevan pastilla: todos hasta 12 periodos; con más, uno de cada pocos (unas diez
    /// pastillas por serie), siempre el primero y el último.
    /// </summary>
    public static bool[] Rotulados(int n)
    {
        var rotulados = new bool[n];
        var paso = n <= 12 ? 1 : n <= 24 ? 2 : (int)Math.Ceiling(n / 10.0);
        for (var i = 0; i < n; i++) rotulados[i] = i % paso == 0;
        if (n > 0) rotulados[n - 1] = true;
        if (paso > 1 && n > 2 && rotulados[n - 2] && n - 2 != 0) rotulados[n - 2] = false;
        return rotulados;
    }

    /// <summary>Etiquetas del eje X que se escriben: todas si caben; si no, una de cada pocas y siempre la última.</summary>
    public static bool[] EtiquetasVisibles(IReadOnlyList<string> etiquetas)
    {
        var n = etiquetas.Count;
        var visibles = new bool[n];
        if (n == 0) return visibles;
        var largo = etiquetas.Max(e => e.Length) + 2;
        var paso = Math.Max(1, (int)Math.Ceiling(n * largo / 120.0));
        for (var i = 0; i < n; i++) visibles[i] = i % paso == 0;
        visibles[n - 1] = true;
        if (paso > 1 && n > 2 && visibles[n - 2]) visibles[n - 2] = false;
        return visibles;
    }

    /// <summary>
    /// En cada periodo, la serie de valor más alto pone su pastilla encima del punto, la siguiente
    /// debajo, la tercera encima… para que no se pisen.
    /// </summary>
    public static bool PastillaArriba(IReadOnlyList<SerieGrafico> series, int serie, int indice)
    {
        if (series[serie].Valores[indice] is not { } valor) return true;
        var puesto = 0;
        for (var s = 0; s < series.Count; s++)
        {
            if (s == serie || series[s].Valores[indice] is not { } otro) continue;
            if (otro > valor || (otro == valor && s < serie)) puesto++;
        }
        return puesto % 2 == 0;
    }

    /// <summary>Texto de la ficha: «Cabecera|serie-x~Nombre · valor|…|Atendidas · n» (site.js la pinta).</summary>
    public static string Ficha(Grafico grafico, int indice)
    {
        var cabecera = grafico.Fichas?[indice] ?? grafico.Categorias[indice];
        var lineas = grafico.Series.Select(s => $"{s.Clase}~{s.Nombre} · {Formato.Valor(s.Valores[indice], grafico.Tipo)}").ToList();
        if (grafico is GraficoLineas { Volumen: { } volumen } && volumen[indice] is { } atendidas)
            lineas.Add($"Atendidas · {Formato.Entero(atendidas)}");
        return string.Join('|', lineas.Prepend(cabecera));
    }
}
