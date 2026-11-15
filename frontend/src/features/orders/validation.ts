// Mismas reglas que Order.Create en el dominio (Orders.Domain/Orders/Order.cs).
// Las claves coinciden con el Field de OrderErrors, que es lo que llega en ProblemDetails.errors.

export interface OrderFormValues {
  nombreCliente: string
  sku: string
  cantidad: string // texto del input; se convierte al enviar
}

export type OrderField = keyof OrderFormValues
export type FieldErrors = Partial<Record<OrderField, string>>

export const ORDER_FIELDS: readonly OrderField[] = ['nombreCliente', 'sku', 'cantidad']

// ⚠️ Igual que Order.CantidadMinima / Order.CantidadMaxima en el dominio
export const CANTIDAD_MIN = 1
export const CANTIDAD_MAX = 100

export function validateOrder(values: OrderFormValues): FieldErrors {
  const errors: FieldErrors = {}

  if (values.nombreCliente.trim() === '') errors.nombreCliente = 'El nombre del cliente es obligatorio.'

  if (values.sku.trim() === '') errors.sku = 'El SKU es obligatorio.'

  const cantidad = Number(values.cantidad)
  if (
    values.cantidad.trim() === '' ||
    !Number.isInteger(cantidad) ||
    cantidad < CANTIDAD_MIN ||
    cantidad > CANTIDAD_MAX
  )
    errors.cantidad = `La cantidad debe estar entre ${CANTIDAD_MIN} y ${CANTIDAD_MAX}.`

  return errors
}
