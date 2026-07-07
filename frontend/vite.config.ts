/// <reference types="vitest/config" />
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  test: {
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
    css: false,
  },
  server: {
    proxy: {
      '/auth': {
        target: 'http://localhost:8080',
        changeOrigin: true,
      },
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
      '/audit/': {
        target: 'http://localhost:8080',
        changeOrigin: true,
      },
      '/reports/': {
        target: 'http://localhost:8080',
        changeOrigin: true,
      },
      '/notifications': {
        target: 'http://localhost:8080',
        changeOrigin: true,
      },
    },
  },
})
