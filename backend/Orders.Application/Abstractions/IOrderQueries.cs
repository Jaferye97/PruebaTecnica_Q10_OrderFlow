using Orders.Application.Orders;

namespace Orders.Application.Abstractions;

public interface IOrderQueries
{
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken ct);
}
