namespace Orders.Application.Orders.Commands;

public sealed record RejectOrderCommand(Guid OrderId, string? Motivo, string? Detalle);

public sealed class RejectOrderHandler(IOrderRepository orders, IUnitOfWork unitOfWork)
{
    public async Task<Result<OrderDto>> HandleAsync(RejectOrderCommand cmd, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(cmd.OrderId, ct);
        if (order is null)
            return Result<OrderDto>.Failure(OrderErrors.NoEncontrado(cmd.OrderId));

        var result = order.Reject(ParseMotivo(cmd.Motivo), cmd.Detalle);
        if (result.IsFailure)
            return Result<OrderDto>.Failure(result.Errors);

        await unitOfWork.SaveChangesAsync(ct);
        return Result<OrderDto>.Success(OrderDto.From(order));
    }

    // Compara solo contra los nombres del enum (sin distinguir mayúsculas), así "2" o "99" no se aceptan como valores
    internal static RejectionReason ParseMotivo(string? motivo)
    {
        var limpio = motivo?.Trim();
        foreach (var valor in Enum.GetValues<RejectionReason>())
        {
            if (string.Equals(valor.ToString(), limpio, StringComparison.OrdinalIgnoreCase))
                return valor;
        }
        return RejectionReason.Unknown;
    }
}