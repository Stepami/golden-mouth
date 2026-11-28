import { clearWorldDrafts } from './worldDraft'
export interface PostDraft { text: string; ownWords: boolean }
const memory = new Map<string, PostDraft>()
const prefix = 'reki:post-draft:v2:'
export const draftKey = (player: { id: string; worldInstanceId?: string; world: string }) => `${prefix}${player.id}:${player.worldInstanceId ?? player.world}`
export const replyDraftKey = (player: { id: string; worldInstanceId?: string; world: string }, postId?: string) =>
  `reki:reply-draft:${player.id}:${player.worldInstanceId ?? player.world}:${postId ?? 'mock'}`
export type ReplyDraft = { requestId: string; commentId: string | null; text: string; submitted: boolean; additionalStarId?: string }
export function readReplyDraft(key: string): ReplyDraft | undefined {
  try {
    const saved = JSON.parse(localStorage.getItem(key) ?? 'null') as ReplyDraft | null
    if (saved && typeof saved.requestId === 'string' && saved.requestId.length > 0 && saved.requestId.length <= 64 &&
      typeof saved.text === 'string' && saved.text.length <= 1000 && (saved.commentId === null || typeof saved.commentId === 'string')) {
      return { ...saved, submitted: saved.submitted === true, additionalStarId: typeof saved.additionalStarId === 'string' ? saved.additionalStarId : undefined }
    }
  } catch { /* Optional storage. */ }
}
export function saveReplyDraft(key: string, draft: ReplyDraft) {
  try { localStorage.setItem(key, JSON.stringify(draft)); return true } catch { return false }
}
export function readPostDraft(key: string): PostDraft {
  if (memory.has(key)) return { ...memory.get(key)! }
  try {
    const value = JSON.parse(localStorage.getItem(key) ?? 'null') as PostDraft | null
    if (value && typeof value.text === 'string' && typeof value.ownWords === 'boolean') { memory.set(key, value); return { ...value } }
  } catch { /* memory fallback */ }
  return { text: '', ownWords: true }
}
export function hasPostDraft(key: string): boolean {
  readPostDraft(key)
  return memory.has(key)
}
export function savePostDraft(key: string, draft: PostDraft) {
  memory.set(key, { ...draft })
  try { localStorage.setItem(key, JSON.stringify(draft)); return true } catch { return false }
}
export function completePostDraft(key: string, text: string) {
  const current = readPostDraft(key)
  savePostDraft(key, current.text === text ? { text: '', ownWords: true } : { text: current.text, ownWords: current.ownWords })
}
export function clearPlayerDrafts(id: string) {
  clearWorldDrafts(id)
  for (const key of memory.keys()) if (key.startsWith(`${prefix}${id}:`)) memory.delete(key)
  try { for (const key of Object.keys(localStorage)) if (key.startsWith(`${prefix}${id}:`) || key.startsWith(`reki:reply-draft:${id}:`)) localStorage.removeItem(key)
    sessionStorage.removeItem(`reki:world-setup:v2:${id}`)
  } catch { /* memory fallback */ }
}
