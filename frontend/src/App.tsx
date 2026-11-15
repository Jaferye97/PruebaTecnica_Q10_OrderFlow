import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ApiError } from './api/client'
import { OrderForm } from './features/orders/components/OrderForm'
import { OrdersTable } from './features/orders/components/OrdersTable'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Un 4xx no se arregla reintentando; red caída y 5xx sí (hasta 3 veces)
      retry: (failureCount, error) => !(error instanceof ApiError && error.isClientError) && failureCount < 3,
    },
  },
})

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <header className="app-header">
        <h1>OrderFlow</h1>
        <p className="muted">Crea pedidos y sigue cómo Inventory los confirma o rechaza.</p>
      </header>
      <main className="layout">
        <section className="panel" aria-labelledby="form-title">
          <h2 id="form-title">Nuevo pedido</h2>
          <OrderForm />
        </section>
        <section className="panel" aria-labelledby="list-title">
          <h2 id="list-title">Pedidos</h2>
          <OrdersTable />
        </section>
      </main>
    </QueryClientProvider>
  )
}
