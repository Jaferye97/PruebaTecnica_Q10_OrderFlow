using Orders.Application.Common;

namespace Orders.Infrastructure.Out.SqlServer.Adapters;

internal sealed class UnitOfWork(OrdersDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) // incluye DbUpdateConcurrencyException
        {
            // Se descartan los cambios pendientes: el reintento empieza con el contexto limpio
            db.ChangeTracker.Clear();
            throw new PersistenceConflictException(
                "No se pudieron guardar los cambios por un conflicto (concurrencia o clave duplicada).", ex);
        }
    }
}
