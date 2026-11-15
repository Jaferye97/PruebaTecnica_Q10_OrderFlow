using Inventory.Application.Abstractions;
using Inventory.Application.Idempotency;
using Inventory.Domain.Stocks;
using OrderFlow.Contracts;

namespace Inventory.Application.Stocks;

public sealed record ReserveStockCommand(Guid EventId, Guid OrderId, string Sku, int Cantidad);

public sealed record ReservationOutcome(
    ProcessingResult Resultado,
    string? Motivo,
    string? Detalle,
    Guid ResultEventId,
    bool EsDuplicado)
{
    public static ReservationOutcome From(ProcessedEvent processed, bool esDuplicado) => new(
        processed.Resultado,
        processed.Motivo,
        processed.Detalle,
        processed.ResultEventId,
        esDuplicado);
}

public sealed class ReserveStockHandler(
    IStockRepository stocks,
    IProcessedEventStore processedEvents,
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher,
    TimeProvider clock)
{
    public async Task<ReservationOutcome> HandleAsync(ReserveStockCommand cmd, CancellationToken ct)
    {
        // 1) Idempotencia: si ya se procesó, se devuelve lo guardado (mismo ResultEventId).
        //    También se republica: si el proceso cayó tras el SaveChanges y antes de publicar,
        //    el mensaje no tuvo ack, se reentregó y así la respuesta llega igual.
        var existing = await processedEvents.FindAsync(cmd.EventId, ct);
        if (existing is not null)
        {
            await PublishResultAsync(existing, ct);
            return ReservationOutcome.From(existing, esDuplicado: true);
        }

        // 2) y 3) Cargar el stock y reservar
        var processed = await ReserveAsync(cmd, ct);

        // 4) Registrar el evento (aquí se genera ResultEventId)
        processedEvents.Add(processed);

        // 5) Un solo SaveChanges: la reserva y el registro se guardan juntos o ninguno
        await unitOfWork.SaveChangesAsync(ct);

        // 6) Publicar después de guardar: nunca se anuncia una reserva que no quedó persistida
        await PublishResultAsync(processed, ct);

        return ReservationOutcome.From(processed, esDuplicado: false);
    }

    // EventId = ResultEventId: la misma respuesta siempre sale con la misma identidad
    private Task PublishResultAsync(ProcessedEvent processed, CancellationToken ct) =>
        processed.Resultado == ProcessingResult.Reserved
            ? publisher.PublishAsync(
                new StockReserved(processed.ResultEventId, processed.OrderId, processed.ProcesadoEn), ct)
            : publisher.PublishAsync(
                new StockRejected(processed.ResultEventId, processed.OrderId,
                    processed.Motivo ?? RejectionReasons.Unknown, processed.Detalle, processed.ProcesadoEn), ct);

    private async Task<ProcessedEvent> ReserveAsync(ReserveStockCommand cmd, CancellationToken ct)
    {
        var now = clock.GetUtcNow();

        var stock = await stocks.GetBySkuAsync(cmd.Sku, ct);
        if (stock is null)
            return ProcessedEvent.Rejected(cmd.EventId, cmd.OrderId,
                RejectionReasons.UnknownSku, $"El SKU '{cmd.Sku}' no existe en el inventario.", now);

        var result = stock.Reserve(cmd.Cantidad);
        if (result.IsSuccess)
            return ProcessedEvent.Reserved(cmd.EventId, cmd.OrderId, now);

        var error = result.Errors[0];
        var motivo = error.Code == StockErrors.InsufficientStockCode
            ? RejectionReasons.InsufficientStock
            : RejectionReasons.Unknown; // p. ej. cantidad <= 0: no debería llegar desde Orders

        return ProcessedEvent.Rejected(cmd.EventId, cmd.OrderId, motivo, error.Message, now);
    }
}
