import { useEffect, useState } from 'react'
import { api, ApiError } from '../api/client'
import type { Post } from '../api/types'
import { contentOf } from '../api/worldContent'
import { useNav } from '../app/nav'
import { usePlayer } from '../app/store'
import { Avatar } from './index'

export function QuotedPost({ repostId }: { repostId?: string | null }) {
  const nav = useNav(); const player = usePlayer()
  const [source, setSource] = useState<Post | null>(null)
  const [error, setError] = useState<'missing' | 'failed' | null>(null)
  const [attempt, setAttempt] = useState(0)
  useEffect(() => {
    setSource(null); setError(null)
    if (!repostId) return
    let active = true
    void api.post(repostId).then(post => { if (active) { setSource(post); if (!post) setError('missing') } }).catch(cause => {
      if (active) setError(cause instanceof ApiError && cause.status === 404 ? 'missing' : 'failed')
    })
    return () => { active = false }
  }, [repostId, player.id, player.worldInstanceId, attempt])
  if (!repostId) return null
  const author = source && contentOf(player.world).stars.find(star => `Star_${star.id}` === source.authorId)
  const own = source?.authorId === `Player_${player.id.replaceAll('-', '').toLowerCase()}`
  return <div className="quoted-post" onClick={event => event.stopPropagation()}>
    {source ? <button type="button" className="quote-source" onClick={() => nav.push('02-6_thread', { postId: repostId })}>
      <span className="row"><Avatar seed={own ? player.handle : author?.avatarSeed ?? ''} world={player.world} mode={own ? 'profile' : 'default'} size={28} /><b>{own ? player.name : author?.name ?? 'Автор'}</b></span>
      <span className="quoted-post-text">{source.text}</span>
    </button> : <span className="muted" role="status">{error === 'missing' ? 'Пост недоступен' : error === 'failed' ? 'Не удалось загрузить оригинал' : 'Загружаем оригинал…'}</span>}
    {error === 'failed' && <button className="text-button repost-action" onClick={() => setAttempt(value => value + 1)}>Повторить</button>}
  </div>
}
