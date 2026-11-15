using Orders.Application.Common;

namespace Orders.Application.Abstractions;

public interface IUnitOfWork
{
    /// <exception cref="PersistenceConflictException">
    /// Conflicto de concurrencia (RowVersion) o clave duplicada.
    /// </exception>
    Task SaveChangesAsync(CancellationToken ct);
}
