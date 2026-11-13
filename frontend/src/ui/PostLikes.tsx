import { useRef, useState } from 'react'
import { fmt, short } from './index'
import './postLikes.css'

/** Values are confirmed by the API; a personal mark never changes audience likes. */
export function PostLikes({ count, marked, onMark }: {
  count: number
  marked: boolean
  onMark: (marked: boolean) => Promise<void>
}) {
  const [pending, setPending] = useState(false)
  const [failed, setFailed] = useState(false)
  const busy = useRef(false)
  const toggle = async () => {
    if (busy.current) return
    busy.current = true; setPending(true); setFailed(false)
    try { await onMark(!marked) }
    catch { setFailed(true) }
    finally { busy.current = false; setPending(false) }
  }
  return <div className="post-likes" onClick={event => event.stopPropagation()}>
    <div className="post-likes-row">
      <button type="button" className="post-like-toggle" aria-pressed={marked}
        aria-label={marked ? 'Убрать личную отметку' : 'Отметить пост для себя'}
        title={marked ? 'Убрать личную отметку' : 'Отметить пост для себя'}
        disabled={pending} aria-busy={pending} onClick={() => void toggle()}>
        <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1.1-1.1a5.5 5.5 0 0 0-7.8 7.8L12 21l8.8-8.6a5.5 5.5 0 0 0 0-7.8Z" /></svg>
      </button>
      <span className="post-likes-count" aria-label={`Лайки аудитории: ${fmt(count)}`} title={`Лайки аудитории: ${fmt(count)}`}>
        <strong>{short(count)}</strong><span>Лайки аудитории</span>
      </span>
    </div>
    {failed && <p className="post-like-error" role="alert">Не удалось сохранить отметку. Нажми на сердце ещё раз.</p>}
  </div>
}
