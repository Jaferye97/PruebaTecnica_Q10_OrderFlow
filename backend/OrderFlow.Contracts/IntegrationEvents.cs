namespace OrderFlow.Contracts;

/// <summary>Datos comunes a todos los eventos de integración.</summary>
public interface IIntegrationEvent
{
    /// <summary>Identidad del mensaje: se publica como MessageId y sirve para la idempotencia.</summary>
    Guid EventId { get; }

    /// <summary>Se publica como CorrelationId.</summary>
    Guid OrderId { get; }

    DateTimeOffset OcurridoEn { get; }
}

public sealed record OrderCreated(
    Guid EventId,
    Guid OrderId,
    string Sku,
    int Cantidad,
    DateTimeOffset OcurridoEn) : IIntegrationEvent;

public sealed record StockReserved(
    Guid EventId,
    Guid OrderId,
    DateTimeOffset OcurridoEn) : IIntegrationEvent;

public sealed record StockRejected(
    Guid EventId,
    Guid OrderId,
    string Motivo,
    string? Detalle,
    DateTimeOffset OcurridoEn) : IIntegrationEvent;
