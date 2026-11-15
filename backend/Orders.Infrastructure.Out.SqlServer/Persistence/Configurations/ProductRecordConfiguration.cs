namespace Orders.Infrastructure.Out.SqlServer.Persistence.Configurations;

internal sealed class ProductRecordConfiguration : IEntityTypeConfiguration<ProductRecord>
{
    public void Configure(EntityTypeBuilder<ProductRecord> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Sku);

        builder.Property(p => p.Sku)
            .HasMaxLength(ColumnLengths.Sku)
            .ValueGeneratedNever();

        builder.Property(p => p.Nombre)
            .HasMaxLength(ColumnLengths.NombreProducto)
            .IsRequired();

        builder.Property(p => p.Stock).IsRequired();

        // Los datos de prueba se insertan con db/init/01-orders.sql, no desde EF
    }
}
