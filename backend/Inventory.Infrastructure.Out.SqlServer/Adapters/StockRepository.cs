namespace Inventory.Infrastructure.Out.SqlServer.Adapters;

internal sealed class StockRepository(InventoryDbContext db) : IStockRepository
{
    // Con tracking: el handler llama a Reserve y luego a SaveChanges
    public async Task<Stock?> GetBySkuAsync(string sku, CancellationToken ct) =>
        await db.Stock.FindAsync([sku], ct);
}
