namespace Inventory.Application.Abstractions;

/// <summary>
/// Publica eventos de integración hacia otros servicios (RabbitMQ con publisher confirms).
/// Solo retorna cuando el broker confirmó el mensaje; si no, lanza excepción.
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct) where TEvent : IIntegrationEvent;
}
