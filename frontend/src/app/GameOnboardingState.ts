import { useSyncExternalStore } from 'react'
import { api } from '../api/client'
import { needsRegistration } from '../api/auth'

type Phase = 'setup' | 'post' | 'reaction' | 'reply' | 'done' | 'skipped'
export type GameOnboardingState = { phase: Phase; worldId?: string; postId?: string; setupScreen?: string; setupWorld?: string }
export const isGameOnboardingPreview = () => import.meta.env.DEV && new URLSearchParams(location.search).get('preview') === 'game-onboarding'
const prefix = 'reki:game-onboarding:v1:'
const storageId = (id: string) => isGameOnboardingPreview() ? `preview:${id}` : id
const memory = new Map<string, GameOnboardingState>()
const listeners = new Set<() => void>()
const owner = () => api.playerId() ?? (needsRegistration() ? 'registration' : undefined)
function read(id: string): GameOnboardingState | undefined {
  if (memory.has(storageId(id))) return memory.get(storageId(id))
  try {
    const value = JSON.parse(localStorage.getItem(prefix + storageId(id)) ?? 'null') as GameOnboardingState | null
    if (value && ['setup', 'post', 'reaction', 'reply', 'done', 'skipped'].includes(value.phase)) memory.set(storageId(id), value)
  } catch { /* Session fallback when storage is unavailable. */ }
  return memory.get(storageId(id))
}
function write(id: string, value: GameOnboardingState) {
  memory.set(storageId(id), value)
  try { localStorage.setItem(prefix + storageId(id), JSON.stringify(value)) } catch { /* Session fallback. */ }
  listeners.forEach(listener => listener())
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
export function useGameOnboarding() {
  return useSyncExternalStore(listener => { listeners.add(listener); return () => { listeners.delete(listener) } }, gameOnboardingState)
}
export function updateGameOnboarding(change: Partial<GameOnboardingState>) {
  const id = owner(), current = gameOnboardingState()
  if (id && current && current.phase !== 'done' && current.phase !== 'skipped') write(id, { ...current, ...change })
}
export function onboardingWorldSaved(worldId: string) {
  if (gameOnboardingState()?.phase === 'setup') updateGameOnboarding({ phase: 'post', worldId })
}
export function onboardingPostSaved(worldId: string, postId: string) {
  const state = gameOnboardingState()
  if (state?.phase === 'post' && state.worldId === worldId) updateGameOnboarding({ phase: 'reaction', postId })
}
export function onboardingReplySaved(postId: string) {
  const state = gameOnboardingState()
  if (state?.postId === postId && (state.phase === 'reaction' || state.phase === 'reply')) updateGameOnboarding({ phase: 'done' })
}
