import { runtimeConfig } from '../app/runtimeConfig'

interface TelegramLoginResult { id_token?: string; error?: string }
interface TelegramLoginSdk {
  auth(options: { client_id: number; scope: string[] }, callback: (result: TelegramLoginResult) => void): void
}
declare global { interface Window { Telegram?: { Login: TelegramLoginSdk } } }

let loading: Promise<void> | undefined
export function preloadTelegramLogin(): Promise<void> {
  if (window.Telegram?.Login) return Promise.resolve()
  return loading ??= new Promise((resolve, reject) => {
    const script = document.createElement('script')
    script.src = 'https://telegram.org/js/telegram-login.js'
    script.async = true
    script.onload = () => window.Telegram?.Login ? resolve() : reject(new Error('Telegram Login SDK unavailable'))
    script.onerror = () => reject(new Error('Telegram Login SDK failed to load'))
    document.head.append(script)
  })
}

export function openTelegramLogin(onToken: (idToken: string) => void, onError: () => void): void {
  // Called directly from the button click so the browser permits the popup.
  window.Telegram?.Login.auth({ client_id: runtimeConfig.telegramClientId, scope: ['profile'] }, result => {
    if (result.id_token) onToken(result.id_token)
    else if (result.error) onError()
  })
}
