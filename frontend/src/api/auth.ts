import { runtimeConfig } from '../app/runtimeConfig'
import type { OnboardingRequest, PlayerResponse } from './generated/reki-api'

const refreshKey = 'reki:refresh-token'
const providerKey = 'reki:auth-provider'
export type AuthProvider = 'telegram' | 'vk' | 'yandex'
export const currentAuthProvider = (): AuthProvider | undefined => {
  const value = localStorage.getItem(providerKey)
  return value === 'telegram' || value === 'vk' || value === 'yandex' ? value : undefined
}
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
let pendingYandexToken = ''
let pendingYandexExpiresAt = 0
let pendingVkToken = ''
let pendingVkExpiresAt = 0
let pendingExpiryTimer: ReturnType<typeof setTimeout> | undefined
let explicitlyLoggedOut = sessionStorage.getItem(loggedOutKey) === '1'
const listeners = new Set<() => void>()

interface TokenResponse { access_token: string; refresh_token: string }

export const setInitData = (raw: string) => { if (raw !== initDataRaw) registrationExternalId = ''; initDataRaw = raw }
export const hasInitData = () => !!initDataRaw
export const getInitData = () => initDataRaw
export const getAccessToken = () => accessToken
export const hasAccess = () => !!accessToken
export const needsRegistration = () => !!registrationExternalId
export function subscribeAuth(listener: () => void): () => void {
  listeners.add(listener)
  return () => { listeners.delete(listener) }
}
const notify = () => { for (const listener of listeners) listener() }

export const hasPendingWidgetLogin = () => !!pendingWidgetToken
export const hasPendingLogin = () => hasPendingWidgetLogin() || !!pendingVkToken || !!pendingYandexToken
export const pendingAuthProvider = (): 'telegram' | 'vk' | 'yandex' | undefined => pendingYandexToken ? 'yandex' : pendingVkToken ? 'vk' : pendingWidgetToken ? 'telegram' : undefined
export const canResumeAuth = () => hasPendingLogin() || !!storedRefresh()

export function clearPendingLogin() {
  pendingYandexToken = ''
  pendingYandexExpiresAt = 0
  pendingVkToken = ''
  pendingVkExpiresAt = 0
  clearPendingWidgetLogin()
}

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
function saveTokens(tokens: TokenResponse, provider?: AuthProvider) {
  if (provider) localStorage.setItem(providerKey, provider)
  localStorage.setItem(refreshKey, tokens.refresh_token)
  accessToken = tokens.access_token
  registrationExternalId = ''
  clearPendingLogin()
}
export function clearTokens() {
  accessToken = ''
  registrationExternalId = ''
  try { localStorage.removeItem(refreshKey); localStorage.removeItem(providerKey) } catch { /* storage unavailable */ }
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
    if ((path === '/auth/external/vk' || path === '/auth/external/yandex') && (response.status === 400 || response.status === 401)) clearPendingLogin()
    throw new Error(`Authorization failed (${response.status})`)
  }
  return response.json() as Promise<TokenResponse>
}
export async function loginMiniApp(): Promise<void> {
  const tokens = await tokenRequest('/auth/external/tg/miniapp', { grant_type: 'urn:reki:grant:telegram-mini-app' }, `TMiniApp ${initDataRaw}`)
  if (tokens) saveTokens(tokens, 'telegram')
  explicitlyLoggedOut = false
  sessionStorage.removeItem(loggedOutKey)
}
export async function loginWidget(idToken: string): Promise<void> {
  pendingVkToken = ''
  pendingYandexToken = ''
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
    if (tokens) saveTokens(tokens, 'telegram')
    explicitlyLoggedOut = false
    sessionStorage.removeItem(loggedOutKey)
  })().finally(() => { widgetRequest = undefined })
  return widgetRequest
}
export async function loginVk(token: string, expiresIn: number): Promise<void> {
  clearPendingLogin()
  pendingVkToken = token
  pendingVkExpiresAt = Date.now() + expiresIn * 1000
  notify()
  await resumeVkLogin()
}
async function resumeVkLogin(): Promise<void> {
  if (!pendingVkToken || pendingVkExpiresAt <= Date.now()) {
    clearPendingLogin()
    throw new Error('VK login expired')
  }
  const proof = pendingVkToken
  const tokens = await tokenRequest('/auth/external/vk', { grant_type: 'urn:reki:grant:vk' }, `Bearer ${proof}`)
  if (proof !== pendingVkToken) return
  if (tokens) saveTokens(tokens, 'vk')
  explicitlyLoggedOut = false
  sessionStorage.removeItem(loggedOutKey)
}
export async function loginYandex(token: string, expiresIn: number): Promise<void> {
  clearPendingLogin()
  pendingYandexToken = token
  pendingYandexExpiresAt = Date.now() + expiresIn * 1000
  notify()
  await resumeYandexLogin()
}
async function resumeYandexLogin(): Promise<void> {
  if (!pendingYandexToken || pendingYandexExpiresAt <= Date.now()) {
    clearPendingLogin()
    throw new Error('Yandex login expired')
  }
  const proof = pendingYandexToken
  const tokens = await tokenRequest('/auth/external/yandex', { grant_type: 'urn:reki:grant:yandex' }, `Bearer ${proof}`)
  if (proof !== pendingYandexToken) return
  if (tokens) saveTokens(tokens, 'yandex')
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
    if (pendingYandexToken) {
      await resumeYandexLogin()
      return hasAccess()
    }
    if (pendingVkToken) {
      await resumeVkLogin()
      return hasAccess()
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
    const authorization = `Basic ${registrationExternalId}`
    const response = await fetch(`${baseUrl}/auth/onboarding`, {
      method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: authorization },
      body: JSON.stringify(body), signal: AbortSignal.timeout(authTimeoutMs),
    })
    if (!response.ok && response.status !== 409) throw new Error(`Registration failed (${response.status})`)
    if (pendingYandexToken) await resumeYandexLogin()
    else if (pendingVkToken) await resumeVkLogin()
    else if (pendingWidgetToken) await resumeWidgetLogin()
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
  clearPendingLogin()
  clearTokens()
}
