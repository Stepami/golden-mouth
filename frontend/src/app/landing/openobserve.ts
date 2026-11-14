import { apiBaseUrl } from '../runtimeConfig'
import { TelemetryClient } from '../../api/generated/reki-api'
import type { AnalyticsBatchRequest, AnalyticsEventRequest } from '../../api/generated/reki-api'
import { transport } from '../../api/transport'
import { isLandingPreview, landingContext, landingVisitIdentity } from './experiment'

export type EntryStep = 'landing_view' | 'landing_continue' | 'landing_auth_complete'
  | 'landing_world_selected' | 'landing_character_selected' | 'landing_reaction_viewed'
type EntryEvent = AnalyticsEventRequest & { t: number; props: Record<string, string | boolean> }
const base = new URL(apiBaseUrl || '/', window.location.origin).href.replace(/\/$/, '')
const anonymous = new TelemetryClient(base, {
  fetch: (url, init) => fetch(url, { ...init, keepalive: true, credentials: 'omit' }),
})
const authenticated = new TelemetryClient(base, transport(true))
const queueKey = 'reki:landing:openobserve-pending:v2'
let pending: EntryEvent[] = []
try {
  const stored: unknown = JSON.parse(localStorage.getItem(queueKey) ?? '[]')
  if (Array.isArray(stored)) pending = stored.filter(e => e && typeof e.props?.event_id === 'string' && typeof e.t === 'number').slice(-20)
} catch { /* Delivery stays usable without storage. */ }
let flushing: Promise<void> | undefined
let linking = false, lastLinked: string | undefined, linkGeneration = 0
const enabled = () => !!apiBaseUrl && import.meta.env.PROD && !isLandingPreview()
const persist = () => { try { localStorage.setItem(queueKey, JSON.stringify(pending)) } catch { /* In-memory retry remains available. */ } }
function visit(): Record<string, string | boolean> | undefined {
  const context = landingContext(), identity = landingVisitIdentity()
  if (!context || !identity) return
  const props: Record<string, string | boolean> = {
    visitor_id: identity.visitorId, entry_id: identity.entryId,
    landing_variant: context.landing_variant, landing_initial_variant: context.landing_initial_variant,
    landing_source: context.landing_source, landing_switched: context.landing_switched === 'true',
  }
  for (const key of ['utm_campaign', 'utm_medium', 'utm_source', 'utm_content'] as const) {
    if (context[key]) props[`landing_${key}`] = context[key]
  }
  return props
}
const batch = (events: EntryEvent[]): AnalyticsBatchRequest => ({
  session: String(events[0].props.visitor_id), platform: 'web', launchSurface: 'browser', experiments: {}, events,
})
export function recordEntryStep(name: EntryStep, postId?: string) {
  if (!enabled()) return
  const context = visit()
  if (!context) return
  pending.push({ name, screen: 'landing', t: Date.now(), props: { ...context, event_id: crypto.randomUUID(), ...(postId ? { entry_post_id: postId } : {}) } })
  pending = pending.filter(e => e.t >= Date.now() - 86400000).slice(-20)
  persist()
  void flushEntryEvents()
}
export function flushEntryEvents(): Promise<void> {
  if (!enabled()) return Promise.resolve()
  if (flushing) return flushing
  flushing = (async () => {
    pending = pending.filter(e => e.t >= Date.now() - 86400000)
    while (pending.length) {
      const events = pending.slice(0, 5), ids = new Set(events.map(e => e.props.event_id))
      try { await anonymous.submitAnalytics(batch(events)) }
      catch (cause) {
        if (!(typeof cause === 'object' && cause && 'status' in cause && cause.status === 400)) return
      }
      pending = pending.filter(e => !ids.has(e.props.event_id))
      persist()
    }
  })().catch(() => {}).finally(() => { flushing = undefined })
  return flushing
}
export function resetEntryProfileLink() { lastLinked = undefined; linkGeneration++ }
export async function linkEntryProfile() {
  if (!enabled()) return
  const context = visit()
  if (!context || context.entry_id === lastLinked || linking) return
  const generation = linkGeneration
  linking = true
  try {
    await authenticated.submitAnalytics(batch([{ name: 'landing_profile_link', screen: 'landing', t: Date.now(), props: { ...context, event_id: crypto.randomUUID() } }]))
    if (generation === linkGeneration) lastLinked = String(context.entry_id)
  } catch { /* Retry without blocking entry or publishing. */ }
  finally { linking = false }
}
