import type { Table } from '@tanstack/react-table'

/** Controles de paginación para cualquier tabla de TanStack Table. No se muestra si hay una sola página. */
export function TablePagination<T>({ table }: { table: Table<T> }) {
  const pageCount = table.getPageCount()
  if (pageCount <= 1) return null

  const { pageIndex, pageSize } = table.getState().pagination
  const total = table.getCoreRowModel().rows.length
  const first = pageIndex * pageSize + 1
  const last = Math.min((pageIndex + 1) * pageSize, total)

  return (
    <nav className="pagination" aria-label="Paginación de pedidos">
      <span className="muted small">
        Mostrando {first}–{last} de {total}
      </span>
      <div className="pagination__controls">
        <button type="button" onClick={() => table.previousPage()} disabled={!table.getCanPreviousPage()}>
          ‹ Anterior
        </button>
        {Array.from({ length: pageCount }, (_, i) => (
          <button
            key={i}
            type="button"
            onClick={() => table.setPageIndex(i)}
            aria-current={i === pageIndex ? 'page' : undefined}
            aria-label={`Página ${i + 1}`}
          >
            {i + 1}
          </button>
        ))}
        <button type="button" onClick={() => table.nextPage()} disabled={!table.getCanNextPage()}>
          Siguiente ›
        </button>
      </div>
    </nav>
  )
}
