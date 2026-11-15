using Inventory.Domain.Common;

namespace Inventory.Domain.Stocks;

public sealed class Stock
{
    public string Sku { get; private set; } = null!;
    public int Disponible { get; private set; }

    // Para EF Core (materialización)
    private Stock() { }

    public Stock(string sku, int disponible)
    {
        Sku = sku;
        Disponible = disponible;
    }

    public Result Reserve(int cantidad)
    {
        if (cantidad <= 0)
            return Result.Failure(StockErrors.CantidadInvalida);

        if (cantidad > Disponible)
            return Result.Failure(StockErrors.InsufficientStock(Disponible, cantidad));

        Disponible -= cantidad;
        return Result.Success();
    }
}
