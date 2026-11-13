import { useNav } from '../app/nav'
import type { QuotedPostResponse } from '../api/generated/reki-api'
import { Avatar } from './index'

export function QuotedPost({ quote }: { quote?: QuotedPostResponse | null }) {
  const nav = useNav()
  if (!quote) return null
  return <button type="button" className="quoted-post" disabled={quote.text == null} onClick={event => {
    event.stopPropagation(); nav.push('02-6_thread', { postId: quote.postId })
  }}>
    {quote.text == null ? <span className="muted">Пост недоступен</span> : <>
      <span className="row"><Avatar seed={quote.avatar ?? ''} mode="default" size={28} /><b>{quote.author}</b></span>
      <span className="quoted-post-text">{quote.text}</span>
    </>}
  </button>
}
