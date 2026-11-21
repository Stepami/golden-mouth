import { sessionGeneration } from '../api/session'
import { isLandingPreview, landingContext, landingVisitIdentity } from './landing/experiment'

type MetrikaParams = Record<string, string | undefined>
type SavedActivity = 'world' | 'post' | 'comment'

const activityGoals = new Set(['dau', 'invited_friend_dau', 'post_1_daily', 'post_3_daily'])
const storageKey = 'reki:metrika-completed-actions'
const pending = new Set<string>()
const completed = new Set<string>()

function attribution(): MetrikaParams {
  const entry = landingVisitIdentity()
  return { ...landingContext(), ...(entry ? { entry_id: entry.entryId } : {}) }
}

function sendGoal(name: string, params: MetrikaParams) {
  const ym = (window as Window & { ym?: (id: number, method: string, goal: string, params: MetrikaParams) => void }).ym
  ym?.(114170439, 'reachGoal', name, params)
}

export function reachMetrikaGoal(name: string, params?: MetrikaParams) {
  if (!import.meta.env.PROD || isLandingPreview()) return
  try {
    sendGoal(name, { ...attribution(), ...params })
  } catch { /* Analytics never blocks entry or publishing. */ }
}

function rememberedActions(): string[] {
  try {
    const data: unknown = JSON.parse(localStorage.getItem(storageKey) ?? '[]')
    return Array.isArray(data) ? data.filter((key): key is string => typeof key === 'string') : []
  } catch {
    return []
  }
}

function deliver(key: string, goals: string[], params: MetrikaParams, session: number) {
  if (session !== sessionGeneration() || completed.has(key)) return
  const remembered = rememberedActions()
  if (remembered.includes(key)) return
  completed.add(key)
  if (completed.size > 256) {
    const oldest = completed.values().next().value
    if (oldest) completed.delete(oldest)
  }
  try { localStorage.setItem(storageKey, JSON.stringify([...remembered.slice(-255), key])) } catch { /* In-memory deduplication still applies. */ }
  for (const name of new Set(goals)) sendGoal(name, params)
}

async function collect(kind: SavedActivity, id: string, key: string, session: number) {
  const params = attribution()
  for (const delay of [0, 1000, 2500, 5000, 10000]) {
    if (delay) await new Promise(resolve => setTimeout(resolve, delay))
    if (session !== sessionGeneration()) return
    const response = await fetch('/analytics/goals', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'omit',
      body: JSON.stringify({ kind, id }),
      signal: AbortSignal.timeout(5000),
    })
    if (response.status === 202) continue
    if (response.status !== 200) return
    const goals: unknown = await response.json()
    if (!Array.isArray(goals) || !goals.every(name => typeof name === 'string' && activityGoals.has(name))) return
    if (navigator.locks) {
      await navigator.locks.request(storageKey, () => deliver(key, goals, params, session))
    } else {
      deliver(key, goals, params, session)
    }
    return
  }
}

export function trackMetrikaActivity(kind: SavedActivity, id: string) {
  if (!import.meta.env.PROD || isLandingPreview()) return
  const compact = id.replaceAll('-', '').toLowerCase()
  const key = `${kind}:${compact}`
  if (pending.has(key) || completed.has(key) || rememberedActions().includes(key)) return
  pending.add(key)
  void collect(kind, compact, key, sessionGeneration()).catch(() => {}).finally(() => pending.delete(key))
}
