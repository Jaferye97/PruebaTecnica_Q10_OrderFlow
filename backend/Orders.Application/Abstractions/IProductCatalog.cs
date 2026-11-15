using Orders.Application.Products;

namespace Orders.Application.Abstractions;

public interface IProductCatalog
{
    Task<bool> ExistsAsync(string sku, CancellationToken ct);

    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct);
}