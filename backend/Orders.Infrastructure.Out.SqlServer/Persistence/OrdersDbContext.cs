using Microsoft.EntityFrameworkCore;
using Orders.Domain.Orders;
using Orders.Infrastructure.Out.SqlServer.Persistence.Configurations;

namespace Orders.Infrastructure.Out.SqlServer.Persistence;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<ProductRecord> Products => Set<ProductRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Explícito: OrderConfiguration necesita saber el proveedor (SQL Server o SQLite)
        modelBuilder.ApplyConfiguration(new OrderConfiguration(useRowVersion: Database.IsSqlServer()));
        modelBuilder.ApplyConfiguration(new ProductRecordConfiguration());
    }
}