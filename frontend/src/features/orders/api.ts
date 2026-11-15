import { apiFetch } from '../../api/client'
import type { CreateOrderRequest, Order, Product } from '../../api/types'

export const ordersKeys = {
  all: ['orders'] as const,
}

export const productsKeys = {
  all: ['products'] as const,
}

export const getOrders = (signal?: AbortSignal) => apiFetch<Order[]>('/orders', { signal })

export const getProducts = (signal?: AbortSignal) => apiFetch<Product[]>('/products', { signal })

export const createOrder = (body: CreateOrderRequest) =>
  apiFetch<Order>('/orders', { method: 'POST', body: JSON.stringify(body) })
