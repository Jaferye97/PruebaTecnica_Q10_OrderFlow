using Orders.Application.Abstractions;
using Orders.Domain.Common;
using Orders.Domain.Orders;

namespace Orders.Application.Orders.Queries;

public sealed record GetOrderByIdQuery(Guid Id);

public sealed class GetOrderByIdHandler(IOrderQueries queries)
{
    public async Task<Result<OrderDto>> HandleAsync(GetOrderByIdQuery query, CancellationToken ct)
    {
        var order = await queries.GetByIdAsync(query.Id, ct);

        return order is null
            ? Result<OrderDto>.Failure(OrderErrors.NoEncontrado(query.Id))
            : Result<OrderDto>.Success(order);
    }
}