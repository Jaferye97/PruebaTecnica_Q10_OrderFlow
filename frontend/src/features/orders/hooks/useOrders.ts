import { useQuery } from '@tanstack/react-query'
import type { Order } from '../../../api/types'
import { getOrders, ordersKeys } from '../api'

/** Mientras algún pedido espera la respuesta de Inventory, el estado cambia en segundos. */
const PENDING_INTERVAL_MS = 2_000

/** Sin pendientes solo pueden aparecer pedidos creados desde otro cliente: basta con un ritmo lento. */
const IDLE_INTERVAL_MS = 15_000

export const ordersRefetchInterval = (orders: Order[] | undefined) =>
  orders?.some((order) => order.estado === 'Pending') ? PENDING_INTERVAL_MS : IDLE_INTERVAL_MS

export function useOrders() {
  return useQuery({
    queryKey: ordersKeys.all,
    queryFn: ({ signal }) => getOrders(signal),
    refetchInterval: (query) => ordersRefetchInterval(query.state.data),
  })
}
