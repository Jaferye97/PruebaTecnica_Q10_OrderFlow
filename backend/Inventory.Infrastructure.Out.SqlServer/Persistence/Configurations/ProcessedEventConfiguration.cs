namespace Inventory.Infrastructure.Out.SqlServer.Persistence.Configurations;

internal sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("ProcessedEvents");

        // PK = EventId: un mismo mensaje no se puede registrar dos veces
        builder.HasKey(e => e.EventId);
        builder.Property(e => e.EventId).ValueGeneratedNever();

        // Único por pedido: dos eventos distintos del mismo pedido no reservan dos veces
        builder.HasIndex(e => e.OrderId).IsUnique();

        builder.Property(e => e.Resultado)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Motivo).HasMaxLength(30);
        builder.Property(e => e.Detalle).HasMaxLength(200);

        builder.Property(e => e.ResultEventId).IsRequired();
        builder.Property(e => e.ProcesadoEn).IsRequired();
    }
}
