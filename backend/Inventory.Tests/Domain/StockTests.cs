using Inventory.Domain.Stocks;

namespace Inventory.Tests.Domain;

public class StockTests
{
    [Theory]
    [InlineData(10, 3, 7)]
    [InlineData(10, 10, 0)] // reservar exactamente lo disponible
    public void Reserve_con_stock_suficiente_descuenta_lo_disponible(int disponible, int cantidad, int esperado)
    {
        var stock = new Stock("SKU-001", disponible);

        var result = stock.Reserve(cantidad);

        Assert.True(result.IsSuccess);
        Assert.Equal(esperado, stock.Disponible);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Reserve_con_cantidad_cero_o_negativa_falla_sin_tocar_el_stock(int cantidad)
    {
        var stock = new Stock("SKU-001", 10);

        var result = stock.Reserve(cantidad);

        Assert.Equal("stock.cantidad_invalida", Assert.Single(result.Errors).Code);
        Assert.Equal(10, stock.Disponible);
    }

    [Fact]
    public void Reserve_sin_stock_suficiente_devuelve_InsufficientStock_con_el_detalle()
    {
        var stock = new Stock("SKU-003", 5);

        var result = stock.Reserve(8);

        var error = Assert.Single(result.Errors);
        Assert.Equal("stock.insufficient_stock", error.Code);
        Assert.Equal("Disponible: 5, solicitado: 8", error.Message);
        Assert.Equal(5, stock.Disponible);
    }
}
