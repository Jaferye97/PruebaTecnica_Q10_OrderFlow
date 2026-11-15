namespace Orders.Application.Common;

/// <summary>
/// La infraestructura la lanza ante un conflicto al guardar (concurrencia optimista por RowVersion
/// o clave duplicada), para que la aplicación lo maneje sin depender de EF Core.
/// </summary>
public sealed class PersistenceConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
