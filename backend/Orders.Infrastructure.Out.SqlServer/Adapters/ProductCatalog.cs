using Microsoft.EntityFrameworkCore;
using Orders.Application.Abstractions;
using Orders.Application.Products;
using Orders.Infrastructure.Out.SqlServer.Persistence;

namespace Orders.Infrastructure.Out.SqlServer.Adapters;

internal sealed class ProductCatalog(OrdersDbContext db) : IProductCatalog
{
    public Task<bool> ExistsAsync(string sku, CancellationToken ct) =>
        db.Products.AnyAsync(p => p.Sku == sku, ct);

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct) =>
        await db.Products
            .AsNoTracking()
            .OrderBy(p => p.Sku)
            .Select(p => new ProductDto(p.Sku, p.Nombre, p.Stock))
            .ToListAsync(ct);
}
