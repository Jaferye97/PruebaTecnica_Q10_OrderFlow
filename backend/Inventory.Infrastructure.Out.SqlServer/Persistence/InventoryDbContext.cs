using Inventory.Infrastructure.Out.SqlServer.Persistence.Configurations;

namespace Inventory.Infrastructure.Out.SqlServer.Persistence;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<Stock> Stock => Set<Stock>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Explícito (no ApplyConfigurationsFromAssembly): StockConfiguration necesita saber el proveedor
        modelBuilder.ApplyConfiguration(new StockConfiguration(useRowVersion: Database.IsSqlServer()));
        modelBuilder.ApplyConfiguration(new ProcessedEventConfiguration());
    }
}
