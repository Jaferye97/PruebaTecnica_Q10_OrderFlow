namespace Inventory.Application.Stocks;

/// <summary>
/// Motivos de rechazo. Se guardan y publican como string con los mismos nombres
/// que el enum RejectionReason de Orders, sin compartir el tipo entre servicios.
/// </summary>
public static class RejectionReasons
{
    public const string InsufficientStock = nameof(InsufficientStock);
    public const string UnknownSku = nameof(UnknownSku);
    public const string Unknown = nameof(Unknown);
}
