using Orders.Domain.Common;

namespace Orders.Domain.Orders;

public static class OrderErrors
{
    public const string NoEncontradoCode = "order.no_encontrado";

    public static Error NombreRequerido =>
        new("order.nombre_requerido", "nombreCliente", "El nombre del cliente es obligatorio.");

    public static Error SkuRequerido =>
        new("order.sku_requerido", "sku", "El SKU es obligatorio.");

    public static Error CantidadInvalida =>
        new("order.cantidad_invalida", "cantidad",
            $"La cantidad debe estar entre {Order.CantidadMinima} y {Order.CantidadMaxima}.");

    public static Error MotivoInvalido =>
        new("order.motivo_invalido", "motivoRechazo", "El motivo de rechazo no es válido.");

    public static Error DetalleMuyLargo =>
        new("order.detalle_muy_largo", "detalleRechazo", "El detalle no puede superar 200 caracteres.");

    public static Error TransicionInvalida(OrderStatus actual, string accion) =>
        new("order.transicion_invalida", null, $"No se puede {accion} un pedido en estado {actual}.");

    public static Error SkuNoExiste(string sku) =>
        new("order.sku_no_existe", "sku", $"El SKU '{sku}' no existe en el catálogo.");

    public static Error NoEncontrado(Guid id) =>
        new(NoEncontradoCode, null, $"No existe el pedido {id}.");

    public static Error TakeFueraDeRango(int min, int max) =>
        new("order.take_fuera_de_rango", "take", $"take debe estar entre {min} y {max}.");
}