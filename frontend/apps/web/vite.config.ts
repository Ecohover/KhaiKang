import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { readFileSync } from 'node:fs'

const appVersion = readFileSync(new URL('../../../VERSION', import.meta.url), 'utf8').trim()

export default defineConfig({
  plugins: [vue()],
  define: {
    __KHAIKANG_VERSION__: JSON.stringify(appVersion),
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5220',
        changeOrigin: true,
      },
      '/openapi': {
        target: 'http://localhost:5220',
        changeOrigin: true,
      },
    },
  },
})
