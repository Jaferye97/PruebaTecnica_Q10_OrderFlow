using Orders.Application.Orders;

namespace Orders.Application.Abstractions;

public interface IOrderQueries
{
    /// <summary>Los <paramref name="take"/> pedidos más recientes (CreadoEn descendente).</summary>
    Task<IReadOnlyList<OrderDto>> GetAsync(int take, CancellationToken ct);

    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken ct);
}
