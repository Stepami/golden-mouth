// Продуктовая аналитика: кто куда ходит и что делает. Отдельно от журнала ошибок (app/log.ts).
// Таксономия, воронки и вопросы: docs/analytics.md. Приватность: только обезличенный id, без текстов, ников, initData.
import { apiBaseUrl as BASE } from './runtimeConfig'
import { isLandingPreview, landingContext } from './landing/experiment'
import { recordEntryStep, flushEntryEvents, linkEntryProfile, resetEntryProfileLink } from './landing/openobserve'
import { ActiveTime } from './activeTime'
import { inTelegram } from '../telegram'
import { getSessionId, sendAnalytics } from '../api/telemetry'
import type { AnalyticsBatchRequest, AnalyticsEventRequest } from '../api/generated/reki-api'
type Props = Record<string, string | number | boolean | null>
export type AEvent = AnalyticsEventRequest & { t: number; props?: Props }

let playerAvailable = false
export const canSendTelemetry = () => playerAvailable
export function setTelemetryPlayerAvailable(available: boolean) {
  playerAvailable = available
  if (available) { flushEvents(); void linkEntryProfile() }
  else { q.length = 0; resetEntryProfileLink() }
}

let selectedDifficulty: string | undefined
let selectedWorldId: string | undefined
export function setAnalyticsWorld(worldId: string | undefined, difficulty?: string) { selectedWorldId = worldId; selectedDifficulty = difficulty }

const activeTime = new ActiveTime(performance.now(), document.visibilityState === 'visible')
let installed = false
function recordActiveTime() {
  const ms = activeTime.take(performance.now())
  if (ms > 0) event('active_time', { ms })
}

const q: AEvent[] = []
let screen = '', prev: string | null = null, enteredAt = Date.now()
const platform = (() => { try { return new URLSearchParams(location.hash.slice(1)).get('tgWebAppPlatform') ?? 'web' } catch { return 'web' } })()
/** Флаги экспериментов (A/B), приходят с бэкенда в /me. Пример: { plus_offer: '10' | '20' }. */
let experiments: Record<string, string> = {}
export const setExperiments = (e: Record<string, string>) => { experiments = e }

const clean = (p?: Props) => p && Object.fromEntries(Object.entries(p).map(([k, v]) => [k, typeof v === 'string' && v.length > 64 ? `len:${v.length}` : v]))

export function event(name: string, props?: Props) {
  if (q.length >= 100) q.shift()
  q.push({ worldId: selectedWorldId, name, t: Date.now(), screen, prev, props: clean({ ...(selectedWorldId ? { difficulty: selectedDifficulty ?? null } : {}), ...props }) })
  if (!BASE && import.meta.env.DEV) console.debug('[event]', name, screen, props ?? '')
  if (q.length >= 25) flushEvents()
}

/** Смена экрана: уходящему экрану пишем время на нём, новому — откуда пришли. */
export function screenView(next: string) {
  recordActiveTime()
  if (screen) event('screen_leave', { ms: Date.now() - enteredAt })
  prev = screen || null; screen = next; enteredAt = Date.now()
  event('screen_view')
}
/** Нажатие ключевой кнопки: id кнопки, не текст. */
export const tap = (id: string, props?: Props) => event('tap', { id, ...props })

export async function flushEvents() {
  if (!BASE || !playerAvailable || !q.length) return
  const session = await getSessionId().catch(() => undefined)
  if (!session || !playerAvailable || !q.length) return
  const body: AnalyticsBatchRequest = { session, platform, launchSurface: inTelegram ? 'telegram-mini-app' : window.matchMedia('(display-mode: standalone)').matches ? 'standalone' : 'browser', experiments, events: q.splice(0) }
  sendAnalytics(body).catch(() => {})
}

export function installAnalytics() {
  if (installed) return
  installed = true
  event('session_start', { platform })
  const interact = () => activeTime.interact(performance.now())
  for (const name of ['pointerdown', 'keydown', 'scroll']) document.addEventListener(name, interact, { passive: true, capture: true })
  setInterval(recordActiveTime, 15_000)
  setInterval(() => { flushEvents(); void flushEntryEvents(); if (playerAvailable) void linkEntryProfile() }, 10_000)
  window.addEventListener('pagehide', () => { recordActiveTime(); activeTime.visibility(performance.now(), false); flushEvents(); void flushEntryEvents() })
  document.addEventListener('visibilitychange', () => {
    recordActiveTime()
    const visible = document.visibilityState === 'visible'
    activeTime.visibility(performance.now(), visible)
    if (!visible) { event('app_hidden', { ms_on_screen: Date.now() - enteredAt }); flushEvents() }
    else { enteredAt = Date.now(); event('app_visible') }
  })
}

// Anonymous entry events use a narrow endpoint; authenticated game events keep /events.
export function trackLanding(name: 'landing_view' | 'landing_continue' | 'landing_auth_complete' | 'landing_first_post') {
  const props = landingContext()
  if (!props || isLandingPreview()) return
  if (name !== 'landing_view') tap(name, { kind: props.landing_variant })
  if (name === 'landing_first_post') void linkEntryProfile()
  else recordEntryStep(name)
  if (import.meta.env.PROD) {
    const ym = (window as Window & { ym?: (id: number, method: string, goal: string, params: Record<string, string>) => void }).ym
    try { ym?.(114170439, 'reachGoal', name, props) } catch { /* Analytics never blocks entry or publishing. */ }
  }
}
