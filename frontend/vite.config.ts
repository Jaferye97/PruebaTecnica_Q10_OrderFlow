import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Las rutas del backend ya empiezan por /api (MapGroup("/api/orders")),
// así que el proxy reenvía la ruta tal cual: quitar /api daría 404.
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:8080',
        changeOrigin: true,
      },
    },
  },
})
