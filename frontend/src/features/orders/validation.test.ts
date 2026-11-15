import { describe, expect, it } from 'vitest'
import { CANTIDAD_MAX, CANTIDAD_MIN, validateOrder, type OrderFormValues } from './validation'

const valido: OrderFormValues = { nombreCliente: 'Ana', sku: 'SKU-001', cantidad: '2' }

describe('validateOrder (mismas reglas que Order.Create del dominio)', () => {
  it('un pedido válido no tiene errores', () => {
    expect(validateOrder(valido)).toEqual({})
  })

  it('nombre y SKU vacíos o solo espacios son obligatorios', () => {
    const errors = validateOrder({ nombreCliente: '   ', sku: '', cantidad: '1' })

    expect(errors.nombreCliente).toBe('El nombre del cliente es obligatorio.')
    expect(errors.sku).toBe('El SKU es obligatorio.')
    expect(errors.cantidad).toBeUndefined()
  })

  it('los límites del rango son el mismo que en el backend (1 y 100)', () => {
    expect([CANTIDAD_MIN, CANTIDAD_MAX]).toEqual([1, 100])
  })

  it.each(['1', '100'])('cantidad %s está dentro del rango', (cantidad) => {
    expect(validateOrder({ ...valido, cantidad }).cantidad).toBeUndefined()
  })

  it.each(['0', '101', '-1', '1.5', 'abc', '', '  '])('cantidad "%s" es inválida', (cantidad) => {
    expect(validateOrder({ ...valido, cantidad }).cantidad).toBe('La cantidad debe estar entre 1 y 100.')
  })

  it('acumula los tres errores a la vez', () => {
    expect(Object.keys(validateOrder({ nombreCliente: '', sku: '', cantidad: '0' }))).toEqual([
      'nombreCliente',
      'sku',
      'cantidad',
    ])
  })
})
