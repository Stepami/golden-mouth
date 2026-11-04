import { defineConfig } from 'vite'
import { readFileSync } from 'node:fs'
import react from '@vitejs/plugin-react'
import { tma } from 'vite-plugin-telegram-mini-app'

const certPath = process.env.REKI_HTTPS_CERT_PATH
const keyPath = process.env.REKI_HTTPS_KEY_PATH

export default defineConfig({
  plugins: [
    react(),
    ...(process.env.REKI_BROWSER_LOGIN === '1' ? [] : [tma({
      botToken: process.env.TG_PAT,
      mode: 'sdk',
      sdkModule: '@tma.js/sdk',
      stateFile: 'dev/telegram-emulator-state.json',
      panel: false,
      eruda: false,
    })]),
  ],
  server: {
    https: certPath && keyPath ? { cert: readFileSync(certPath), key: readFileSync(keyPath) } : undefined,
    proxy: {
      '/api/v1': {
        target: process.env.REKI_BACKEND_PROXY_TARGET ?? 'http://127.0.0.1:8081',
        rewrite: path => path.replace(/^\/api\/v1/, ''),
      },
    },
  },
})
