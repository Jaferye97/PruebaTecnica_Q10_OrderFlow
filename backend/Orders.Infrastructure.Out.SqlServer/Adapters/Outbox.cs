using System.Text.Json;
using OrderFlow.Contracts;
using Orders.Application.Abstractions;
using Orders.Infrastructure.Out.SqlServer.Persistence;

namespace Orders.Infrastructure.Out.SqlServer.Adapters;

internal sealed class Outbox(OrdersDbContext db) : IOutbox
{
    // Mismo DbContext que el pedido: entra en el mismo SaveChanges (misma transacción)
    public void Add(IIntegrationEvent integrationEvent) =>
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = integrationEvent.EventId,
            Type = Messaging.TypeName(integrationEvent),
            Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), Messaging.Json),
            OcurridoEn = integrationEvent.OcurridoEn
        });
}
