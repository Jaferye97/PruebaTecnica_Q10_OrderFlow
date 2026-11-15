using OrderFlow.Contracts;
using Orders.Domain.Orders;
using Topology = OrderFlow.Contracts.Messaging;

namespace Orders.Infrastructure.In.WebApi.Messaging;

/// <summary>Consume orders.stock-results: confirma o rechaza el pedido según la respuesta de Inventory.</summary>
internal sealed class StockResultsConsumer(
    RabbitMqConnectionProvider connections,
    IServiceScopeFactory scopes,
    ILogger<StockResultsConsumer> logger) : QueueConsumer(connections, scopes, logger)
{
    protected override string Queue => RabbitMqTopology.StockResultsQueue;
    protected override string BindingKey => Topology.RoutingKeys.AllStockResults;

    protected override async Task<MessageOutcome> HandleAsync(
        IServiceProvider services, BasicDeliverEventArgs delivery, CancellationToken ct)
    {
        // La propiedad Type decide cómo deserializar. JsonException → la base lo manda a la DLQ
        var type = delivery.BasicProperties.Type;
        var (orderId, result) = type switch
        {
            nameof(StockReserved) => await ConfirmAsync(services, delivery, ct),
            nameof(StockRejected) => await RejectAsync(services, delivery, ct),
            _ => (Guid.Empty, null)
        };

        if (result is null)
        {
            logger.LogError("Mensaje inválido en {Queue}: tipo '{Type}', MessageId {MessageId}; a la DLQ",
                Queue, type, delivery.BasicProperties.MessageId);
            return MessageOutcome.DeadLetter;
        }

        if (result.IsSuccess)
        {
            logger.LogInformation("Pedido {OrderId} → {Estado}", orderId, result.Value.Estado);
            return MessageOutcome.Ack;
        }

        // Pedido inexistente: reintentar no lo va a crear → DLQ para revisarlo
        if (result.Errors.Any(e => e.Code == OrderErrors.NoEncontradoCode))
        {
            logger.LogError("{Type} para un pedido inexistente {OrderId}; a la DLQ", type, orderId);
            return MessageOutcome.DeadLetter;
        }

        // Fallo de dominio (p. ej. transición inválida por un duplicado): reintentar no lo arregla
        logger.LogWarning("{Type} ignorado para el pedido {OrderId}: {Errores}",
            type, orderId, string.Join("; ", result.Errors.Select(e => e.Message)));
        return MessageOutcome.Ack;
    }

    private static async Task<(Guid, Result<OrderDto>?)> ConfirmAsync(
        IServiceProvider services, BasicDeliverEventArgs delivery, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<StockReserved>(delivery.Body.Span, Topology.Json);
        if (evt is null || evt.OrderId == Guid.Empty)
            return (Guid.Empty, null);

        var handler = services.GetRequiredService<ConfirmOrderHandler>();
        return (evt.OrderId, await handler.HandleAsync(new ConfirmOrderCommand(evt.OrderId), ct));
    }

    private static async Task<(Guid, Result<OrderDto>?)> RejectAsync(
        IServiceProvider services, BasicDeliverEventArgs delivery, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<StockRejected>(delivery.Body.Span, Topology.Json);
        if (evt is null || evt.OrderId == Guid.Empty)
            return (Guid.Empty, null);

        var handler = services.GetRequiredService<RejectOrderHandler>();
        return (evt.OrderId, await handler.HandleAsync(new RejectOrderCommand(evt.OrderId, evt.Motivo, evt.Detalle), ct));
    }
}
