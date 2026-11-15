import type { ProblemDetails } from './types'

/**
 * Error de la API con el ProblemDetails ya parseado.
 * `status === 0` significa que no hubo respuesta (red caída, proxy sin backend, CORS...).
 */
export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails

  constructor(status: number, problem: ProblemDetails) {
    super(problem.title ?? `Error HTTP ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }

  get isValidation(): boolean {
    return this.status === 400 && this.problem.errors !== undefined
  }

  get isClientError(): boolean {
    return this.status >= 400 && this.status < 500
  }
}

async function parseProblem(response: Response): Promise<ProblemDetails> {
  try {
    const body: unknown = await response.json()
    if (body && typeof body === 'object') return body as ProblemDetails
  } catch {
    // Cuerpo vacío o no-JSON (p. ej. un 502 de nginx): se usa el status
  }
  return { status: response.status, title: response.statusText || undefined }
}

export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers)
  headers.set('Accept', 'application/json')
  if (init?.body !== undefined) headers.set('Content-Type', 'application/json')

  let response: Response
  try {
    response = await fetch(`/api${path}`, { ...init, headers })
  } catch (cause) {
    // Cancelaciones de TanStack Query: se propagan tal cual
    if (cause instanceof DOMException && cause.name === 'AbortError') throw cause
    throw new ApiError(0, { title: 'No se pudo conectar con el servidor' })
  }

  if (!response.ok) throw new ApiError(response.status, await parseProblem(response))

  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}
