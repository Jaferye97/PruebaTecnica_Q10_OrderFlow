import { useQuery } from '@tanstack/react-query'
import { getProducts, productsKeys } from '../api'

/** El catálogo no cambia durante la sesión: se pide una vez y no se vuelve a pedir. */
export function useProducts() {
  return useQuery({
    queryKey: productsKeys.all,
    queryFn: ({ signal }) => getProducts(signal),
    staleTime: Infinity,
  })
}
