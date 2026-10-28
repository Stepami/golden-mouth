import type { Player } from '../api/types'
import { draftKey, readPostDraft } from './postDraft'
export function resumeTarget(player: Player) {
  const post = readPostDraft(draftKey(player))
  if (post.publicationId) return { title: 'Проверить публикацию', detail: 'Текст сохранён · повторно не отправляем', screen: '02-2_live', params: { text: post.submittedText ?? post.text, publicationId: post.publicationId, ownWords: post.submittedOwnWords, wantId: post.submittedWantId, recoveryOnly: true } }
  const replies: { postId: string; submitted: boolean; at: number }[] = []
  const prefix = `reki:reply-draft:${player.id}:${player.worldInstanceId ?? player.world}:`
  try {
    for (const key of Object.keys(localStorage)) if (key.startsWith(prefix)) {
      try {
        const value = JSON.parse(localStorage.getItem(key) ?? 'null')
        if (typeof value?.text === 'string' && value.text.trim() && typeof value.requestId === 'string' && (value.commentId === null || typeof value.commentId === 'string')) replies.push({ postId: key.slice(prefix.length), submitted: value.submitted === true, at: Number(value.at) || 0 })
      } catch { /* One damaged draft must not hide other valid drafts. */ }
    }
  } catch { /* Optional device storage; no fabricated pending operation. */ }
  const reply = replies.sort((a,b) => b.at - a.at)[0]
  if (reply && (!post.text.trim() || reply.at > (post.at ?? 0))) return { title: reply.submitted ? 'Проверить ответ' : 'Продолжить ответ', detail: 'Черновик сохранён · адресат не изменён', screen: '02-6_thread', params: { postId: reply.postId } }
  if (post.text.trim()) return { title: 'Продолжить публикацию', detail: 'Черновик сохранён', screen: '02-1_compose', params: {} }
  if (reply) return { title: reply.submitted ? 'Проверить ответ' : 'Продолжить ответ', detail: 'Черновик сохранён · адресат не изменён', screen: '02-6_thread', params: { postId: reply.postId } }
}
