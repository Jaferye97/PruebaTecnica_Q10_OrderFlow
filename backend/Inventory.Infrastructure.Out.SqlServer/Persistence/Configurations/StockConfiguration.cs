namespace Inventory.Infrastructure.Out.SqlServer.Persistence.Configurations;

internal sealed class StockConfiguration(bool useRowVersion) : IEntityTypeConfiguration<Stock>
{
    public const string RowVersion = "RowVersion";
    // ⚠️ Igual que Orders (ColumnLengths.Sku)
    public const int SkuMaxLength = 30;

    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.ToTable("Stock", table =>
            // Última defensa: aunque falle el dominio, la BD no acepta stock negativo
            table.HasCheckConstraint("CK_Stock_Disponible", "[Disponible] >= 0"));

        builder.HasKey(s => s.Sku);

        builder.Property(s => s.Sku)
            .HasMaxLength(SkuMaxLength)
            .ValueGeneratedNever();

        builder.Property(s => s.Disponible).IsRequired();

        // Concurrencia optimista: dos reservas simultáneas del mismo SKU no pisan el stock.
        // Solo en SQL Server: SQLite no genera rowversion y rompería los tests.
        if (useRowVersion)
            builder.Property<byte[]>(RowVersion).IsRowVersion();

        // Los datos de prueba se insertan con db/init/02-inventory.sql, no desde EF
    }
}
