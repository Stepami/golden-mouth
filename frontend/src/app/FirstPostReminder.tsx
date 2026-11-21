import { useEffect, useRef, useState } from 'react'
import { api } from '../api/client'
import { useNav } from './nav'
import { usePlayer, useStore } from './store'

export function FirstPostReminder({ worldId, worldTitle }: { worldId: string; worldTitle: string }) {
  const [target] = useState({ worldId, worldTitle })
  const player = usePlayer()
  const { setPlayer } = useStore()
  const nav = useNav()
  const key = `reki:first-post-reminder-dismissed:${player.id}`
  const [dismissed, setDismissed] = useState(() => {
    try { return sessionStorage.getItem(key) === '1' } catch { return false }
  })
  const [opening, setOpening] = useState(false)
  const [error, setError] = useState('')
  const request = useRef<AbortController | null>(null)
  useEffect(() => () => request.current?.abort(), [nav.current])
  const dismiss = () => {
    request.current?.abort()
    setDismissed(true)
    try { sessionStorage.setItem(key, '1') } catch { /* Keep dismissal in memory. */ }
  }
  const open = async () => {
    if (opening) return
    const controller = new AbortController()
    request.current = controller
    setOpening(true); setError('')
    try {
      const next = await api.me(target.worldId, controller.signal)
      if (controller.signal.aborted) return
      setPlayer(next)
      if (next.worldInstanceId !== target.worldId) {
        nav.push('01-3_world')
        setError('Этот мир больше недоступен. Выбери другой в списке миров.')
        return
      }
      nav.push('02-1_compose')
      dismiss()
    } catch { if (!controller.signal.aborted) setError('Не получилось открыть мир. Попробуй ещё раз.') }
    finally { if (request.current === controller) { request.current = null; setOpening(false) } }
  }
  if (dismissed) return null
  return <aside className="toast first-post-reminder" role="status" aria-label="Напоминание о первом посте">
    <span className="first-post-reminder-icon" aria-hidden="true">✍️</span>
    <div className="first-post-reminder-content"><b>Начни свою историю</b><p>Напиши первый пост в мире «{target.worldTitle}».</p>
      <button className="btn" disabled={opening} onClick={() => void open()}>{opening ? 'Открываем…' : 'Написать первый пост'}</button>
      {error && <p role="alert">{error}</p>}
    </div>
    <button className="first-post-reminder-close" aria-label="Закрыть напоминание" onClick={dismiss}>×</button>
  </aside>
}
