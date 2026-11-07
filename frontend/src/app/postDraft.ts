import { clearWorldDrafts } from './worldDraft'
export interface PostDraft { text: string; publicationId?: string; submittedText?: string; ownWords: boolean; submittedOwnWords?: boolean; wantId?: string; submittedWantId?: string }
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
export function completePostDraft(key: string, publicationId: string, text: string) {
  const current = readPostDraft(key)
  if (current.publicationId !== publicationId) return
  savePostDraft(key, current.text === text ? { text: '', ownWords: true } : { text: current.text, ownWords: current.ownWords, wantId: current.wantId })
}
export function clearPlayerDrafts(id: string) {
  clearWorldDrafts(id)
  for (const key of firstComments.keys()) if (key.startsWith(`reki:first-comment:${id}:`)) firstComments.delete(key)
  for (const key of memory.keys()) if (key.startsWith(`${prefix}${id}:`)) memory.delete(key)
  try { for (const key of Object.keys(localStorage)) if (key.startsWith(`${prefix}${id}:`) || key.startsWith(`reki:reply-draft:${id}:`) || key.startsWith(`reki:first-comment:${id}:`)) localStorage.removeItem(key)
    sessionStorage.removeItem(`reki:world-setup:v2:${id}`)
  } catch { /* memory fallback */ }
}

export interface FirstCommentDraft { requestId: string; authorId: string; text?: string }
const firstComments = new Map<string, FirstCommentDraft>()
const firstKey = (playerId: string, postId: string) => `reki:first-comment:${playerId}:${postId}`
export function readFirstComment(playerId: string, postId: string): FirstCommentDraft | undefined {
  const key = firstKey(playerId, postId)
  if (firstComments.has(key)) return firstComments.get(key)
  try { const saved = JSON.parse(localStorage.getItem(key) ?? 'null') as FirstCommentDraft | null; if (saved) { firstComments.set(key, saved); return saved } } catch { /* memory fallback */ }
}
export function saveFirstComment(playerId: string, postId: string, draft: FirstCommentDraft) {
  const key = firstKey(playerId, postId); firstComments.set(key, draft)
  try { localStorage.setItem(key, JSON.stringify(draft)) } catch { /* memory fallback */ }
}
export function clearFirstComment(playerId: string, postId: string) {
  const key = firstKey(playerId, postId); firstComments.delete(key)
  try { localStorage.removeItem(key) } catch { /* memory fallback */ }
}
