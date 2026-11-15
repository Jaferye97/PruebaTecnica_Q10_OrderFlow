namespace Orders.Application.Products.Queries;

public sealed record GetProductsQuery;

public sealed class GetProductsHandler(IProductCatalog catalog)
{
    public async Task<Result<IReadOnlyList<ProductDto>>> HandleAsync(GetProductsQuery query, CancellationToken ct)
    {
        var products = await catalog.GetAllAsync(ct);
        return Result<IReadOnlyList<ProductDto>>.Success(products);
    }
}