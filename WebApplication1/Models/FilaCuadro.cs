namespace WebApplication1.Models;

/// <summary>
/// Una fila del resumen diario que llega de BigQuery (Consultas/Agregado.sql): las llamadas de un día
/// agrupadas por site, sector, supervisor, agente, marca, servicio y detallado.
/// </summary>
public sealed class FilaCuadro
{
    public required DateOnly Fecha { get; init; }

    /// <summary>Semana ISO del día (36 = «Semana 36»).</summary>
    public required int Semana { get; init; }

    /// <summary>Lunes de esa semana: sirve para ordenar las semanas aunque cambie el año.</summary>
    public required DateOnly Lunes { get; init; }

    public required string Site { get; init; }
    public required string Sector { get; init; }
    public required string Supervisor { get; init; }
    public required string Agente { get; init; }
    public required string Marca { get; init; }
    public required string Servicio { get; init; }
    public required string Detallado { get; init; }

    /// <summary>Registros de la consulta de detalle que forman esta fila.</summary>
    public long Filas { get; init; }

    public long Atendidas { get; init; }
    public long QRell { get; init; }
    public double SegundosAtencion { get; init; }
    public long NoSolucion { get; init; }

    /// <summary>Llamadas con no_solucion_kpi informado: la base del % No solución.</summary>
    public long BaseNoSolucion { get; init; }
    public long Rell24 { get; init; }
    public long Rell72 { get; init; }
    public long Transferencias { get; init; }
}
