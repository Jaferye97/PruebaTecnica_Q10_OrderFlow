namespace Orders.Application.Orders.Commands;

public sealed record ConfirmOrderCommand(Guid OrderId);

public sealed class ConfirmOrderHandler(IOrderRepository orders, IUnitOfWork unitOfWork)
{
    public async Task<Result<OrderDto>> HandleAsync(ConfirmOrderCommand cmd, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(cmd.OrderId, ct);
        if (order is null)
            return Result<OrderDto>.Failure(OrderErrors.NoEncontrado(cmd.OrderId));

        var result = order.Confirm();
        if (result.IsFailure)
            return Result<OrderDto>.Failure(result.Errors);

        await unitOfWork.SaveChangesAsync(ct);
        return Result<OrderDto>.Success(OrderDto.From(order));
    }
}