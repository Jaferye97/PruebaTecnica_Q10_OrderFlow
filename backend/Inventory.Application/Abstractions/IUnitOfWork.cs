using Inventory.Application.Common;

namespace Inventory.Application.Abstractions;

public interface IUnitOfWork
{
    /// <exception cref="PersistenceConflictException">
    /// Conflicto de concurrencia o evento ya registrado por otro consumidor.
    /// </exception>
    Task SaveChangesAsync(CancellationToken ct);
}
