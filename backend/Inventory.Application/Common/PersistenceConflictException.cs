namespace Inventory.Application.Common;

/// <summary>
/// La infraestructura la lanza ante un conflicto al guardar (concurrencia optimista o clave duplicada),
/// para que la aplicación lo maneje sin depender de EF Core.
/// </summary>
public sealed class PersistenceConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
