namespace Orders.Application.Abstractions;

public sealed record PendingOutboxMessage(Guid Id, string Type, string Payload);

/// <summary>Lado de lectura del Outbox, usado por el relay.</summary>
public interface IOutboxReader
{
    /// <summary>Los <paramref name="max"/> pendientes más antiguos.</summary>
    Task<IReadOnlyList<PendingOutboxMessage>> GetPendingAsync(int max, CancellationToken ct);

    Task MarkProcessedAsync(Guid id, DateTimeOffset procesadoEn, CancellationToken ct);

    /// <summary>Suma un intento y guarda el error; el mensaje sigue pendiente.</summary>
    Task MarkFailedAsync(Guid id, string error, CancellationToken ct);
}
