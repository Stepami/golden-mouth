import { useSyncExternalStore } from 'react'

const prefix = 'reki:avatar-personalization:v1:'
const memory = new Map<string, boolean>()
const listeners = new Set<() => void>()
const notify = () => listeners.forEach(listener => listener())

export function avatarPersonalization(playerId: string): boolean {
  try { const saved = localStorage.getItem(prefix + playerId); return saved === null ? memory.get(playerId) ?? true : saved !== 'off' }
  catch { return memory.get(playerId) ?? true }
}

export function setAvatarPersonalization(playerId: string, enabled: boolean) {
  memory.set(playerId, enabled)
  try { localStorage.setItem(prefix + playerId, enabled ? 'on' : 'off') } catch { /* session fallback */ }
  notify()
}

export function clearAvatarPersonalization(playerId: string) {
  memory.delete(playerId)
  try { localStorage.removeItem(prefix + playerId) } catch { /* session fallback */ }
  notify()
}

function subscribe(listener: () => void) {
  listeners.add(listener)
  const onStorage = (event: StorageEvent) => { if (event.key === null || event.key.startsWith(prefix)) listener() }
  window.addEventListener('storage', onStorage)
  return () => { listeners.delete(listener); window.removeEventListener('storage', onStorage) }
}

export function useAvatarPersonalization(playerId: string) {
  return useSyncExternalStore(subscribe, () => avatarPersonalization(playerId))
}
