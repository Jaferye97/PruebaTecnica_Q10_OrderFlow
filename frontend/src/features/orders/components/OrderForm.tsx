import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react'
import { ApiError } from '../../../api/client'
import { useCreateOrder } from '../hooks/useCreateOrder'
import { useProducts } from '../hooks/useProducts'
import { ORDER_FIELDS, validateOrder, type FieldErrors, type OrderField, type OrderFormValues } from '../validation'

const EMPTY: OrderFormValues = { nombreCliente: '', sku: '', cantidad: '1' }

const isOrderField = (key: string): key is OrderField => (ORDER_FIELDS as readonly string[]).includes(key)

/** Separa un error de la API en errores por campo y un mensaje general (banner). */
function toFormErrors(error: Error): { fields: FieldErrors; banner: string | null } {
  if (!(error instanceof ApiError)) return { fields: {}, banner: 'Ocurrió un error inesperado.' }

  if (error.isValidation) {
    const fields: FieldErrors = {}
    const others: string[] = []
    for (const [key, messages] of Object.entries(error.problem.errors ?? {})) {
      if (isOrderField(key)) fields[key] = messages.join(' ')
      else others.push(...messages)
    }
    return { fields, banner: others.length > 0 ? others.join(' ') : null }
  }

  if (error.status === 0) return { fields: {}, banner: 'No se pudo conectar con el servidor. Inténtalo de nuevo.' }
  if (error.status >= 500)
    return { fields: {}, banner: 'El servidor tuvo un problema. Inténtalo de nuevo en unos segundos.' }

  // Otros 4xx (p. ej. 400 por JSON mal formado, sin `errors`)
  return { fields: {}, banner: error.problem.detail ?? error.problem.title ?? error.message }
}

export function OrderForm() {
  const [values, setValues] = useState<OrderFormValues>(EMPTY)
  const [errors, setErrors] = useState<FieldErrors>({})
  const [banner, setBanner] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  const products = useProducts()
  const createOrder = useCreateOrder()

  useEffect(() => {
    if (!success) return
    const timer = setTimeout(() => setSuccess(null), 4000)
    return () => clearTimeout(timer)
  }, [success])

  const handleChange = (e: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const field = e.target.name as OrderField
    const value = e.target.value
    setValues((v) => ({ ...v, [field]: value }))
    // El error de un campo desaparece en cuanto el usuario lo corrige
    setErrors((current) => {
      if (!current[field]) return current
      const next = { ...current }
      delete next[field]
      return next
    })
  }

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    setBanner(null)
    setSuccess(null)

    const clientErrors = validateOrder(values)
    setErrors(clientErrors)
    if (Object.keys(clientErrors).length > 0) return

    createOrder.mutate(
      {
        nombreCliente: values.nombreCliente.trim(),
        sku: values.sku.trim(),
        cantidad: Number(values.cantidad),
      },
      {
        onSuccess: (order) => {
          setValues(EMPTY)
          setErrors({})
          setSuccess(`Pedido de ${order.nombreCliente} creado (${order.cantidad} × ${order.sku}).`)
        },
        onError: (error) => {
          const result = toFormErrors(error)
          setErrors(result.fields)
          setBanner(result.banner)
        },
      },
    )
  }

  const submitting = createOrder.isPending

  const fieldProps = (field: OrderField) => ({
    id: field,
    name: field,
    value: values[field],
    onChange: handleChange,
    'aria-invalid': errors[field] ? true : undefined,
    'aria-describedby': errors[field] ? `${field}-error` : undefined,
  })

  const fieldError = (field: OrderField) =>
    errors[field] && (
      <p id={`${field}-error`} className="field-error">
        {errors[field]}
      </p>
    )

  const skuPlaceholder = products.isPending
    ? 'Cargando productos…'
    : products.isError
      ? 'No se pudo cargar el catálogo'
      : 'Selecciona un producto'

  return (
    <form onSubmit={handleSubmit} noValidate>
      {banner && (
        <div className="banner banner--error" role="alert">
          {banner}
        </div>
      )}
      {success && (
        <div className="banner banner--success" role="status">
          {success}
        </div>
      )}

      <div className="field">
        <label htmlFor="nombreCliente">Nombre del cliente</label>
        <input type="text" autoComplete="name" {...fieldProps('nombreCliente')} />
        {fieldError('nombreCliente')}
      </div>

      <div className="field">
        <label htmlFor="sku">Producto</label>
        <select {...fieldProps('sku')} disabled={products.isPending || products.isError}>
          <option value="">{skuPlaceholder}</option>
          {products.data?.map((p) => (
            <option key={p.sku} value={p.sku}>
              {p.nombre} ({p.sku})
            </option>
          ))}
        </select>
        {products.isError && (
          <button type="button" className="link small" onClick={() => products.refetch()}>
            Reintentar catálogo
          </button>
        )}
        {fieldError('sku')}
      </div>

      <div className="field">
        <label htmlFor="cantidad">Cantidad</label>
        <input type="number" inputMode="numeric" min={1} step={1} {...fieldProps('cantidad')} />
        {fieldError('cantidad')}
      </div>

      <button type="submit" className="primary" disabled={submitting}>
        {submitting ? 'Creando…' : 'Crear pedido'}
      </button>
    </form>
  )
}
