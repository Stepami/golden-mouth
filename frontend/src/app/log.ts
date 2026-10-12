// Журнал ошибок фронта (POST /logs). Аналитика поведения отдельно: app/analytics.ts. Правила:
// - никаких текстов игрока, имён, ников и initData: только коды экранов, id, числа, длины;
// - события копятся и уходят пачкой (POST /logs, fetch keepalive при закрытии), в mock mode пишутся в консоль в dev;
// - ошибки ловятся глобально и в ErrorBoundary; повтор одной ошибки не чаще раза в минуту.
import { event, canSendTelemetry } from './analytics'
import { apiBaseUrl as BASE } from './runtimeConfig'
import { telegramAuthHeaders } from '../api/auth'
const DEV = import.meta.env.DEV

export type Level = 'info' | 'warn' | 'error'
export interface LogEntry { t: number; level: Level; event: string; screen?: string; props?: Record<string, string | number | boolean | null> }

const queue: LogEntry[] = []
const seen = new Map<string, number>()
let screen = ''
let session = Math.random().toString(36).slice(2, 10)

export const setScreen = (s: string) => { screen = s }
export const sessionId = () => session

/** Убираем всё похожее на текст: строки длиннее 64 символов режем до длины. */
function clean(props?: LogEntry['props']): LogEntry['props'] {
  if (!props) return undefined
  const out: NonNullable<LogEntry['props']> = {}
  for (const [k, v] of Object.entries(props)) out[k] = typeof v === 'string' && v.length > 64 ? `len:${v.length}` : v
  return out
}

function push(level: Level, event: string, props?: LogEntry['props']) {
  const e: LogEntry = { t: Date.now(), level, event, screen, props: clean(props) }
  if (DEV || !BASE) (level === 'error' ? console.error : console.debug)('[log]', e)
  if (!BASE) return
  if (queue.length >= 100) queue.shift()
  queue.push(e)
  if (queue.length >= 20 || level === 'error') flush()
}

export function flush() {
  if (!BASE || !canSendTelemetry() || !queue.length) return
  const body = JSON.stringify({ session, entries: queue.splice(0) })
  fetch(`${BASE}/logs`, { method: 'POST', headers: { 'Content-Type': 'application/json', ...telegramAuthHeaders() }, body, keepalive: true }).catch(() => {})
}

/** Событие продуктовой аналитики: уходит в app/analytics.ts (POST /events), не в журнал ошибок. */
export const track = (name: string, props?: LogEntry['props']) => event(name, props ?? undefined)
export const warn = (event: string, props?: LogEntry['props']) => push('warn', event, props)
export function error(event: string, err: unknown, props?: LogEntry['props']) {
  const msg = err instanceof Error ? `${err.name}: ${err.message}`.slice(0, 200) : String(err).slice(0, 200)
  const key = `${event}|${msg}`; const now = Date.now()
  if ((seen.get(key) ?? 0) > now - 60_000) return
  seen.set(key, now)
  push('error', event, { ...props, msg, stack: err instanceof Error ? (err.stack ?? '').split('\n').slice(0, 3).join(' | ').slice(0, 300) : null })
}

export function installGlobalHandlers() {
  window.addEventListener('error', e => error('js_error', e.error ?? e.message))
  window.addEventListener('unhandledrejection', e => error('promise_rejection', e.reason))
  setInterval(() => flush(), 15_000)
  document.addEventListener('visibilitychange', () => { if (document.visibilityState === 'hidden') flush() })
}
