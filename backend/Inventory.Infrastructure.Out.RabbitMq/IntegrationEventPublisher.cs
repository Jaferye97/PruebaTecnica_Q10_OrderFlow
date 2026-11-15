using System.Text.Json;
using Inventory.Application.Abstractions;
using OrderFlow.Contracts;

namespace Inventory.Infrastructure.Out.RabbitMq;

internal sealed class IntegrationEventPublisher(RabbitMqPublisher publisher) : IIntegrationEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct) where TEvent : IIntegrationEvent =>
        publisher.PublishAsync(
            messageId: integrationEvent.EventId,
            type: Messaging.TypeName(integrationEvent),
            correlationId: integrationEvent.OrderId,
            payload: JsonSerializer.Serialize(integrationEvent, Messaging.Json),
            ct);
}
