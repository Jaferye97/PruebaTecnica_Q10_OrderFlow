import type { ReactElement } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import { vi } from 'vitest'
import type { Order } from '../api/types'

/** Renderiza con un QueryClient nuevo por test (sin caché compartida) y sin reintentos. */
export function renderWithQuery(ui: ReactElement) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>)
}

export const json = (status: number, body: unknown) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })

type Handler = (url: string, init?: RequestInit) => Response | Promise<Response>

/** Sustituye fetch global por un handler; devuelve el mock para inspeccionar las llamadas. */
export function mockFetch(handler: Handler) {
  const fn = vi.fn((input: RequestInfo | URL, init?: RequestInit) => Promise.resolve(handler(String(input), init)))
  vi.stubGlobal('fetch', fn)
  return fn
}

export function order(overrides: Partial<Order> = {}): Order {
  return {
    id: crypto.randomUUID(),
    nombreCliente: 'Ana',
    sku: 'SKU-001',
    cantidad: 1,
    estado: 'Confirmed',
    motivoRechazo: null,
    detalleRechazo: null,
    creadoEn: '2026-11-15T10:00:00Z',
    ...overrides,
  }
}
