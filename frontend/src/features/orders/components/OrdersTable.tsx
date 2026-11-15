import { useEffect } from 'react'
import {
  createColumnHelper,
  flexRender,
  getCoreRowModel,
  getPaginationRowModel,
  useReactTable,
} from '@tanstack/react-table'
import type { Order, RejectionReason } from '../../../api/types'
import { useOrders } from '../hooks/useOrders'
import { StatusBadge } from './StatusBadge'
import { TablePagination } from './TablePagination'

/** Filas por página. Se pagina en el cliente la lista que devuelve GET /api/orders (los 50 más recientes). */
const PAGE_SIZE = 10

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

// Columnas fuera del componente: referencia estable (TanStack no recalcula en cada render)
const column = createColumnHelper<Order>()

const columns = [
  column.accessor('nombreCliente', { header: 'Cliente' }),
  column.accessor('sku', { header: 'SKU', cell: (c) => <code>{c.getValue()}</code> }),
  column.accessor('cantidad', { header: 'Cantidad', meta: { className: 'num' } }),
  column.accessor('estado', { header: 'Estado', cell: (c) => <StatusBadge status={c.getValue()} /> }),
  column.display({
    id: 'motivo',
    header: 'Motivo de rechazo',
    cell: (c) => rejectionText(c.row.original) ?? <span className="muted">—</span>,
    meta: { className: 'reason' },
  }),
  column.accessor('creadoEn', {
    header: 'Creado',
    cell: (c) => dateFormat.format(new Date(c.getValue())),
    meta: { className: 'nowrap' },
  }),
]

// Referencia estable mientras no hay datos (un [] nuevo en cada render haría recalcular la tabla)
const NO_ORDERS: Order[] = []

export function OrdersTable() {
  const { data: orders, isPending, isError, error, refetch, isFetching } = useOrders()

  // Los hooks van antes de los "return" tempranos: siempre se llaman en el mismo orden
  const table = useReactTable({
    data: orders ?? NO_ORDERS,
    columns,
    getRowId: (order) => order.id,
    getCoreRowModel: getCoreRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    // Por defecto vuelve a la página 1 cada vez que cambian los datos: con el polling cada 2 s
    // el usuario no podría quedarse en la página 3. Se desactiva.
    autoResetPageIndex: false,
    initialState: { pagination: { pageIndex: 0, pageSize: PAGE_SIZE } },
  })

  // Si un refresco deja menos páginas que la actual, se salta a la última que existe
  const pageCount = table.getPageCount()
  const { pageIndex } = table.getState().pagination
  useEffect(() => {
    if (pageCount > 0 && pageIndex >= pageCount) table.setPageIndex(pageCount - 1)
  }, [pageCount, pageIndex, table])

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

  // El ritmo del polling depende de TODOS los pedidos, no solo de la página visible
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
            {table.getHeaderGroups().map((group) => (
              <tr key={group.id}>
                {group.headers.map((header) => (
                  <th key={header.id} className={header.column.columnDef.meta?.className}>
                    {flexRender(header.column.columnDef.header, header.getContext())}
                  </th>
                ))}
              </tr>
            ))}
          </thead>
          <tbody>
            {table.getRowModel().rows.map((row) => (
              <tr key={row.id}>
                {row.getVisibleCells().map((cell) => (
                  <td key={cell.id} className={cell.column.columnDef.meta?.className}>
                    {flexRender(cell.column.columnDef.cell, cell.getContext())}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <TablePagination table={table} />
    </>
  )
}
