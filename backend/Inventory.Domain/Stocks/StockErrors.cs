namespace Inventory.Domain.Stocks;

public static class StockErrors
{
    public const string CantidadInvalidaCode = "stock.cantidad_invalida";
    public const string InsufficientStockCode = "stock.insufficient_stock";

    public static Error CantidadInvalida =>
        new(CantidadInvalidaCode, "cantidad", "La cantidad a reservar debe ser mayor que 0.");

    public static Error InsufficientStock(int disponible, int solicitado) =>
        new(InsufficientStockCode, "cantidad", $"Disponible: {disponible}, solicitado: {solicitado}");
}
