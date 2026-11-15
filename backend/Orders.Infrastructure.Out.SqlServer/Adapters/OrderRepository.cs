using Orders.Application.Abstractions;
using Orders.Domain.Orders;
using Orders.Infrastructure.Out.SqlServer.Persistence;

namespace Orders.Infrastructure.Out.SqlServer.Adapters;

internal sealed class OrderRepository(OrdersDbContext db) : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken ct) =>
        await db.Orders.AddAsync(order, ct);

    // Con tracking: el handler modifica la entidad y luego llama a SaveChanges
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await db.Orders.FindAsync([id], ct);
}
