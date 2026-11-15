using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Out.SqlServer.Persistence.Configurations;

internal sealed class OrderConfiguration(bool useRowVersion) : IEntityTypeConfiguration<Order>
{
    public const string RowVersion = "RowVersion";

    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        // PK no clustered: el Guid lo genera el dominio; el orden físico va por CreadoEn
        builder.HasKey(o => o.Id).IsClustered(false);
        builder.Property(o => o.Id).ValueGeneratedNever();

        // Índice clustered en CreadoEn: "más recientes primero" lee en orden físico
        builder.HasIndex(o => o.CreadoEn).IsClustered();

        builder.Property(o => o.NombreCliente)
            .HasMaxLength(ColumnLengths.NombreCliente)
            .IsRequired();

        builder.Property(o => o.Sku)
            .HasMaxLength(ColumnLengths.Sku)
            .IsRequired();

        builder.Property(o => o.Cantidad).IsRequired();

        builder.Property(o => o.Estado)
            .HasConversion<string>()
            .HasMaxLength(ColumnLengths.Enum)
            .IsRequired();

        builder.Property(o => o.MotivoRechazo)
            .HasConversion<string>()
            .HasMaxLength(ColumnLengths.Enum);

        builder.Property(o => o.DetalleRechazo)
            .HasMaxLength(Order.DetalleRechazoMaxLength);

        builder.Property(o => o.CreadoEn).IsRequired();

        // Concurrencia optimista: shadow property, el dominio no la conoce.
        // Solo en SQL Server: SQLite no genera rowversion y rompería los tests.
        if (useRowVersion)
            builder.Property<byte[]>(RowVersion).IsRowVersion();
    }
}
