namespace WebApplication1.Models;

/// <summary>
/// Suma de las medidas de varias filas del resumen y cálculo de los indicadores.
/// Las fórmulas están explicadas también en Consultas/Agregado.sql.
/// </summary>
public sealed class Acumulado
{
    public long Atendidas { get; private set; }
    public long QRell { get; private set; }
    public double SegundosAtencion { get; private set; }
    public long NoSolucion { get; private set; }
    public long BaseNoSolucion { get; private set; }
    public long Rell24 { get; private set; }
    public long Rell72 { get; private set; }
    public long Transferencias { get; private set; }

    public void Sumar(FilaCuadro fila)
    {
        Atendidas += fila.Atendidas;
        QRell += fila.QRell;
        SegundosAtencion += fila.SegundosAtencion;
        NoSolucion += fila.NoSolucion;
        BaseNoSolucion += fila.BaseNoSolucion;
        Rell24 += fila.Rell24;
        Rell72 += fila.Rell72;
        Transferencias += fila.Transferencias;
    }

    public double? Valor(Indicador indicador) => indicador switch
    {
        Indicador.Atendidas => Atendidas,
        Indicador.Tmo => Atendidas > 0 ? SegundosAtencion / Atendidas : null,
        Indicador.NoSolucion => Porcentaje(NoSolucion, BaseNoSolucion),
        Indicador.Rell24 => Porcentaje(Rell24, Atendidas),
        Indicador.Rell72 => Porcentaje(Rell72, QRell),
        Indicador.Transferencia => Porcentaje(Transferencias, Atendidas),
        _ => null,
    };

    private static double? Porcentaje(long parte, long total) => total > 0 ? 100.0 * parte / total : null;

    public static Acumulado De(IEnumerable<FilaCuadro> filas)
    {
        var acumulado = new Acumulado();
        foreach (var fila in filas) acumulado.Sumar(fila);
        return acumulado;
    }
}

public static class ExtensionesAcumulado
{
    /// <summary>Agrupa las filas por la clave y suma sus medidas.</summary>
    public static Dictionary<TClave, Acumulado> Agrupar<TClave>(this IEnumerable<FilaCuadro> filas, Func<FilaCuadro, TClave> clave)
        where TClave : notnull
    {
        var grupos = new Dictionary<TClave, Acumulado>();
        foreach (var fila in filas)
        {
            var k = clave(fila);
            if (!grupos.TryGetValue(k, out var acumulado)) grupos[k] = acumulado = new Acumulado();
            acumulado.Sumar(fila);
        }
        return grupos;
    }
}
