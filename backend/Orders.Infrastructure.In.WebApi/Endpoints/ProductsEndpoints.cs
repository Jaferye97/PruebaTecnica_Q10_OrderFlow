using Orders.Application.Products;
using Orders.Application.Products.Queries;

namespace Orders.Infrastructure.In.WebApi.Endpoints;

public static class ProductsEndpoints
{
    public static IEndpointRouteBuilder MapProductsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        group.MapGet("/", GetProducts).WithName("GetProducts");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ProductDto>>> GetProducts(
        GetProductsHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetProductsQuery(), ct);
        return TypedResults.Ok(result.Value);
    }
}
