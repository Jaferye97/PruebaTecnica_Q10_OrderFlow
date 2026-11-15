using Orders.Domain.Orders;

namespace Orders.Application.Abstractions;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct);

    /// <summary>Devuelve la entidad con seguimiento para modificarla (Confirm/Reject).</summary>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);
}