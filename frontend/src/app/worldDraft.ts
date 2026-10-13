import { useSyncExternalStore } from 'react'
import type { Address, WorldDefinition } from '../api/types'
import type { AgeBand } from './ageBand'
export interface WorldDraft {
  definition: WorldDefinition; persona?: { star: WorldDefinition['stars'][number]; description: string; name: string; avatar: string; role: string }; editingId?: string; reason?: string; address: Address; group: '14-17' | '18+'; ageBand?: AgeBand
}
const key = 'reki:world-setup:v1'
let current: WorldDraft | null = null
let owner = ''
const listeners = new Set<() => void>()
export function selectDraftOwner(id: string) {
  if (owner === id) return
  owner = id; current = null
  try { current = JSON.parse(sessionStorage.getItem(`${key}:${owner}`) ?? 'null') as WorldDraft | null } catch { /* memory fallback */ }
}
export function setWorldDraft(value: WorldDraft | null) {
  current = value
  try { if (value) sessionStorage.setItem(`${key}:${owner}`, JSON.stringify(value)); else sessionStorage.removeItem(`${key}:${owner}`) } catch { /* memory fallback */ }
  listeners.forEach(listener => listener())
}
export function getWorldDraft() { return current }
export function updateWorldDraft(patch: Partial<WorldDefinition>) { if (current) setWorldDraft({ ...current, definition: { ...current.definition, ...patch } }) }
export function useWorldDraft() { return useSyncExternalStore(listener => { listeners.add(listener); return () => { listeners.delete(listener) } }, () => current) }
export function blankWorld(): WorldDefinition {
  return { balanceKey: 'own', title: '', emoji: '✨', description: '', where: '', difficulty: 1, summary: 'Твоя история', avatarStyle: 'lorelei',
    name: '', avatar: '', role: '', voice: '', drama: 'soft', roles: [], voices: [], postIdeas: [], demoPosts: [], firstContext: '', firstPrompt: '',
    rules: [{ text: 'Без тем 18+ и травли по внешности', locked: true }], stars: [], wants: [], achievements: [] }
}
