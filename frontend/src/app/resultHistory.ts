import type { Player, Post, PostResult, SkillId } from '../api/types'

export type SavedResult = { seen?: boolean; at: string; post: Pick<Post, 'id' | 'text'>; result: PostResult; skills?: Partial<Record<SkillId, { before: number; after: number }>> }
const memory = new Map<string, SavedResult[]>()
const storageFallback = new Set<string>()
const key = (p: Pick<Player, 'id' | 'world' | 'worldInstanceId'>) => `reki:results:v1:${p.id}:${p.worldInstanceId ?? p.world}`
export function resultHistory(p: Pick<Player, 'id' | 'world' | 'worldInstanceId'>): SavedResult[] {
  const k = key(p)
  if (storageFallback.has(k)) return memory.get(k) ?? []
  try {
    const saved: unknown = JSON.parse(localStorage.getItem(k) ?? 'null')
    if (Array.isArray(saved)) return saved.filter((item): item is SavedResult => item && typeof item.at === 'string' && typeof item.post?.id === 'string' && typeof item.post?.text === 'string' && typeof item.result?.delta === 'number' && Array.isArray(item.result.whys)).slice(0, 100)
  } catch { /* Device storage is optional. */ }
  return memory.get(k) ?? []
}
export function rememberResult(before: Player, post: Post, result: PostResult) {
  const k = key(before)
  const history = resultHistory(before)
  if (history.some(item => item.post.id === post.id)) return
  const next = [{ seen: false, at: new Date().toISOString(), post: { id: post.id, text: post.text }, result }, ...history].slice(0, 100)
  memory.set(k, next)
  try { localStorage.setItem(k, JSON.stringify(next)); storageFallback.delete(k) } catch { storageFallback.add(k) }
  window.dispatchEvent(new Event('reki:results-changed'))
}
export function clearResultHistory() {
  memory.clear(); storageFallback.clear()
  try { Object.keys(localStorage).filter(k => k.startsWith('reki:results:')).forEach(k => localStorage.removeItem(k)) } catch { /* optional storage */ }
}

export function markResultSeen(player: Pick<Player, 'id' | 'world' | 'worldInstanceId'>, postId: string) {
  const k = key(player)
  const history = resultHistory(player)
  if (!history.some(item => item.post.id === postId && item.seen === false)) return
  const next = history.map(item => item.post.id === postId ? { ...item, seen: true } : item)
  memory.set(k, next)
  try { localStorage.setItem(k, JSON.stringify(next)); storageFallback.delete(k) } catch { storageFallback.add(k) }
  window.dispatchEvent(new Event('reki:results-changed'))
}
