using Orders.Domain.Common;

namespace Orders.Domain.Orders;

public sealed class Order
{
    public const int DetalleRechazoMaxLength = 200;

    public Guid Id { get; private set; }
    public string NombreCliente { get; private set; } = null!;
    public string Sku { get; private set; } = null!;
    public int Cantidad { get; private set; }
    public OrderStatus Estado { get; private set; }
    public RejectionReason? MotivoRechazo { get; private set; }
    public string? DetalleRechazo { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }

    // Para EF Core (materialización)
    private Order() { }

    // Para la fábrica
    private Order(Guid id, string nombreCliente, string sku, int cantidad, DateTimeOffset creadoEn)
    {
        Id = id;
        NombreCliente = nombreCliente;
        Sku = sku;
        Cantidad = cantidad;
        CreadoEn = creadoEn;
        Estado = OrderStatus.Pending;
    }

    public static Result<Order> Create(string? nombre, string? sku, int cantidad, DateTimeOffset creadoEn)
    {
        var nombreNorm = nombre?.Trim();
        var skuNorm = sku?.Trim();
        var errors = new List<Error>();

        if (string.IsNullOrEmpty(nombreNorm))
            errors.Add(OrderErrors.NombreRequerido);

        if (string.IsNullOrEmpty(skuNorm))
            errors.Add(OrderErrors.SkuRequerido);

        if (cantidad <= 0)
            errors.Add(OrderErrors.CantidadInvalida);

        if (errors.Count > 0)
            return Result<Order>.Failure(errors);

        var order = new Order(Guid.CreateVersion7(), nombreNorm!, skuNorm!, cantidad, creadoEn);
        return Result<Order>.Success(order);
    }

    public Result Confirm()
    {
        if (Estado != OrderStatus.Pending)
            return Result.Failure(OrderErrors.TransicionInvalida(Estado, "confirmar"));

        Estado = OrderStatus.Confirmed;
        return Result.Success();
    }

    public Result Reject(RejectionReason motivo, string? detalle = null)
    {
        // 1) Primero el estado: si la transición es inválida, solo se devuelve ese error
        if (Estado != OrderStatus.Pending)
            return Result.Failure(OrderErrors.TransicionInvalida(Estado, "rechazar"));

        // 2) Después los datos del rechazo, acumulando los errores
        var errors = new List<Error>();

        if (!Enum.IsDefined(motivo))
            errors.Add(OrderErrors.MotivoInvalido);

        var detalleNorm = string.IsNullOrWhiteSpace(detalle) ? null : detalle.Trim();

        if (detalleNorm is { Length: > DetalleRechazoMaxLength })
            errors.Add(OrderErrors.DetalleMuyLargo);

        if (errors.Count > 0)
            return Result.Failure(errors);

        Estado = OrderStatus.Rejected;
        MotivoRechazo = motivo;
        DetalleRechazo = detalleNorm;
        return Result.Success();
    }
}