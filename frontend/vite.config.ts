import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { tma } from 'vite-plugin-telegram-mini-app'

export default defineConfig({
  cacheDir: process.env.REKI_VITE_CACHE_DIR ?? "node_modules/.vite",
  plugins: [
    react(),
    tma({
      botToken: process.env.TG_PAT,
      mode: 'sdk',
      sdkModule: '@tma.js/sdk',
      stateFile: 'dev/telegram-emulator-state.json',
      panel: false,
      eruda: false,
    }),
  ],
  server: {
    proxy: {
      '/api/v1': {
        target: process.env.REKI_BACKEND_PROXY_TARGET ?? 'http://127.0.0.1:8081',
        rewrite: path => path.replace(/^\/api\/v1/, ''),
      },
    },
  },
})
