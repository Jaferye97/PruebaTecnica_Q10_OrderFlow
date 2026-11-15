using System.Text;

namespace Inventory.Infrastructure.Out.RabbitMq;

/// <summary>
/// Un canal con publisher confirms: BasicPublishAsync espera el ack del broker y lanza
/// excepción si hay nack o si el mensaje no llega a ninguna cola (mandatory).
/// Sin confirms, "publicado" solo significaría "salió de mi proceso".
/// </summary>
public sealed class RabbitMqPublisher(RabbitMqConnectionProvider connections) : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IChannel? _channel;

    public async Task PublishAsync(Guid messageId, string type, Guid correlationId, string payload, CancellationToken ct)
    {
        var properties = new BasicProperties
        {
            MessageId = messageId.ToString(),
            Type = type,
            CorrelationId = correlationId.ToString(),
            ContentType = "application/json",
            Persistent = true
        };

        await _lock.WaitAsync(ct);
        try
        {
            var channel = await GetChannelAsync(ct);

            // mandatory: si nadie tiene la cola enlazada todavía, el broker lo devuelve y aquí
            // se lanza PublishReturnException; el OrderCreated no recibe ack y se reentrega.
            await channel.BasicPublishAsync(
                Messaging.Exchange,
                Messaging.RoutingKeyFor(type),
                mandatory: true,
                properties,
                Encoding.UTF8.GetBytes(payload),
                ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        if (_channel is not null)
            await _channel.DisposeAsync();

        var connection = await connections.GetConnectionAsync(ct);
        _channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true),
            ct);

        await RabbitMqTopology.DeclareExchangesAsync(_channel, ct);
        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
        _lock.Dispose();
    }
}
