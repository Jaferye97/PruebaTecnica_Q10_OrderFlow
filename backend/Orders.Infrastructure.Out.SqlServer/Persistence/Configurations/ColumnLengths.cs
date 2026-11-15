namespace Orders.Infrastructure.Out.SqlServer.Persistence.Configurations;

internal static class ColumnLengths
{
    // ⚠️ Igual que Inventory (StockConfiguration.SkuMaxLength)
    public const int Sku = 30;
    public const int NombreCliente = 100;
    public const int NombreProducto = 100;
    public const int Enum = 30;
}
