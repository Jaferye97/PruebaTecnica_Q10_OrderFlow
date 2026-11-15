using Microsoft.AspNetCore.Http.HttpResults;
using Orders.Application.Orders;
using Orders.Application.Orders.Commands;
using Orders.Application.Orders.Queries;

namespace Orders.Infrastructure.In.WebApi.Endpoints;

public sealed record CreateOrderRequest(string? NombreCliente, string? Sku, int Cantidad);

public static class OrdersEndpoints
{
    private const int DefaultTake = 50;

    public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        group.MapGet("/", GetOrders).WithName("GetOrders");
        group.MapGet("/{id:guid}", GetOrderById).WithName("GetOrderById");
        group.MapPost("/", CreateOrder).WithName("CreateOrder");

        return app;
    }

    private static async Task<Results<Ok<IReadOnlyList<OrderDto>>, ValidationProblem>> GetOrders(
        GetOrdersHandler handler, CancellationToken ct, int take = DefaultTake)
    {
        var result = await handler.HandleAsync(new GetOrdersQuery(take), ct);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : ErrorResults.Validation(result.Errors);
    }

    private static async Task<Results<Ok<OrderDto>, ProblemHttpResult>> GetOrderById(
        Guid id, GetOrderByIdHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetOrderByIdQuery(id), ct);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : ErrorResults.Problem(result.Errors[0], StatusCodes.Status404NotFound);
    }

    private static async Task<Results<Created<OrderDto>, ValidationProblem>> CreateOrder(
        CreateOrderRequest request, CreateOrderHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(
            new CreateOrderCommand(request.NombreCliente, request.Sku, request.Cantidad), ct);

        if (result.IsFailure)
            return ErrorResults.Validation(result.Errors);

        var order = result.Value;
        return TypedResults.Created($"/api/orders/{order.Id}", order);
    }
}
