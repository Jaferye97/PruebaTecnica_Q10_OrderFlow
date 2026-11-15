namespace Orders.Infrastructure.Out.SqlServer.Persistence;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<ProductRecord> Products => Set<ProductRecord>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Explícito (no ApplyConfigurationsFromAssembly): OrderConfiguration necesita saber el proveedor
        modelBuilder.ApplyConfiguration(new OrderConfiguration(useRowVersion: Database.IsSqlServer()));
        modelBuilder.ApplyConfiguration(new ProductRecordConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}
