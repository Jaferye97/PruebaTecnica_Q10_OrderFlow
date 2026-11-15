import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { json, mockFetch, order, renderWithQuery } from '../../../test/utils'
import { OrderForm } from './OrderForm'

const products = [
  { sku: 'SKU-001', nombre: 'Teclado mecánico', stock: 100 },
  { sku: 'SKU-002', nombre: 'Mouse inalámbrico', stock: 10 },
]

/** fetch simulado: GET productos ok; el POST responde lo que diga cada test. */
function api(post: () => Response) {
  return mockFetch((_url, init) => (init?.method === 'POST' ? post() : json(200, products)))
}

const posts = (fetch: ReturnType<typeof mockFetch>) =>
  fetch.mock.calls.filter(([, init]) => init?.method === 'POST')

async function rellenar(nombre: string, sku: string, cantidad: string) {
  const user = userEvent.setup()
  if (nombre) await user.type(screen.getByLabelText('Nombre del cliente'), nombre)
  await screen.findByRole('option', { name: /Teclado/ }) // espera a que cargue el catálogo
  if (sku) await user.selectOptions(screen.getByLabelText('Producto'), sku)
  const cantidadInput = screen.getByLabelText('Cantidad')
  await user.clear(cantidadInput)
  if (cantidad) await user.type(cantidadInput, cantidad)
  await user.click(screen.getByRole('button', { name: 'Crear pedido' }))
}

describe('OrderForm', () => {
  it('carga el catálogo en el select', async () => {
    api(() => json(201, order()))
    renderWithQuery(<OrderForm />)

    expect(await screen.findByRole('option', { name: 'Teclado mecánico (SKU-001)' })).toBeTruthy()
    expect(screen.getByRole('option', { name: 'Mouse inalámbrico (SKU-002)' })).toBeTruthy()
  })

  it('con datos inválidos muestra los errores en pantalla y NO llama a la API', async () => {
    const fetch = api(() => json(201, order()))
    renderWithQuery(<OrderForm />)

    await rellenar('', '', '101')

    expect(screen.getByText('El nombre del cliente es obligatorio.')).toBeTruthy()
    expect(screen.getByText('El SKU es obligatorio.')).toBeTruthy()
    expect(screen.getByText('La cantidad debe estar entre 1 y 100.')).toBeTruthy()
    expect(screen.getByLabelText('Cantidad').getAttribute('aria-invalid')).toBe('true')
    expect(posts(fetch)).toHaveLength(0)
  })

  it('un pedido válido se envía y muestra la confirmación', async () => {
    const fetch = api(() => json(201, order({ nombreCliente: 'Ana', cantidad: 2, sku: 'SKU-001', estado: 'Pending' })))
    renderWithQuery(<OrderForm />)

    await rellenar('  Ana  ', 'SKU-001', '2')

    expect(await screen.findByRole('status')).toHaveProperty('textContent', 'Pedido de Ana creado (2 × SKU-001).')
    expect(JSON.parse(String(posts(fetch)[0]?.[1]?.body))).toEqual({ nombreCliente: 'Ana', sku: 'SKU-001', cantidad: 2 })
  })

  it('el error de validación del servidor aparece debajo de su campo', async () => {
    api(() =>
      json(400, {
        status: 400,
        title: 'La solicitud tiene errores de validación',
        errors: { sku: ["El SKU 'SKU-002' no existe en el catálogo."] },
        codes: ['order.sku_no_existe'],
      }),
    )
    renderWithQuery(<OrderForm />)

    await rellenar('Ana', 'SKU-002', '1')

    expect(await screen.findByText("El SKU 'SKU-002' no existe en el catálogo.")).toBeTruthy()
    expect(screen.getByLabelText('Producto').getAttribute('aria-invalid')).toBe('true')
  })

  it('si el servidor falla (5xx) muestra un banner de error', async () => {
    api(() => json(500, { status: 500, title: 'Error interno del servidor' }))
    renderWithQuery(<OrderForm />)

    await rellenar('Ana', 'SKU-001', '1')

    expect((await screen.findByRole('alert')).textContent).toBe(
      'El servidor tuvo un problema. Inténtalo de nuevo en unos segundos.',
    )
  })
})
