namespace Orders.Application.Outbox;

public sealed record OutboxDispatchResult(int Leidos, int Publicados, Exception? Error);

/// <summary>Un ciclo del relay: publica los pendientes más antiguos, en orden.</summary>
public sealed class OutboxDispatcher(IOutboxReader reader, IMessagePublisher publisher, TimeProvider clock)
{
    public const int BatchSize = 20;

    public async Task<OutboxDispatchResult> DispatchPendingAsync(CancellationToken ct)
    {
        var pending = await reader.GetPendingAsync(BatchSize, ct);
        var publicados = 0;

        foreach (var message in pending)
        {
            try
            {
                await publisher.PublishAsync(message, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await reader.MarkFailedAsync(message.Id, ex.Message, ct);

                // Se corta el lote: si el broker está caído, no tiene sentido seguir con el resto
                return new OutboxDispatchResult(pending.Count, publicados, ex);
            }

            // Si esto fallara tras publicar, se republica en el próximo ciclo con el mismo
            // MessageId (= EventId) y el consumidor lo descarta por idempotencia.
            await reader.MarkProcessedAsync(message.Id, clock.GetUtcNow(), ct);
            publicados++;
        }

        return new OutboxDispatchResult(pending.Count, publicados, Error: null);
    }
}
