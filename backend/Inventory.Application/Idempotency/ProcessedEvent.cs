namespace Inventory.Application.Idempotency;

/// <summary>
/// Evento ya procesado. Si el mismo mensaje llega otra vez, se responde con el mismo
/// resultado (y el mismo ResultEventId) sin volver a tocar el stock.
/// </summary>
public sealed class ProcessedEvent
{
    public Guid EventId { get; private set; }
    public Guid OrderId { get; private set; }
    public ProcessingResult Resultado { get; private set; }
    public string? Motivo { get; private set; }
    public string? Detalle { get; private set; }

    /// <summary>Id del evento de respuesta publicado; se reutiliza al republicar un duplicado.</summary>
    public Guid ResultEventId { get; private set; }

    public DateTimeOffset ProcesadoEn { get; private set; }

    // Para EF Core (materialización)
    private ProcessedEvent() { }

    private ProcessedEvent(
        Guid eventId, Guid orderId, ProcessingResult resultado,
        string? motivo, string? detalle, DateTimeOffset procesadoEn)
    {
        EventId = eventId;
        OrderId = orderId;
        Resultado = resultado;
        Motivo = motivo;
        Detalle = detalle;
        ResultEventId = Guid.CreateVersion7();
        ProcesadoEn = procesadoEn;
    }

    public static ProcessedEvent Reserved(Guid eventId, Guid orderId, DateTimeOffset procesadoEn) =>
        new(eventId, orderId, ProcessingResult.Reserved, motivo: null, detalle: null, procesadoEn);

    public static ProcessedEvent Rejected(
        Guid eventId, Guid orderId, string motivo, string? detalle, DateTimeOffset procesadoEn) =>
        new(eventId, orderId, ProcessingResult.Rejected, motivo, detalle, procesadoEn);
}
