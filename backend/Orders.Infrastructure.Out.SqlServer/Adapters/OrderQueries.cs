using System.Linq.Expressions;
using Orders.Application.Orders;

namespace Orders.Infrastructure.Out.SqlServer.Adapters;

internal sealed class OrderQueries(OrdersDbContext db) : IOrderQueries
{
    // Proyección traducible a SQL: solo se leen las columnas del DTO
    private static readonly Expression<Func<Order, OrderDto>> ToDto = o =>
        new OrderDto(
            o.Id,
            o.NombreCliente,
            o.Sku,
            o.Cantidad,
            o.Estado.ToString(),
            o.MotivoRechazo.HasValue ? o.MotivoRechazo.Value.ToString() : null,
            o.DetalleRechazo,
            o.CreadoEn);

    public async Task<IReadOnlyList<OrderDto>> GetAsync(int take, CancellationToken ct) =>
        await db.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.CreadoEn)
            .Take(take)
            .Select(ToDto)
            .ToListAsync(ct);

    public async Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await db.Orders
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(ToDto)
            .SingleOrDefaultAsync(ct);
}
