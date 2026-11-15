import { cleanup } from '@testing-library/react'
import { afterEach, vi } from 'vitest'

// Sin "globals: true", Testing Library no desmonta solo: se hace aquí tras cada test.
// También se restauran los fetch simulados con vi.stubGlobal.
afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})
