import '@tanstack/react-table'
import type { RowData } from '@tanstack/react-table'

// Permite poner la clase CSS de cada columna en columnDef.meta (num, nowrap, reason) con tipos
declare module '@tanstack/react-table' {
  interface ColumnMeta<TData extends RowData, TValue> {
    className?: string
  }
}
