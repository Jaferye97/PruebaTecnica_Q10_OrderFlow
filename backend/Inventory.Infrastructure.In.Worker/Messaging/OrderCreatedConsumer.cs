using System.Text.Json;
using Inventory.Application.Stocks;
using Inventory.Infrastructure.Out.RabbitMq;
using OrderFlow.Contracts;
using Topology = OrderFlow.Contracts.Messaging;
using RabbitMQ.Client.Events;

namespace Inventory.Infrastructure.In.Worker.Messaging;

/// <summary>Consume inventory.order-created: reserva stock y publica StockReserved/StockRejected.</summary>
internal sealed class OrderCreatedConsumer(
    RabbitMqConnectionProvider connections,
    IServiceScopeFactory scopes,
    ILogger<OrderCreatedConsumer> logger) : QueueConsumer(connections, scopes, logger)
{
    protected override string Queue => RabbitMqTopology.OrderCreatedQueue;
    protected override string BindingKey => Topology.RoutingKeys.OrderCreated;

    protected override async Task<MessageOutcome> HandleAsync(
        IServiceProvider services, BasicDeliverEventArgs delivery, CancellationToken ct)
    {
        // Type es opcional (p. ej. un mensaje publicado a mano), pero si viene debe ser OrderCreated
        var type = delivery.BasicProperties.Type;
        if (!string.IsNullOrEmpty(type) && type != nameof(OrderCreated))
        {
            logger.LogError("Tipo de mensaje desconocido '{Type}' en {Queue}; a la DLQ", type, Queue);
            return MessageOutcome.DeadLetter;
        }

        // JsonException → la base lo manda a la DLQ
        var evt = JsonSerializer.Deserialize<OrderCreated>(delivery.Body.Span, Topology.Json);
        if (evt is null || evt.EventId == Guid.Empty || evt.OrderId == Guid.Empty || string.IsNullOrWhiteSpace(evt.Sku))
        {
            logger.LogError("OrderCreated incompleto (MessageId {MessageId}); a la DLQ", delivery.BasicProperties.MessageId);
            return MessageOutcome.DeadLetter;
        }

        var handler = services.GetRequiredService<ReserveStockHandler>();
        var outcome = await handler.HandleAsync(
            new ReserveStockCommand(evt.EventId, evt.OrderId, evt.Sku, evt.Cantidad), ct);

        logger.LogInformation(
            "Pedido {OrderId}: {Resultado} {Motivo}{Duplicado}",
            evt.OrderId, outcome.Resultado, outcome.Motivo ?? "", outcome.EsDuplicado ? " (duplicado, se republica)" : "");

        return MessageOutcome.Ack;
    }
}
