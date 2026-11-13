import { useEffect, useRef } from 'react'
import { api } from '../api/client'
import type { PostDetailResponse } from '../api/types'
import { usePlayer } from '../app/store'
import { PostLikes } from './PostLikes'

export function PostLikeAction({ postId, likes, likedBy, onSaved }: {
  postId: string; likes: number; likedBy: string[]
  onSaved: (post: PostDetailResponse) => void
}) {
  const player = usePlayer()
  const author = `Player_${player.id.replaceAll('-', '').toLowerCase()}`
  const scope = `${player.id}:${player.worldInstanceId}:${postId}`
  const current = useRef(scope); current.current = scope
  useEffect(() => { current.current = scope; return () => { current.current = '' } }, [scope])
  return <PostLikes key={scope} count={likes} marked={likedBy.includes(author)} onMark={async desired => {
    try {
      const saved = await api.togglePostLike(postId)
      if (current.current === scope) onSaved(saved)
    } catch (cause) {
      if (current.current !== scope) throw cause
      // A toggle cannot be replayed after a lost response: reconcile with a read.
      const saved = await api.post(postId).catch(() => null)
      if (current.current === scope && saved) {
        const { id, ...detail } = saved
        onSaved({ ...detail, postId: id })
        if (saved.likedBy.includes(author) === desired) return
      }
      throw cause
    }
  }} />
}
