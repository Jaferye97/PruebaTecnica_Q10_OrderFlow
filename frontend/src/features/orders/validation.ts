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

const INT32_MAX = 2_147_483_647 // Cantidad es int en el backend

export function validateOrder(values: OrderFormValues): FieldErrors {
  const errors: FieldErrors = {}

  if (values.nombreCliente.trim() === '') errors.nombreCliente = 'El nombre del cliente es obligatorio.'

  if (values.sku.trim() === '') errors.sku = 'El SKU es obligatorio.'

  const cantidad = Number(values.cantidad)
  if (values.cantidad.trim() === '' || !Number.isInteger(cantidad) || cantidad <= 0)
    errors.cantidad = 'La cantidad debe ser mayor que 0.'
  else if (cantidad > INT32_MAX) errors.cantidad = `La cantidad no puede superar ${INT32_MAX}.`

  return errors
}
