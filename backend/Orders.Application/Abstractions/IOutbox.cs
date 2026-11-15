using OrderFlow.Contracts;

namespace Orders.Application.Abstractions;

/// <summary>
/// Outbox transaccional: el evento se guarda en el mismo SaveChanges que el cambio de negocio
/// y un relay lo publica después. Así no hay pedido sin evento ni evento sin pedido.
/// </summary>
public interface IOutbox
{
    void Add(IIntegrationEvent integrationEvent);
}
