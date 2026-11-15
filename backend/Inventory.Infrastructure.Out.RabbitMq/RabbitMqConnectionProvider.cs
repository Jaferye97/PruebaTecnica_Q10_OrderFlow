using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Inventory.Infrastructure.Out.RabbitMq;

/// <summary>
/// Conexión única (singleton) creada de forma perezosa y asíncrona: en la v7 todo es async
/// y el contenedor de DI no puede construir objetos async.
/// </summary>
public sealed class RabbitMqConnectionProvider(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConnectionProvider> logger) : IAsyncDisposable
{
    private const string ClientName = "inventory-worker";
    private const int MaxAttempts = 5;
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetConnectionAsync(CancellationToken ct)
    {
        if (_connection is not null)
            return _connection;

        await _lock.WaitAsync(ct);
        try
        {
            // Otro hilo pudo crearla mientras esperábamos el semáforo
            if (_connection is not null)
                return _connection;

            _connection = await ConnectWithRetryAsync(ct);
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    // La recuperación automática de la v7 solo actúa DESPUÉS de la primera conexión:
    // si el broker aún no está listo al arrancar, el primer intento lo reintentamos nosotros.
    private async Task<IConnection> ConnectWithRetryAsync(CancellationToken ct)
    {
        var o = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = o.Host,
            Port = o.Port,
            UserName = o.User,
            Password = o.Password,
            VirtualHost = o.VirtualHost,
            ClientProvidedName = ClientName,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };

        var delay = TimeSpan.FromSeconds(1);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var connection = await factory.CreateConnectionAsync(ct);
                logger.LogInformation("Conectado a RabbitMQ en {Host}:{Port}", o.Host, o.Port);
                return connection;
            }
            catch (BrokerUnreachableException ex) when (attempt < MaxAttempts)
            {
                logger.LogWarning(ex, "RabbitMQ no disponible (intento {Attempt}/{Max}); reintento en {Delay}s",
                    attempt, MaxAttempts, delay.TotalSeconds);
                await Task.Delay(delay, ct);
                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxDelay.Ticks));
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
        _lock.Dispose();
    }
}
