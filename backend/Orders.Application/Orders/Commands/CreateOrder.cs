using Orders.Application.Abstractions;
using Orders.Domain.Common;
using Orders.Domain.Orders;

namespace Orders.Application.Orders.Commands;

public sealed record CreateOrderCommand(string? NombreCliente, string? Sku, int Cantidad);

public sealed class CreateOrderHandler(
    IOrderRepository orders,
    IProductCatalog catalog,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<Result<OrderDto>> HandleAsync(CreateOrderCommand cmd, CancellationToken ct)
    {
        var created = Order.Create(cmd.NombreCliente, cmd.Sku, cmd.Cantidad, clock.GetUtcNow());

        var errors = created.IsFailure ? created.Errors.ToList() : [];

        // Solo se consulta el catálogo si hay SKU (si viene vacío, ya está SkuRequerido)
        var sku = cmd.Sku?.Trim();
        if (!string.IsNullOrEmpty(sku) && !await catalog.ExistsAsync(sku, ct))
            errors.Add(OrderErrors.SkuNoExiste(sku));

        if (errors.Count > 0)
            return Result<OrderDto>.Failure(errors);

        var order = created.Value;
        await orders.AddAsync(order, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<OrderDto>.Success(OrderDto.From(order));
    }
}
