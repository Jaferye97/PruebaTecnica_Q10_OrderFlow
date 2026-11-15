using System.Text.Json;
using Orders.Application.Abstractions;

namespace Orders.Infrastructure.Out.RabbitMq;

/// <summary>Adaptador de IMessagePublisher: publica el payload del Outbox sin re-serializarlo.</summary>
internal sealed class OutboxMessagePublisher(RabbitMqPublisher publisher) : IMessagePublisher
{
    public Task PublishAsync(PendingOutboxMessage message, CancellationToken ct) =>
        publisher.PublishAsync(message.Id, message.Type, ReadOrderId(message.Payload), message.Payload, ct);

    // Todos los contratos llevan OrderId (IIntegrationEvent); se usa como CorrelationId
    private static Guid ReadOrderId(string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        return doc.RootElement.GetProperty("orderId").GetGuid();
    }
}
