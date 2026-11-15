namespace Orders.Infrastructure.Out.SqlServer.Persistence;

/// <summary>Fila del Outbox: un evento de integración pendiente de publicar.</summary>
public sealed class OutboxMessage
{
    /// <summary>= EventId del evento (se publica como MessageId).</summary>
    public Guid Id { get; set; }

    /// <summary>Nombre del evento (p. ej. OrderCreated); se publica como propiedad Type.</summary>
    public string Type { get; set; } = null!;

    public string Payload { get; set; } = null!;
    public DateTimeOffset OcurridoEn { get; set; }
    public DateTimeOffset? ProcesadoEn { get; set; }
    public int Intentos { get; set; }
    public string? UltimoError { get; set; }
}
