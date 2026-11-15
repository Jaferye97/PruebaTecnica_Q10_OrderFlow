namespace Orders.Domain.Common;

public sealed record Error(string Code, string? Field, string Message);