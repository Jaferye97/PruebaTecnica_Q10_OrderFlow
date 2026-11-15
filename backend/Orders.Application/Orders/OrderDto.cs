using Orders.Domain.Orders;

namespace Orders.Application.Orders;

public sealed record OrderDto(
    Guid Id,
    string NombreCliente,
    string Sku,
    int Cantidad,
    string Estado,
    string? MotivoRechazo,
    string? DetalleRechazo,
    DateTimeOffset CreadoEn)
{
    public static OrderDto From(Order order) => new(
        order.Id,
        order.NombreCliente,
        order.Sku,
        order.Cantidad,
        order.Estado.ToString(),
        order.MotivoRechazo?.ToString(),
        order.DetalleRechazo,
        order.CreadoEn);
}