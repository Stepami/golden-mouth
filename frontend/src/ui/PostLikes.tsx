import { useRef, useState } from 'react'
import { fmt, short } from './index'
import './postLikes.css'

/** The count and personal mark are confirmed by the API. */
export function PostLikes({ count, marked, onMark }: {
  count: number
  marked: boolean
  onMark: (marked: boolean) => Promise<void>
}) {
  const [pending, setPending] = useState(false)
  const [failed, setFailed] = useState(false)
  const [animate, setAnimate] = useState(false)
  const busy = useRef(false)
  const toggle = async () => {
    if (busy.current) return
    busy.current = true; setPending(true); setFailed(false); setAnimate(false)
    try { await onMark(!marked); setAnimate(!marked) }
    catch { setFailed(true) }
    finally { busy.current = false; setPending(false) }
  }
  return <div className="post-likes" onClick={event => event.stopPropagation()}>
    <button type="button" className={`react post-like-toggle${animate ? ' post-like-animated' : ''}`} aria-pressed={marked}
      aria-label={`${marked ? 'Убрать личную отметку' : 'Отметить пост для себя'}; лайков: ${fmt(count)}`}
      title={`${marked ? 'Убрать личную отметку' : 'Отметить пост для себя'}. Лайков: ${fmt(count)}`}
      disabled={pending} aria-busy={pending} onClick={() => void toggle()}>
      <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1.1-1.1a5.5 5.5 0 0 0-7.8 7.8L12 21l8.8-8.6a5.5 5.5 0 0 0 0-7.8Z" /></svg>
      <span className="post-likes-count" aria-hidden="true">{short(count)}</span>
    </button>
    {failed && <p className="post-like-error" role="alert">Не удалось сохранить отметку. Нажми на сердце ещё раз.</p>}
  </div>
}
