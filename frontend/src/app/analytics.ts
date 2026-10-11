// Продуктовая аналитика: кто куда ходит и что делает. Отдельно от журнала ошибок (app/log.ts).
// Таксономия, воронки и вопросы: docs/analytics.md. Приватность: только обезличенный id, без текстов, ников, initData.
import { apiBaseUrl as BASE } from './runtimeConfig'
import { ActiveTime } from './activeTime'
import { telegramAuthHeaders } from '../api/auth'
type Props = Record<string, string | number | boolean | null>
export interface AEvent { name: string; t: number; screen: string; prev: string | null; props?: Props }

const activeTime = new ActiveTime(performance.now(), document.visibilityState === 'visible')
let installed = false
function recordActiveTime() {
  const ms = activeTime.take(performance.now())
  if (ms > 0) event('active_time', { ms })
}

const q: AEvent[] = []
let screen = '', prev: string | null = null, enteredAt = Date.now()
const session = crypto.randomUUID?.() ?? Math.random().toString(36).slice(2)
const platform = (() => { try { return new URLSearchParams(location.hash.slice(1)).get('tgWebAppPlatform') ?? 'web' } catch { return 'web' } })()
/** Флаги экспериментов (A/B), приходят с бэкенда в /me. Пример: { plus_offer: '10' | '20' }. */
let experiments: Record<string, string> = {}
export const setExperiments = (e: Record<string, string>) => { experiments = e }

const clean = (p?: Props) => p && Object.fromEntries(Object.entries(p).map(([k, v]) => [k, typeof v === 'string' && v.length > 64 ? `len:${v.length}` : v]))

export function event(name: string, props?: Props) {
  q.push({ name, t: Date.now(), screen, prev, props: clean(props) })
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

export function flushEvents() {
  if (!BASE || !q.length) return
  const body = JSON.stringify({ session, platform, experiments, events: q.splice(0) })
  fetch(`${BASE}/events`, { method: 'POST', headers: { 'Content-Type': 'application/json', ...telegramAuthHeaders() }, body, keepalive: true }).catch(() => {})
}

export function installAnalytics() {
  if (installed) return
  installed = true
  event('session_start', { platform })
  const interact = () => activeTime.interact(performance.now())
  for (const name of ['pointerdown', 'keydown', 'scroll']) document.addEventListener(name, interact, { passive: true, capture: true })
  setInterval(recordActiveTime, 15_000)
  setInterval(() => flushEvents(), 10_000)
  window.addEventListener('pagehide', () => { recordActiveTime(); activeTime.visibility(performance.now(), false); flushEvents() })
  document.addEventListener('visibilitychange', () => {
    recordActiveTime()
    const visible = document.visibilityState === 'visible'
    activeTime.visibility(performance.now(), visible)
    if (!visible) { event('app_hidden', { ms_on_screen: Date.now() - enteredAt }); flushEvents() }
    else { enteredAt = Date.now(); event('app_visible') }
  })
}
