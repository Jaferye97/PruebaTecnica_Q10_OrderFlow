import { describe, expect, it } from 'vitest'
import { order } from '../../../test/utils'
import { ordersRefetchInterval } from './useOrders'

describe('ordersRefetchInterval (ritmo del polling)', () => {
  it('cada 2 s si algún pedido sigue Pending', () => {
    expect(ordersRefetchInterval([order({ estado: 'Confirmed' }), order({ estado: 'Pending' })])).toBe(2_000)
  })

  it('cada 15 s si todos están resueltos', () => {
    expect(ordersRefetchInterval([order({ estado: 'Confirmed' }), order({ estado: 'Rejected' })])).toBe(15_000)
  })

  it('cada 15 s sin pedidos o antes de la primera respuesta', () => {
    expect(ordersRefetchInterval([])).toBe(15_000)
    expect(ordersRefetchInterval(undefined)).toBe(15_000)
  })
})
