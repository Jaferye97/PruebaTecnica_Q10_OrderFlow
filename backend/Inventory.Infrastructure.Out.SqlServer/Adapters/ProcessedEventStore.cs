namespace Inventory.Infrastructure.Out.SqlServer.Adapters;

internal sealed class ProcessedEventStore(InventoryDbContext db) : IProcessedEventStore
{
    // Solo lectura: si existe, se devuelve tal cual
    public Task<ProcessedEvent?> FindAsync(Guid eventId, CancellationToken ct) =>
        db.ProcessedEvents.AsNoTracking().SingleOrDefaultAsync(e => e.EventId == eventId, ct);

    public void Add(ProcessedEvent processedEvent) => db.ProcessedEvents.Add(processedEvent);
}
