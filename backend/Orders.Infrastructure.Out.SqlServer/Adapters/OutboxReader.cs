using Microsoft.EntityFrameworkCore;
using Orders.Application.Abstractions;
using Orders.Infrastructure.Out.SqlServer.Persistence;
using Orders.Infrastructure.Out.SqlServer.Persistence.Configurations;

namespace Orders.Infrastructure.Out.SqlServer.Adapters;

internal sealed class OutboxReader(OrdersDbContext db) : IOutboxReader
{
    // Usa el índice filtrado IX_OutboxMessages_Pendientes
    public async Task<IReadOnlyList<PendingOutboxMessage>> GetPendingAsync(int max, CancellationToken ct) =>
        await db.OutboxMessages
            .AsNoTracking()
            .Where(m => m.ProcesadoEn == null)
            .OrderBy(m => m.OcurridoEn)
            .Take(max)
            .Select(m => new PendingOutboxMessage(m.Id, m.Type, m.Payload))
            .ToListAsync(ct);

    // UPDATE directo, sin cargar la entidad
    public Task MarkProcessedAsync(Guid id, DateTimeOffset procesadoEn, CancellationToken ct) =>
        db.OutboxMessages
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.ProcesadoEn, procesadoEn), ct);

    public Task MarkFailedAsync(Guid id, string error, CancellationToken ct)
    {
        var truncated = error.Length <= OutboxMessageConfiguration.UltimoErrorMaxLength
            ? error
            : error[..OutboxMessageConfiguration.UltimoErrorMaxLength];

        return db.OutboxMessages
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Intentos, m => m.Intentos + 1)
                .SetProperty(m => m.UltimoError, truncated), ct);
    }
}
