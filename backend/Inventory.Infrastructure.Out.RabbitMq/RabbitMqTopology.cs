namespace Inventory.Infrastructure.Out.RabbitMq;

/// <summary>
/// Cada servicio declara al arrancar lo que usa. Las declaraciones son idempotentes
/// mientras los argumentos sean iguales (si cambian, RabbitMQ responde PRECONDITION_FAILED).
/// </summary>
public static class RabbitMqTopology
{
    public const string OrderCreatedQueue = "inventory.order-created";

    public const int DeliveryLimit = 5;

    public static async Task DeclareExchangesAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(Messaging.Exchange, ExchangeType.Topic,
            durable: true, autoDelete: false, cancellationToken: ct);

        // Direct: cada cola muerta llega con su propio nombre como routing key (ver abajo)
        await channel.ExchangeDeclareAsync(Messaging.DeadLetterExchange, ExchangeType.Direct,
            durable: true, autoDelete: false, cancellationToken: ct);
    }

    /// <summary>Cola quorum + su DLQ. Al 5.º intento fallido el mensaje pasa solo a la DLQ.</summary>
    public static async Task DeclareQueueAsync(IChannel channel, string queue, string bindingKey, CancellationToken ct)
    {
        await DeclareExchangesAsync(channel, ct);

        var deadLetterQueue = $"{queue}.dlq";

        await channel.QueueDeclareAsync(deadLetterQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" },
            cancellationToken: ct);
        await channel.QueueBindAsync(deadLetterQueue, Messaging.DeadLetterExchange, routingKey: queue,
            cancellationToken: ct);

        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-queue-type"] = "quorum",
                ["x-delivery-limit"] = DeliveryLimit,
                ["x-dead-letter-exchange"] = Messaging.DeadLetterExchange,
                // Routing key fija = nombre de la cola: la DLQ correcta sin depender de la key original
                ["x-dead-letter-routing-key"] = queue
            },
            cancellationToken: ct);
        await channel.QueueBindAsync(queue, Messaging.Exchange, bindingKey, cancellationToken: ct);
    }
}
