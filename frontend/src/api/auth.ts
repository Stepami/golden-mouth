import { runtimeConfig } from '../app/runtimeConfig'
import type { OnboardingRequest, PlayerResponse } from './generated/reki-api'

const refreshKey = 'reki:refresh-token'
const loggedOutKey = 'reki:logged-out'
const pendingLoginKey = 'reki:pending-telegram-login'
const authTimeoutMs = 15_000
const baseUrl = runtimeConfig.apiBaseUrl
let initDataRaw = ''
let accessToken = ''
let registrationExternalId = ''
let refreshRequest: Promise<boolean> | undefined
let startupRequest: Promise<boolean> | undefined
let telegramClientIdRequest: Promise<number> | undefined
let widgetRequest: Promise<void> | undefined
let pendingWidgetToken = sessionStorage.getItem(pendingLoginKey) ?? ''
let pendingExpiryTimer: ReturnType<typeof setTimeout> | undefined
let explicitlyLoggedOut = sessionStorage.getItem(loggedOutKey) === '1'
const listeners = new Set<() => void>()

interface TokenResponse { access_token: string; refresh_token: string }

export const setInitData = (raw: string) => { if (raw !== initDataRaw) registrationExternalId = ''; initDataRaw = raw }
export const hasInitData = () => !!initDataRaw
export const getAccessToken = () => accessToken
export const hasAccess = () => !!accessToken
export const needsRegistration = () => !!registrationExternalId
export function subscribeAuth(listener: () => void): () => void {
  listeners.add(listener)
  return () => { listeners.delete(listener) }
}
const notify = () => { for (const listener of listeners) listener() }

export const hasPendingWidgetLogin = () => !!pendingWidgetToken
export const canResumeAuth = () => hasPendingWidgetLogin() || !!storedRefresh()

export function clearPendingWidgetLogin() {
  pendingWidgetToken = ''
  clearTimeout(pendingExpiryTimer)
  sessionStorage.removeItem(pendingLoginKey)
  registrationExternalId = ''
  notify()
}

function schedulePendingExpiry() {
  clearTimeout(pendingExpiryTimer)
  if (!pendingWidgetToken) return
  try {
    const payload = pendingWidgetToken.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    const { exp } = JSON.parse(atob(payload)) as { exp?: number }
    // This only expires the local retry; the backend validates the token.
    if (typeof exp !== 'number' || exp * 1000 <= Date.now()) {
      clearPendingWidgetLogin()
      return
    }
    pendingExpiryTimer = setTimeout(clearPendingWidgetLogin, exp * 1000 - Date.now())
  } catch {
    clearPendingWidgetLogin()
  }
}

schedulePendingExpiry()

export function loadTelegramClientId(): Promise<number> {
  return telegramClientIdRequest ??= fetch(`${baseUrl}/auth/external/tg/client-id`, { cache: 'no-store', signal: AbortSignal.timeout(authTimeoutMs) }).then(async response => {
    if (!response.ok) throw new Error(`Telegram client ID unavailable (${response.status})`)
    return response.json() as Promise<number>
  }).catch(error => {
    telegramClientIdRequest = undefined
    throw error
  })
}

