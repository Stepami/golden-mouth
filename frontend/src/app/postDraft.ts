import { clearWorldDrafts } from './worldDraft'
export interface PostDraft { text: string; ownWords: boolean; wantId?: string }
const memory = new Map<string, PostDraft>()
const prefix = 'reki:post-draft:v2:'
export const draftKey = (player: { id: string; worldInstanceId?: string; world: string }) => `${prefix}${player.id}:${player.worldInstanceId ?? player.world}`
export function readPostDraft(key: string): PostDraft {
  if (memory.has(key)) return { ...memory.get(key)! }
  try {
    const value = JSON.parse(localStorage.getItem(key) ?? 'null') as PostDraft | null
    if (value && typeof value.text === 'string' && typeof value.ownWords === 'boolean') { memory.set(key, value); return { ...value } }
  } catch { /* memory fallback */ }
  return { text: '', ownWords: true }
}
export function savePostDraft(key: string, draft: PostDraft) {
  memory.set(key, { ...draft })
  try { localStorage.setItem(key, JSON.stringify(draft)); return true } catch { return false }
}
export function completePostDraft(key: string, text: string) {
  const current = readPostDraft(key)
  savePostDraft(key, current.text === text ? { text: '', ownWords: true } : { text: current.text, ownWords: current.ownWords, wantId: current.wantId })
}
export function clearPlayerDrafts(id: string) {
  clearWorldDrafts(id)
  for (const key of memory.keys()) if (key.startsWith(`${prefix}${id}:`)) memory.delete(key)
  try { for (const key of Object.keys(localStorage)) if (key.startsWith(`${prefix}${id}:`) || key.startsWith(`reki:reply-draft:${id}:`)) localStorage.removeItem(key)
    sessionStorage.removeItem(`reki:world-setup:v2:${id}`)
  } catch { /* memory fallback */ }
}
