using Orders.Domain.Orders;

namespace Orders.Tests.Domain;

public class OrderTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    public enum Accion { Confirm, Reject }

    private static Order PedidoEn(OrderStatus estado)
    {
        var order = Order.Create("Ana", "SKU-1", 1, Ahora).Value;

        if (estado == OrderStatus.Confirmed) order.Confirm();
        if (estado == OrderStatus.Rejected) order.Reject(RejectionReason.UnknownSku);

        return order;
    }

    [Theory]
    [InlineData(OrderStatus.Pending, Accion.Confirm, true, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Pending, Accion.Reject, true, OrderStatus.Rejected)]
    [InlineData(OrderStatus.Confirmed, Accion.Confirm, false, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Confirmed, Accion.Reject, false, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Rejected, Accion.Confirm, false, OrderStatus.Rejected)]
    [InlineData(OrderStatus.Rejected, Accion.Reject, false, OrderStatus.Rejected)]
    public void ConfirmYReject_DesdeCadaEstado_SoloPendingPermiteLaTransicion(OrderStatus inicial, Accion accion, bool exito, OrderStatus esperado)
    {
        var order = PedidoEn(inicial);
        var motivoAntes = order.MotivoRechazo;

        var result = accion == Accion.Confirm
            ? order.Confirm()
            : order.Reject(RejectionReason.InsufficientStock, "sin stock");

        Assert.Equal(exito, result.IsSuccess);
        Assert.Equal(esperado, order.Estado);

        if (!exito)
        {
            Assert.Equal("order.transicion_invalida", Assert.Single(result.Errors).Code);
            Assert.Equal(motivoAntes, order.MotivoRechazo); // el fallo no modifica el pedido
        }
    }

    [Theory]
    [InlineData("  sin stock  ", "sin stock")]
    [InlineData("   ", null)]
    public void Reject_guarda_el_detalle_normalizado(string detalle, string? esperado)
    {
        var order = PedidoEn(OrderStatus.Pending);

        var result = order.Reject(RejectionReason.InsufficientStock, detalle);

        Assert.True(result.IsSuccess);
        Assert.Equal(esperado, order.DetalleRechazo);
    }

    [Fact]
    public void Reject_acepta_detalle_de_200_caracteres_con_espacios_alrededor()
    {
        var order = PedidoEn(OrderStatus.Pending);
        var detalle = new string('x', Order.DetalleRechazoMaxLength);

        var result = order.Reject(RejectionReason.Unknown, $"  {detalle}  ");

        Assert.True(result.IsSuccess);
        Assert.Equal(Order.DetalleRechazoMaxLength, order.DetalleRechazo!.Length); // cabe en nvarchar(200)
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(Order.CantidadMinima, true)]   // 1: límite inferior incluido
    [InlineData(Order.CantidadMaxima, true)]   // 100: límite superior incluido
    [InlineData(Order.CantidadMaxima + 1, false)]
    public void Create_CantidadDebeEstarEntre1y100(int cantidad, bool esValida)
    {
        var result = Order.Create("Ana", "SKU-1", cantidad, Ahora);

        Assert.Equal(esValida, result.IsSuccess);
        if (!esValida)
            Assert.Equal("order.cantidad_invalida", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void Create_NombreVacioYCantidadCero_AcumulaDosErrores()
    {
        var result = Order.Create("   ", "SKU-1", 0, Ahora);

        Assert.True(result.IsFailure);
        Assert.Equal(
            new[] { "order.nombre_requerido", "order.cantidad_invalida" },
            result.Errors.Select(e => e.Code));
    }
}
