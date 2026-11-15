namespace Orders.Application.Abstractions;

public interface IMessagePublisher
{
    /// <summary>
    /// Publica el mensaje tal como quedó guardado en el Outbox.
    /// Solo retorna cuando el broker lo confirmó; si no, lanza excepción.
    /// </summary>
    Task PublishAsync(PendingOutboxMessage message, CancellationToken ct);
}
