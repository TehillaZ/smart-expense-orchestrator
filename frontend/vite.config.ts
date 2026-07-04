import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      '/submissions': {
        target: 'http://localhost:8080',
        changeOrigin: true,
      },
      '/approvals/': {
        target: 'http://localhost:8080',
        changeOrigin: true,
      },
      '/policy': {
        target: 'http://localhost:8080',
        changeOrigin: true,
      },
      '/notifications': {
        target: 'http://localhost:5004',
        changeOrigin: true,
      },
    },
  },
})
