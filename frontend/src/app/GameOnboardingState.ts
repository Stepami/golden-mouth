import { event } from './analytics'
import { useSyncExternalStore } from 'react'
import { api } from '../api/client'
import { needsRegistration } from '../api/auth'

type Phase = 'setup' | 'post' | 'reaction' | 'reply' | 'done'
type NextStory = { phase: 'offered' | 'dismissed' } | { phase: 'post' | 'done'; worldId: string; templateId: string }
export type GameOnboardingState = { phase: Phase; worldId?: string; postId?: string; setupScreen?: string; setupWorld?: string; conversationPostId?: string; replyRequestId?: string; completionSeen?: boolean; bonusClaim?: 'pending' | 'confirmed' | 'uncertain'; crystalShopIntroPending?: boolean; nextStory?: NextStory }
export const isGameOnboardingPreview = () => import.meta.env.DEV && new URLSearchParams(location.search).get('preview') === 'game-onboarding'
export const isNextStoryPreview = () => import.meta.env.DEV && new URLSearchParams(location.search).get('preview') === 'next-story'
const prefix = 'reki:game-onboarding:v1:'
const storageId = (id: string) => isGameOnboardingPreview() ? `preview:${id}` : id
const memory = new Map<string, GameOnboardingState>()
const listeners = new Set<() => void>()
const owner = () => api.playerId() ?? (needsRegistration() ? 'registration' : undefined)
function read(id: string): GameOnboardingState | undefined {
  if (memory.has(storageId(id))) return memory.get(storageId(id))
  try {
    const value = JSON.parse(localStorage.getItem(prefix + storageId(id)) ?? 'null') as GameOnboardingState | null
    if (value && ['setup', 'post', 'reaction', 'reply', 'done', 'skipped'].includes(value.phase)) memory.set(storageId(id), (value.phase as string) === 'skipped' ? { ...value, phase: value.postId ? 'reaction' : value.worldId ? 'post' : 'setup' } : value)
  } catch { /* Session fallback when storage is unavailable. */ }
  return memory.get(storageId(id))
}
function write(id: string, value: GameOnboardingState) {
  memory.set(storageId(id), value)
  let durable = true
  try { localStorage.setItem(prefix + storageId(id), JSON.stringify(value)) } catch { durable = false }
  listeners.forEach(listener => listener())
  return durable
}
export function restartGameOnboardingPreview(worldId?: string) {
  const id = owner()
  if (isGameOnboardingPreview() && id) write(id, worldId ? { phase: 'post', worldId } : { phase: 'setup' })
}
export function beginGameOnboardingPreview(worldId?: string) {
  const id = owner()
  if (isGameOnboardingPreview() && id && !read(id)) write(id, worldId ? { phase: 'post', worldId } : { phase: 'setup' })
}
export function beginGameOnboarding() {
  if (needsRegistration() && !read('registration')) write('registration', { phase: 'setup' })
}
// Enrollment is transferred only after this session's registration succeeds.
export function registerGameOnboarding(id: string) {
  const pending = read('registration')
  if (pending && !read(id)) write(id, pending)
  memory.delete(storageId('registration'))
  try { localStorage.removeItem(prefix + storageId('registration')) } catch { /* Optional storage. */ }
}
export function gameOnboardingState() { const id = owner(); return id ? read(id) : undefined }
const subscribe = (listener: () => void) => { listeners.add(listener); return () => { listeners.delete(listener) } }
export function useGameOnboarding() {
  return useSyncExternalStore(subscribe, gameOnboardingState)
}
export function updateGameOnboarding(change: Partial<GameOnboardingState>) {
  const id = owner(), current = gameOnboardingState()
  if (!id || !current || current.phase === 'done') return
  write(id, { ...current, ...change })
  if (change.phase && change.phase !== current.phase && !isGameOnboardingPreview()) {
    const step = { setup: 'setup', post: 'world_saved', reaction: 'post_saved', reply: 'reply_started', done: 'reply_saved' }[change.phase]
    event('onboarding_step', { step, ok: true })
  }
}
export function onboardingWorldSaved(worldId: string) {
  if (gameOnboardingState()?.phase === 'setup') updateGameOnboarding({ phase: 'post', worldId })
}
export function onboardingPostSaved(worldId: string, postId: string) {
  const state = gameOnboardingState()
  if (state?.phase === 'post' && state.worldId === worldId) updateGameOnboarding({ phase: 'reaction', postId, conversationPostId: postId })
}
export function onboardingReplyStarted(postId: string, requestId: string) {
  const state = gameOnboardingState()
  if (state?.postId && (state.conversationPostId ?? state.postId) === postId && (state.phase === 'reaction' || state.phase === 'reply')) updateGameOnboarding({ phase: 'reply', replyRequestId: requestId })
}
export function onboardingReplySaved(postId: string, requestId: string) {
  const state = gameOnboardingState()
  if ((state?.conversationPostId ?? state?.postId) === postId && state?.replyRequestId === requestId && state.phase === 'reply') updateGameOnboarding({ phase: 'done' })
}
export function acknowledgeGameOnboarding() {
  const id = owner(), state = gameOnboardingState()
  if (id && state?.phase === 'done') write(id, { ...state, completionSeen: true, crystalShopIntroPending: true })
}
export function acknowledgeCrystalShopOnboarding() {
  const id = owner(), state = gameOnboardingState()
  if (id && state?.crystalShopIntroPending) write(id, { ...state, crystalShopIntroPending: false })
}
export function gameOnboardingActive(state: GameOnboardingState | undefined) { return !!state && state.phase !== 'done' }

