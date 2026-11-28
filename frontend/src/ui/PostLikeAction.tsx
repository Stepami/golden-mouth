import { useEffect, useRef } from 'react'
import { api } from '../api/client'
import type { PostDetailResponse } from '../api/types'
import { useStore } from '../app/store'
import { track } from '../app/log'
import { PostLikes } from './PostLikes'

export function PostLikeAction({ postId, likes, likedBy, onSaved }: {
  postId: string; likes: number; likedBy: string[]
  onSaved: (post: PostDetailResponse) => void
}) {
  const { player, refresh } = useStore()
  const profile = player!
  const author = `Player_${profile.id.replaceAll('-', '').toLowerCase()}`
  const scope = `${profile.id}:${profile.worldInstanceId}:${postId}`
  const current = useRef(scope); current.current = scope
  useEffect(() => { current.current = scope; return () => { current.current = '' } }, [scope])
  const confirmed = (saved: PostDetailResponse) => {
    if (current.current !== scope) return
    onSaved(saved)
    const liked = saved.likedBy.includes(author)
    track('tap', { to: 'post_mark', own: liked, id: postId })
    if (liked && profile.dailyChallenge?.completion.likes === false) void refresh()
  }
  return <PostLikes key={scope} count={likes + likedBy.length} marked={likedBy.includes(author)} onMark={async desired => {
    try {
      const saved = await api.togglePostLike(postId)
      confirmed(saved)
    } catch (cause) {
      if (current.current !== scope) throw cause
      // A toggle cannot be replayed after a lost response: reconcile with a read.
      const saved = await api.post(postId).catch(() => null)
      if (current.current === scope && saved) {
        const { id, ...detail } = saved
        confirmed({ ...detail, postId: id })
        if (saved.likedBy.includes(author) === desired) return
      }
      throw cause
    }
  }} />
}