function storedRefresh(): string | null {
  try { return localStorage.getItem(refreshKey) } catch { return null }
}
function saveTokens(tokens: TokenResponse) {
  localStorage.setItem(refreshKey, tokens.refresh_token)
  accessToken = tokens.access_token
  registrationExternalId = ''
  clearPendingWidgetLogin()
}
export function clearTokens() {
  accessToken = ''
  registrationExternalId = ''
  try { localStorage.removeItem(refreshKey) } catch { /* storage unavailable */ }
  notify()
}
async function tokenRequest(path: string, fields: Record<string, string>, authorization: string): Promise<TokenResponse | undefined> {
  const response = await fetch(`${baseUrl}${path}`, {
    method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded', Authorization: authorization },
    body: new URLSearchParams({ client_id: runtimeConfig.appClientId, ...fields }),
    signal: AbortSignal.timeout(authTimeoutMs),
  })
  if (!response.ok) {
    const body = response.status === 400 ? await response.json().catch(() => null) as { error?: string; external_user_id?: string } | null : null
    if (body?.error === 'registration_required' && body.external_user_id) { registrationExternalId = body.external_user_id; notify(); return undefined }
    throw new Error(`Authorization failed (${response.status})`)
  }
  return response.json() as Promise<TokenResponse>
}
export async function loginMiniApp(): Promise<void> {
  const tokens = await tokenRequest('/auth/external/tg/miniapp', { grant_type: 'urn:reki:grant:telegram-mini-app' }, `TMiniApp ${initDataRaw}`)
  if (tokens) saveTokens(tokens)
  explicitlyLoggedOut = false
  sessionStorage.removeItem(loggedOutKey)
}
export async function loginWidget(idToken: string): Promise<void> {
  registrationExternalId = ''
  sessionStorage.setItem(pendingLoginKey, idToken)
  pendingWidgetToken = idToken
  schedulePendingExpiry()
  notify()
  return resumeWidgetLogin()
}
export function resumeWidgetLogin(): Promise<void> {
  if (widgetRequest) return widgetRequest
  schedulePendingExpiry()
  const idToken = pendingWidgetToken
  if (!idToken) return Promise.reject(new Error('Telegram login expired'))
  widgetRequest = (async () => {
    const tokens = await tokenRequest('/auth/external/tg/widget', { grant_type: 'urn:reki:grant:telegram-login' }, `Bearer ${idToken}`)
    if (pendingWidgetToken !== idToken) return
    if (tokens) saveTokens(tokens)
    explicitlyLoggedOut = false
    sessionStorage.removeItem(loggedOutKey)
  })().finally(() => { widgetRequest = undefined })
  return widgetRequest
}
export function refreshAccess(): Promise<boolean> {
  if (refreshRequest) return refreshRequest
  const refreshToken = storedRefresh()
  if (!refreshToken) return Promise.resolve(false)
  refreshRequest = (async () => {
    const response = await fetch(`${baseUrl}/auth/refresh`, {
      method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams({ client_id: runtimeConfig.appClientId, grant_type: 'refresh_token', refresh_token: refreshToken }),
      signal: AbortSignal.timeout(authTimeoutMs),
    })
    if (!response.ok) {
      const failure = response.status === 400
        ? await response.json().catch(() => null) as { error?: string } | null
        : null
      if (failure?.error === 'invalid_grant') {
        clearTokens()
        return false
      }
      throw new Error(`Session restore failed (${response.status})`)
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
    if (hasPendingWidgetLogin()) {
      await resumeWidgetLogin()
      return hasAccess()
    }
    if (initDataRaw && !explicitlyLoggedOut) {
      await loginMiniApp()
      return hasAccess()
    }
    return false
  })().finally(() => { startupRequest = undefined })
}
export async function registerPlayer(body: OnboardingRequest): Promise<PlayerResponse> {
  if (!hasAccess()) {
    if (!registrationExternalId) throw new Error('Registration identity unavailable')
    const authorization = `Basic ${btoa(String.fromCharCode(...new TextEncoder().encode(registrationExternalId)))}`
    const response = await fetch(`${baseUrl}/onboarding`, {
      method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: authorization },
      body: JSON.stringify(body), signal: AbortSignal.timeout(authTimeoutMs),
    })
    if (!response.ok && response.status !== 409) throw new Error(`Registration failed (${response.status})`)
    if (pendingWidgetToken) await resumeWidgetLogin()
    else await loginMiniApp()
    if (!hasAccess()) throw new Error('Registration could not be confirmed')
  }
  const response = await fetch(`${baseUrl}/me`, { headers: { Authorization: `Bearer ${accessToken}` } })
  if (!response.ok) throw new Error(`Profile unavailable (${response.status})`)
  return response.json() as Promise<PlayerResponse>
}
export async function logout(): Promise<void> {
  const refreshToken = storedRefresh()
  if (refreshToken) {
    const response = await fetch(`${baseUrl}/auth/logout`, {
      method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams({ refresh_token: refreshToken }),
    })
    if (!response.ok) throw new Error(`Logout failed (${response.status})`)
  }
  explicitlyLoggedOut = true
  sessionStorage.setItem(loggedOutKey, '1')
  clearPendingWidgetLogin()
  clearTokens()
}
