using System.Text.Json;
using Orders.Application.Common;
using Orders.Infrastructure.Out.RabbitMq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Orders.Infrastructure.In.WebApi.Messaging;

public enum MessageOutcome
{
    /// <summary>Éxito, o fallo de dominio que reintentar no arregla (el derivado loguea el warning).</summary>
    Ack,

    /// <summary>Mensaje inválido (tipo desconocido, datos faltantes...): directo a la DLQ.</summary>
    DeadLetter
}

/// <summary>
/// Consumidor base con la política de ack/nack común:
/// <list type="bullet">
/// <item>Ack → ack. DeadLetter o JSON inválido → nack(requeue: false) → DLQ.</item>
/// <item>PersistenceConflictException → hasta 3 intentos, cada uno en un scope nuevo; si sigue, nack(requeue: true).</item>
/// <item>Cualquier otro error → nack(requeue: true). El x-delivery-limit lo lleva a la DLQ al 5.º intento.</item>
/// </list>
/// Prefetch 10 y procesamiento secuencial (ConsumerDispatchConcurrency = 1, el valor por defecto).
/// </summary>
public abstract class QueueConsumer(
    RabbitMqConnectionProvider connections,
    IServiceScopeFactory scopes,
    ILogger logger) : BackgroundService
{
    private const ushort Prefetch = 10;
    private const int MaxConflictAttempts = 3;
    private static readonly TimeSpan ConnectRetryDelay = TimeSpan.FromSeconds(5);

    private IChannel? _channel;

    protected abstract string Queue { get; }
    protected abstract string BindingKey { get; }

    protected abstract Task<MessageOutcome> HandleAsync(
        IServiceProvider services, BasicDeliverEventArgs delivery, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _channel = await OpenChannelAsync(stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += (_, delivery) => OnReceivedAsync(_channel, delivery, stoppingToken);

            await _channel.BasicConsumeAsync(Queue, autoAck: false, consumer, stoppingToken);
            logger.LogInformation("Consumiendo {Queue}", Queue);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Apagado normal
        }
    }

    // Si el broker no está al arrancar, se sigue intentando (el provider ya hace backoff por llamada)
    private async Task<IChannel> OpenChannelAsync(CancellationToken ct)
    {
        while (true)
        {
            try
            {
                var connection = await connections.GetConnectionAsync(ct);
                var channel = await connection.CreateChannelAsync(cancellationToken: ct);
                await RabbitMqTopology.DeclareQueueAsync(channel, Queue, BindingKey, ct);
                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: Prefetch, global: false, ct);
                return channel;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "No se pudo preparar {Queue}; reintento en {Delay}s", Queue, ConnectRetryDelay.TotalSeconds);
                await Task.Delay(ConnectRetryDelay, ct);
            }
        }
    }

    private async Task OnReceivedAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken ct)
    {
        var messageId = delivery.BasicProperties.MessageId;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                // Scope nuevo por intento: DbContext limpio
                await using var scope = scopes.CreateAsyncScope();
                var outcome = await HandleAsync(scope.ServiceProvider, delivery, ct);

                if (outcome == MessageOutcome.Ack)
                    await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, ct);
                else
                    await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, ct);
                return;
            }
            catch (PersistenceConflictException ex) when (attempt < MaxConflictAttempts)
            {
                logger.LogWarning(ex, "Conflicto al persistir {MessageId} (intento {Attempt}/{Max}); reintento",
                    messageId, attempt, MaxConflictAttempts);
            }
            catch (PersistenceConflictException ex)
            {
                logger.LogWarning(ex, "Conflicto persistente en {MessageId}; se devuelve a la cola", messageId);
                await NackAsync(channel, delivery, requeue: true);
                return;
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "JSON inválido en {MessageId}; a la DLQ", messageId);
                await NackAsync(channel, delivery, requeue: false);
                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Apagando: sin ack, RabbitMQ lo reentrega al cerrar el canal
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error procesando {MessageId}; se devuelve a la cola", messageId);
                await NackAsync(channel, delivery, requeue: true);
                return;
            }
        }
    }

    private async Task NackAsync(IChannel channel, BasicDeliverEventArgs delivery, bool requeue)
    {
        try
        {
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue);
        }
        catch (Exception ex)
        {
            // Canal caído: el mensaje sin ack se reentrega solo
            logger.LogWarning(ex, "No se pudo hacer nack de {MessageId}", delivery.BasicProperties.MessageId);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
        }
    }
}
