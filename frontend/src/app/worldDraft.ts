import { useSyncExternalStore } from 'react'
import type { Address, Player, WorldDefinition } from '../api/types'
import type { AgeBand } from './ageBand'
import { avatarPersonalization } from './avatarPersonalization'
export interface WorldDraft {
  definition: WorldDefinition; heroOrder?: string[]; advanced?: boolean; previewOnly?: boolean; persona?: { star: WorldDefinition['stars'][number]; description: string; name: string; avatar: string; role: string }; editingId?: string; reason?: string; address: Address; group: '14-17' | '18+'; ageBand?: AgeBand; personalizeAvatar?: boolean
}
const key = 'reki:world-setup:v1'
let current: WorldDraft | null = null
let owner = ''
const saved = new Map<string, WorldDraft>()
const slot = (world: string) => `${key}:${owner}:world:${world}`
export function savedWorldDraft(world: string): WorldDraft | null {
  if (current?.definition.balanceKey === world && !current.editingId) return current
  try { return JSON.parse(sessionStorage.getItem(slot(world)) ?? 'null') as WorldDraft | null ?? saved.get(slot(world)) ?? null } catch { return saved.get(slot(world)) ?? null }
}
const listeners = new Set<() => void>()
export function selectDraftOwner(id: string) {
  if (owner === id) return
  owner = id; current = null
  try { current = JSON.parse(sessionStorage.getItem(`${key}:${owner}`) ?? 'null') as WorldDraft | null } catch { /* memory fallback */ }
  if (current && current.personalizeAvatar === undefined) current = { ...current, personalizeAvatar: avatarPersonalization(id) }
}
export function setWorldDraft(value: WorldDraft | null) {
  if (current && !current.editingId) {
    const storageKey = slot(current.definition.balanceKey)
    saved.set(storageKey, current)
    try { sessionStorage.setItem(storageKey, JSON.stringify(current)) } catch { /* memory fallback */ }
    if (!value) { saved.delete(storageKey); try { sessionStorage.removeItem(storageKey) } catch { /* memory fallback */ } }
  }
  current = value
  try { if (value) sessionStorage.setItem(`${key}:${owner}`, JSON.stringify(value)); else sessionStorage.removeItem(`${key}:${owner}`) } catch { /* memory fallback */ }
  listeners.forEach(listener => listener())
}
export function clearWorldDrafts(id: string) {
  const prefix = `${key}:${id}`
  for (const storageKey of saved.keys()) if (storageKey === prefix || storageKey.startsWith(prefix + ':')) saved.delete(storageKey)
  try { for (const storageKey of Object.keys(sessionStorage)) if (storageKey === prefix || storageKey.startsWith(prefix + ':')) sessionStorage.removeItem(storageKey) } catch { /* memory fallback */ }
  if (owner === id) { current = null; listeners.forEach(listener => listener()) }
}
export function getWorldDraft() { return current }
export function updateWorldDraft(patch: Partial<WorldDefinition>) { if (current) setWorldDraft({ ...current, definition: { ...current.definition, ...patch } }) }
export function useWorldDraft() { return useSyncExternalStore(listener => { listeners.add(listener); return () => { listeners.delete(listener) } }, () => current) }
export function blankWorld(): WorldDefinition {
  return { balanceKey: 'own', title: '', emoji: '✨', description: '', where: '', difficulty: 1, summary: 'Твоя история', avatarStyle: 'lorelei',
    name: '', avatar: '', role: '', voice: '', drama: 'soft', roles: [], voices: [], postIdeas: [], demoPosts: [], firstContext: '', firstPrompt: '',
    rules: [{ text: 'Без тем 18+ и травли по внешности', locked: true }], stars: [], wants: [], achievements: [] }
}

/** Start the full editor supported by POST /worlds. AI quick setup remains a separate demo. */
export function beginOwnWorld(player: Player) {
  selectDraftOwner(player.id)
  const restored = savedWorldDraft('own')
  if (restored && !restored.previewOnly) { setWorldDraft(restored); return }
  setWorldDraft({ definition: { ...blankWorld(), name: player.name, avatar: player.handle, role: player.role }, advanced: true, previewOnly: false, address: player.address, group: player.ageBand === '14-17' || !player.ageBand ? '14-17' : '18+', ageBand: player.ageBand, personalizeAvatar: avatarPersonalization(player.id) })
}
