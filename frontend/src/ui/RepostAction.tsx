import { useEffect, useId, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { api } from '../api/client'
import { useNav } from '../app/nav'
import { usePlayer, useStore } from '../app/store'
import { Button, Sheet } from './index'

export function RepostAction({ postId, onChanged, undoOnly = false }: {
  postId: string; onChanged?: () => void; undoOnly?: boolean
}) {
  const nav = useNav(); const p = usePlayer(); const { refresh } = useStore()
  const [open, setOpen] = useState(false)
  const titleId = useId()
  const dialog = useRef<HTMLDivElement>(null)
  const [pending, setPending] = useState(false)
  const busy = useRef(false)
  const [note, setNote] = useState('')
  const mounted = useRef(true)
  const latest = useRef(p); latest.current = p
  useEffect(() => { mounted.current = true; return () => { mounted.current = false } }, [])
  const world = p.worldInstanceId
  useEffect(() => {
    if (!open) return
    const previous = document.activeElement as HTMLElement | null
    const heading = dialog.current?.querySelector<HTMLElement>('h3')
    heading?.focus()
    const handle = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !busy.current) setOpen(false)
      if (event.key !== 'Tab') return
      const buttons = [...(dialog.current?.querySelectorAll<HTMLButtonElement>('button:not(:disabled)') ?? [])]
      const first = buttons[0], last = buttons.at(-1)
      if (event.shiftKey && (document.activeElement === first || document.activeElement === heading)) { event.preventDefault(); last?.focus() }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus() }
    }
    document.addEventListener('keydown', handle)
    return () => { document.removeEventListener('keydown', handle); previous?.focus() }
  }, [open])
  const remove = async () => {
    if (busy.current || !world) return
    busy.current = true; setPending(true); setNote('')
    try {
      await api.deletePost(postId)
      if (!mounted.current || latest.current.id !== p.id || latest.current.worldInstanceId !== world) return
      await refresh(); onChanged?.()
      setNote('Репост убран из канала')
    } catch { setNote('Не удалось удалить репост. Обнови канал перед повтором.') }
    finally { busy.current = false; setPending(false) }
  }
  return <>
    <button type="button" className={undoOnly ? "text-button repost-action" : "post-repost-button"} aria-label={undoOnly ? undefined : "Репостнуть публикацию"} title={undoOnly ? undefined : "Репост"} disabled={pending} onClick={event => {
      event.stopPropagation(); undoOnly ? void remove() : setOpen(true)
    }}>{undoOnly ? 'Убрать репост' : <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d="m16 3 4 4-4 4M4 11V9a2 2 0 0 1 2-2h14M8 21l-4-4 4-4m12 0v2a2 2 0 0 1-2 2H4"/></svg>}</button>
    {note && <div className="small muted" role="status">{note}</div>}
    {open && createPortal(<Sheet onClose={() => { if (!pending) setOpen(false) }}>
      <div ref={dialog} role="dialog" aria-modal="true" aria-labelledby={titleId}>
      <h3 id={titleId} tabIndex={-1}>Репост</h3>
      <p className="small muted">+1 подписчик · навыки не растут</p>
      <div className="repost-options">
        <Button secondary onClick={() => { if (busy.current) return; busy.current = true; setOpen(false); nav.push('02-2_live', { text: '', ownWords: false, repostId: postId }) }}>Репостнуть · ⚡1</Button>
        <Button secondary disabled={pending} onClick={() => { if (busy.current) return; busy.current = true; setOpen(false); nav.push('02-1_compose', { repostId: postId }) }}>Добавить свой текст · ⚡1</Button>
      </div>
      {note && <p className="small" role="alert">{note}</p>}
      <button className="text-button repost-action" disabled={pending} onClick={() => setOpen(false)}>Закрыть</button>
      </div>
    </Sheet>, document.body)}
  </>
}