// This device's UX guard is not server-side idempotency.
export function setOnboardingBonusClaim(status: GameOnboardingState['bonusClaim'], playerId: string) {
  if (isGameOnboardingPreview()) return false
  const state = read(playerId)
  return state?.phase === 'done' ? write(playerId, { ...state, bonusClaim: status }) : false
}

export function completeGuestOnboarding(playerId: string, worldId: string, postId: string, bonusClaim: GameOnboardingState['bonusClaim']) {
  if (api.playerId() !== playerId) return false
  const durable = write(playerId, { phase: 'done', worldId, postId, conversationPostId: postId, completionSeen: true, crystalShopIntroPending: true, bonusClaim })
  memory.delete(storageId('registration'))
  try { localStorage.removeItem(prefix + storageId('registration')) } catch { /* Optional cleanup. */ }
  return durable
}

function nextStoryOwner() {
  const id = api.playerId()
  return id && (isNextStoryPreview() ? `next-story-preview:${id}` : id)
}

function nextStoryState() {
  const id = nextStoryOwner()
  return id ? read(id)?.nextStory : undefined
}

export function useNextStory() { return useSyncExternalStore(subscribe, nextStoryState) }

export function trackNextStory(step: string, templateId: string, props?: { kind?: string; star?: string }) {
  if (!isNextStoryPreview()) event('onboarding_step', { step: `next_story_${step}`, template_id: templateId, ok: true, ...props })
}

function saveNextStory(nextStory: NextStory) {
  const id = nextStoryOwner()
  if (!id) return false
  const state = read(id)
  if (state?.phase !== 'done' && !isNextStoryPreview()) return false
  write(id, { ...state, phase: 'done', nextStory })
  return true
}

export function nextStoryOfferShown(templateId: string) {
  if (!nextStoryState() && saveNextStory({ phase: 'offered' })) trackNextStory('shown', templateId)
}

export function dismissNextStory(templateId: string) {
  if (saveNextStory({ phase: 'dismissed' })) trackNextStory('dismissed', templateId)
}

export function nextStoryWorldSaved(worldId: string, templateId: string) {
  if (saveNextStory({ phase: 'post', worldId, templateId })) trackNextStory('world_saved', templateId)
}

export function nextStoryPostSaved(worldId: string) {
  const state = nextStoryState()
  if (state?.phase === 'post' && state.worldId === worldId && saveNextStory({ ...state, phase: 'done' })) trackNextStory('post_saved', state.templateId)
}
