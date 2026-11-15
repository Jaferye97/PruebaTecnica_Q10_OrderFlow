using Inventory.Domain.Stocks;

namespace Inventory.Application.Abstractions;

public interface IStockRepository
{
    /// <summary>Devuelve el stock con seguimiento para poder reservar y guardar.</summary>
    Task<Stock?> GetBySkuAsync(string sku, CancellationToken ct);
}
