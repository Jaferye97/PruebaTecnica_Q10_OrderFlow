import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { ApiError } from '../../../api/client'
import type { CreateOrderRequest, Order } from '../../../api/types'
import { createOrder, ordersKeys } from '../api'

export function useCreateOrder() {
  const queryClient = useQueryClient()

  return useMutation<Order, ApiError | Error, CreateOrderRequest>({
    mutationFn: createOrder,
    // Se devuelve la promesa: la mutación sigue "pending" hasta que la lista se recarga,
    // así el pedido nuevo ya está en la tabla cuando aparece la confirmación.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ordersKeys.all }),
  })
}
