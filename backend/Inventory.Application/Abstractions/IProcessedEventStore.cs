namespace Inventory.Application.Abstractions;

/// <summary>Registro de eventos ya procesados (idempotencia del consumidor).</summary>
public interface IProcessedEventStore
{
    Task<ProcessedEvent?> FindAsync(Guid eventId, CancellationToken ct);

    /// <summary>Se persiste en el mismo SaveChanges que la reserva de stock.</summary>
    void Add(ProcessedEvent processedEvent);
}
