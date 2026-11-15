import { describe, expect, it } from 'vitest'
import { json, mockFetch } from '../test/utils'
import { ApiError, apiFetch } from './client'

describe('apiFetch', () => {
  it('antepone /api y devuelve el JSON si la respuesta es ok', async () => {
    const fetch = mockFetch(() => json(200, [{ sku: 'SKU-001' }]))

    await expect(apiFetch('/products')).resolves.toEqual([{ sku: 'SKU-001' }])
    expect(fetch).toHaveBeenCalledWith('/api/products', expect.anything())
  })

  it('un 400 de validación lanza ApiError con los errores por campo', async () => {
    mockFetch(() => json(400, { status: 400, title: 'Errores', errors: { cantidad: ['Fuera de rango'] } }))

    const error = await apiFetch('/orders', { method: 'POST', body: '{}' }).catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).isValidation).toBe(true)
    expect((error as ApiError).problem.errors?.cantidad).toEqual(['Fuera de rango'])
  })

  it('un 502 sin JSON (p. ej. de nginx) usa el status y el statusText', async () => {
    mockFetch(() => new Response('<html>bad gateway</html>', { status: 502, statusText: 'Bad Gateway' }))

    const error = (await apiFetch('/orders').catch((e: unknown) => e)) as ApiError

    expect(error.status).toBe(502)
    expect(error.message).toBe('Bad Gateway')
    expect(error.isClientError).toBe(false)
  })

  it('sin red: ApiError con status 0', async () => {
    mockFetch(() => {
      throw new TypeError('Failed to fetch')
    })

    const error = (await apiFetch('/orders').catch((e: unknown) => e)) as ApiError

    expect(error.status).toBe(0)
    expect(error.message).toBe('No se pudo conectar con el servidor')
  })
})
