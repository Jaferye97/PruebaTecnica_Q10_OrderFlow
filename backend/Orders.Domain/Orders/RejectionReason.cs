namespace Orders.Domain.Orders;

public enum RejectionReason
{
    InsufficientStock = 1,
    UnknownSku = 2,
    Unknown = 3
}