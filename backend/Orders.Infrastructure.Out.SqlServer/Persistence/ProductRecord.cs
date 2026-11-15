namespace Orders.Infrastructure.Out.SqlServer.Persistence;

/// <summary>
/// Copia local (read model) del catálogo de productos. No es una entidad de dominio:
/// Orders solo la usa para validar SKU y listar productos.
/// </summary>
public sealed class ProductRecord
{
    public string Sku { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public int Stock { get; set; }
}
