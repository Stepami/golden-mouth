import { runtimeConfig } from '../app/runtimeConfig'

const refreshKey = 'reki:refresh-token'
const loggedOutKey = 'reki:logged-out'
const baseUrl = runtimeConfig.apiBaseUrl
let initDataRaw = ''
let accessToken = ''
let refreshRequest: Promise<boolean> | undefined
let startupRequest: Promise<boolean> | undefined
let telegramClientIdRequest: Promise<number> | undefined
let explicitlyLoggedOut = sessionStorage.getItem(loggedOutKey) === '1'
const listeners = new Set<() => void>()

interface TokenResponse { access_token: string; refresh_token: string }

export const setInitData = (raw: string) => { initDataRaw = raw }
export const hasInitData = () => !!initDataRaw
export const getAccessToken = () => accessToken
export const hasAccess = () => !!accessToken
export function subscribeAuth(listener: () => void): () => void {
  listeners.add(listener)
  return () => { listeners.delete(listener) }
}
const notify = () => { for (const listener of listeners) listener() }

export function loadTelegramClientId(): Promise<number> {
  return telegramClientIdRequest ??= fetch(`${baseUrl}/auth/external/tg/client-id`, { cache: 'no-store' }).then(async response => {
    if (!response.ok) throw new Error(`Telegram client ID unavailable (${response.status})`)
    return response.json() as Promise<number>
  })
}

function storedRefresh(): string | null {
  try { return localStorage.getItem(refreshKey) } catch { return null }
}
function saveTokens(tokens: TokenResponse) {
  accessToken = tokens.access_token
  localStorage.setItem(refreshKey, tokens.refresh_token)
  notify()
}
export function clearTokens() {
  accessToken = ''
  try { localStorage.removeItem(refreshKey) } catch { /* storage unavailable */ }
  notify()
}
async function tokenRequest(path: string, fields: Record<string, string>, authorization: string): Promise<TokenResponse> {
  const response = await fetch(`${baseUrl}${path}`, {
    method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded', Authorization: authorization },
    body: new URLSearchParams({ client_id: runtimeConfig.appClientId, ...fields }),
  })
  if (!response.ok) throw new Error(`Authorization failed (${response.status})`)
  return response.json() as Promise<TokenResponse>
}
export async function loginMiniApp(): Promise<void> {
  const tokens = await tokenRequest('/auth/external/tg/miniapp', { grant_type: 'urn:reki:grant:telegram-mini-app' }, `TMiniApp ${initDataRaw}`)
  saveTokens(tokens)
  explicitlyLoggedOut = false
  sessionStorage.removeItem(loggedOutKey)
}
export async function loginWidget(idToken: string): Promise<void> {
  const tokens = await tokenRequest('/auth/external/tg/widget', { grant_type: 'urn:reki:grant:telegram-login' }, `Bearer ${idToken}`)
  saveTokens(tokens)
  explicitlyLoggedOut = false
  sessionStorage.removeItem(loggedOutKey)
}
export function refreshAccess(): Promise<boolean> {
  if (refreshRequest) return refreshRequest
  const refreshToken = storedRefresh()
  if (!refreshToken) return Promise.resolve(false)
  refreshRequest = (async () => {
    const response = await fetch(`${baseUrl}/auth/refresh`, {
      method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams({ client_id: runtimeConfig.appClientId, grant_type: 'refresh_token', refresh_token: refreshToken }),
    })
    if (!response.ok) {
      if (response.status >= 400 && response.status < 500) clearTokens()
      return false
    }
    saveTokens(await response.json() as TokenResponse)
    return true
  })().finally(() => { refreshRequest = undefined })
  return refreshRequest
}
export function startAuth(): Promise<boolean> {
  return startupRequest ??= (async () => {
    if (accessToken) return true
    if (storedRefresh()) {
      const restored = await refreshAccess()
      if (restored) return true
    }
    if (initDataRaw && !explicitlyLoggedOut) {
      await loginMiniApp()
      return true
    }
    return false
  })().finally(() => { startupRequest = undefined })
}
export async function logout(): Promise<void> {
  const refreshToken = storedRefresh()
  if (refreshToken) {
    const response = await fetch(`${baseUrl}/auth/logout`, {
      method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams({ client_id: runtimeConfig.appClientId, refresh_token: refreshToken }),
    })
    if (!response.ok) throw new Error(`Logout failed (${response.status})`)
  }
  explicitlyLoggedOut = true
  sessionStorage.setItem(loggedOutKey, '1')
  clearTokens()
}
