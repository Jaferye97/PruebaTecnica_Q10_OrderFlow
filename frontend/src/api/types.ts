// Contratos del backend (Orders.Infrastructure.In.WebApi). Los enums viajan como string
// gracias a JsonStringEnumConverter.

export type OrderStatus = 'Pending' | 'Confirmed' | 'Rejected'

export type RejectionReason = 'InsufficientStock' | 'UnknownSku' | 'Unknown'

export interface Order {
  id: string
  nombreCliente: string
  sku: string
  cantidad: number
  estado: OrderStatus
  motivoRechazo: RejectionReason | null
  detalleRechazo: string | null
  creadoEn: string // DateTimeOffset en ISO 8601
}

export interface Product {
  sku: string
  nombre: string
  stock: number
}

export interface CreateOrderRequest {
  nombreCliente: string
  sku: string
  cantidad: number
}

/** RFC 9457. `errors` solo viene en los 400 de validación (ValidationProblem). */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
  code?: string
  codes?: string[]
}
