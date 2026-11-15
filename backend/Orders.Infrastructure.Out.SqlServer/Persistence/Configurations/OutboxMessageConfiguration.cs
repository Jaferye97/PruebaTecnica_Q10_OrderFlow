using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orders.Infrastructure.Out.SqlServer.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public const int TypeMaxLength = 100;
    public const int UltimoErrorMaxLength = 1000;

    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Type).HasMaxLength(TypeMaxLength).IsRequired();
        builder.Property(m => m.Payload).IsRequired(); // nvarchar(max)
        builder.Property(m => m.OcurridoEn).IsRequired();
        builder.Property(m => m.Intentos).IsRequired();
        builder.Property(m => m.UltimoError).HasMaxLength(UltimoErrorMaxLength);

        // Índice filtrado: el relay solo busca pendientes (por antigüedad) y el índice
        // contiene únicamente esas filas, así se mantiene pequeño aunque la tabla crezca.
        builder.HasIndex(m => m.OcurridoEn)
            .HasDatabaseName("IX_OutboxMessages_Pendientes")
            .HasFilter("[ProcesadoEn] IS NULL");
    }
}
