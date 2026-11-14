import { apiBaseUrl } from '../runtimeConfig'
import { EntryTelemetryClient } from '../../api/generated/reki-api'
import type { LandingEntryEventRequest, LandingVisitRequest } from '../../api/generated/reki-api'
import { transport } from '../../api/transport'
import { isLandingPreview, landingContext, landingVisitIdentity } from './experiment'

export type EntryStep = 'landing_view' | 'landing_continue' | 'landing_auth_complete'
  | 'landing_world_selected' | 'landing_character_selected' | 'landing_reaction_viewed'
const base = new URL(apiBaseUrl || '/', window.location.origin).href.replace(/\/$/, '')
const anonymous = new EntryTelemetryClient(base, {
  fetch: (url, init) => fetch(url, { ...init, keepalive: true, credentials: 'omit' }),
})
const authenticated = new EntryTelemetryClient(base, transport(true))
const queueKey = 'reki:landing:openobserve-pending:v1'
let pending: LandingEntryEventRequest[] = []
try {
  const stored: unknown = JSON.parse(localStorage.getItem(queueKey) ?? '[]')
  if (Array.isArray(stored)) pending = stored.filter(e => e && typeof e.eventId === 'string' && typeof e.timestamp === 'number' && e.visit).slice(-20)
} catch { /* Delivery stays usable without storage. */ }
let flushing: Promise<void> | undefined
let linking = false, lastLinked: string | undefined, linkGeneration = 0
const enabled = () => !!apiBaseUrl && import.meta.env.PROD && !isLandingPreview()
const persist = () => { try { localStorage.setItem(queueKey, JSON.stringify(pending)) } catch { /* In-memory retry remains available. */ } }
function visit(): LandingVisitRequest | undefined {
  const context = landingContext(), identity = landingVisitIdentity()
  if (!context || !identity) return
  return {
    ...identity, variant: context.landing_variant, initialVariant: context.landing_initial_variant,
    source: context.landing_source, switched: context.landing_switched === 'true',
    campaign: context.utm_campaign, medium: context.utm_medium,
    trafficSource: context.utm_source, content: context.utm_content,
  }
}
export function recordEntryStep(name: EntryStep, postId?: string) {
  if (!enabled()) return
  const context = visit()
  if (!context) return
  pending.push({ eventId: crypto.randomUUID(), name, timestamp: Date.now(), visit: context, ...(postId ? { postId } : {}) })
  pending = pending.filter(e => e.timestamp >= Date.now() - 86400000).slice(-20)
  persist()
  void flushEntryEvents()
}
export function flushEntryEvents(): Promise<void> {
  if (!enabled()) return Promise.resolve()
  if (flushing) return flushing
  flushing = (async () => {
    pending = pending.filter(e => e.timestamp >= Date.now() - 86400000)
    while (pending.length) {
      const batch = pending.slice(0, 5), ids = new Set(batch.map(e => e.eventId))
      try { await anonymous.submitLandingEntries({ events: batch }) }
      catch (cause) {
        if (!(typeof cause === 'object' && cause && 'status' in cause && cause.status === 400)) return
      }
      pending = pending.filter(e => !ids.has(e.eventId))
      persist()
    }
  })().catch(() => {}).finally(() => { flushing = undefined })
  return flushing
}
export function resetEntryProfileLink() { lastLinked = undefined; linkGeneration++ }
export async function linkEntryProfile() {
  if (!enabled()) return
  const context = visit()
  if (!context || context.entryId === lastLinked || linking) return
  const generation = linkGeneration
  linking = true
  try {
    await authenticated.linkLandingProfile(context)
    if (generation === linkGeneration) lastLinked = context.entryId
  } catch { /* Retry without blocking entry or publishing. */ }
  finally { linking = false }
}
