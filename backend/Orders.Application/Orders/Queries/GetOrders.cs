namespace Orders.Application.Orders.Queries;

public sealed record GetOrdersQuery(int Take);

public sealed class GetOrdersHandler(IOrderQueries queries)
{
    public const int MinTake = 1;
    public const int MaxTake = 200;

    public async Task<Result<IReadOnlyList<OrderDto>>> HandleAsync(GetOrdersQuery query, CancellationToken ct)
    {
        if (query.Take is < MinTake or > MaxTake)
            return Result<IReadOnlyList<OrderDto>>.Failure(OrderErrors.TakeFueraDeRango(MinTake, MaxTake));

        var items = await queries.GetAsync(query.Take, ct);
        return Result<IReadOnlyList<OrderDto>>.Success(items);
    }
}
