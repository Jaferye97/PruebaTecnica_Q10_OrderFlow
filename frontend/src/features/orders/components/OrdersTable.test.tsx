import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import type { Order } from '../../../api/types'
import { json, mockFetch, order, renderWithQuery } from '../../../test/utils'
import { OrdersTable } from './OrdersTable'

// 23 pedidos "Cliente 1".."Cliente 23" -> 3 páginas de 10
const muchos: Order[] = Array.from({ length: 23 }, (_, i) => order({ nombreCliente: `Cliente ${i + 1}` }))

const filas = () => screen.getAllByRole('row').slice(1) // sin la cabecera
const primeraCelda = () => within(filas()[0]!).getAllByRole('cell')[0]!.textContent

describe('OrdersTable', () => {
  it('muestra estado en español y el motivo del rechazo', async () => {
    mockFetch(() =>
      json(200, [
        order({ nombreCliente: 'Rechazado', estado: 'Rejected', motivoRechazo: 'InsufficientStock', detalleRechazo: 'Disponible: 0, solicitado: 1' }),
        order({ nombreCliente: 'Pendiente', estado: 'Pending' }),
      ]),
    )
    renderWithQuery(<OrdersTable />)

    expect(await screen.findByText('Stock insuficiente: Disponible: 0, solicitado: 1')).toBeTruthy()
    expect(screen.getByText('Rechazado', { selector: '.badge' })).toBeTruthy()
    expect(screen.getByText('Pendiente', { selector: '.badge' })).toBeTruthy()
    // Hay un Pending -> polling rápido
    expect(screen.getByText(/Actualizando cada 2 s/)).toBeTruthy()
  })

  it('pagina de 10 en 10 y navega con Siguiente / número / Anterior', async () => {
    mockFetch(() => json(200, muchos))
    const user = userEvent.setup()
    renderWithQuery(<OrdersTable />)

    expect(await screen.findByText('Mostrando 1–10 de 23')).toBeTruthy()
    expect(filas()).toHaveLength(10)
    expect(screen.getByRole('button', { name: /Anterior/ })).toHaveProperty('disabled', true)

    await user.click(screen.getByRole('button', { name: /Siguiente/ }))
    expect(screen.getByText('Mostrando 11–20 de 23')).toBeTruthy()
    expect(primeraCelda()).toBe('Cliente 11')

    await user.click(screen.getByRole('button', { name: 'Página 3' }))
    expect(filas()).toHaveLength(3)
    expect(screen.getByRole('button', { name: 'Página 3' }).getAttribute('aria-current')).toBe('page')
    expect(screen.getByRole('button', { name: /Siguiente/ })).toHaveProperty('disabled', true)

    await user.click(screen.getByRole('button', { name: /Anterior/ }))
    expect(screen.getByText('Mostrando 11–20 de 23')).toBeTruthy()
  })

  it('con una sola página no muestra la paginación', async () => {
    mockFetch(() => json(200, muchos.slice(0, 5)))
    renderWithQuery(<OrdersTable />)

    await screen.findByText('Cliente 1')
    expect(screen.queryByRole('navigation', { name: 'Paginación de pedidos' })).toBeNull()
  })

  it('sin pedidos muestra el mensaje vacío', async () => {
    mockFetch(() => json(200, []))
    renderWithQuery(<OrdersTable />)

    expect(await screen.findByText('Todavía no hay pedidos. Crea el primero.')).toBeTruthy()
  })

  it('si la primera carga falla muestra el error con Reintentar', async () => {
    mockFetch(() => json(500, { status: 500, title: 'Error interno del servidor' }))
    renderWithQuery(<OrdersTable />)

    const alerta = await screen.findByRole('alert')
    expect(alerta.textContent).toContain('No se pudieron cargar los pedidos: Error interno del servidor')
    expect(within(alerta).getByRole('button', { name: 'Reintentar' })).toBeTruthy()
  })
})
