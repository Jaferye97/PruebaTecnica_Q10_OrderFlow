import type { Order, RejectionReason } from '../../../api/types'
import { useOrders } from '../hooks/useOrders'
import { StatusBadge } from './StatusBadge'

const dateFormat = new Intl.DateTimeFormat('es-CO', { dateStyle: 'medium', timeStyle: 'medium' })

const REASONS: Record<RejectionReason, string> = {
  InsufficientStock: 'Stock insuficiente',
  UnknownSku: 'SKU desconocido',
  Unknown: 'Motivo desconocido',
}

function rejectionText(order: Order): string | null {
  if (order.estado !== 'Rejected') return null
  const reason = order.motivoRechazo ? (REASONS[order.motivoRechazo] ?? order.motivoRechazo) : 'Sin motivo'
  return order.detalleRechazo ? `${reason}: ${order.detalleRechazo}` : reason
}

export function OrdersTable() {
  const { data: orders, isPending, isError, error, refetch, isFetching } = useOrders()

  if (isPending) return <p className="muted">Cargando pedidos…</p>

  // Error sin datos previos: no hay nada que mostrar
  if (!orders)
    return (
      <div className="banner banner--error" role="alert">
        No se pudieron cargar los pedidos: {error?.message}{' '}
        <button type="button" className="link" onClick={() => refetch()}>
          Reintentar
        </button>
      </div>
    )

  if (orders.length === 0) return <p className="muted">Todavía no hay pedidos. Crea el primero.</p>

  const polling = orders.some((o) => o.estado === 'Pending')

  return (
    <>
      {/* Error en un refetch: se conservan los últimos datos */}
      {isError && (
        <div className="banner banner--warning" role="status">
          Falló la última actualización ({error.message}). Mostrando los últimos datos.
        </div>
      )}
      <p className="muted small" aria-live="polite">
        {polling
          ? 'Actualizando cada 2 s mientras haya pedidos pendientes…'
          : 'Sin pedidos pendientes. Actualizando cada 15 s.'}
        {isFetching && ' ⟳'}
      </p>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Cliente</th>
              <th>SKU</th>
              <th className="num">Cantidad</th>
              <th>Estado</th>
              <th>Motivo de rechazo</th>
              <th>Creado</th>
            </tr>
          </thead>
          <tbody>
            {orders.map((order) => (
              <tr key={order.id}>
                <td>{order.nombreCliente}</td>
                <td>
                  <code>{order.sku}</code>
                </td>
                <td className="num">{order.cantidad}</td>
                <td>
                  <StatusBadge status={order.estado} />
                </td>
                <td className="reason">{rejectionText(order) ?? <span className="muted">—</span>}</td>
                <td className="nowrap">{dateFormat.format(new Date(order.creadoEn))}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}
