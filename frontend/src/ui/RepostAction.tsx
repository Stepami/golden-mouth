import { useEffect, useId, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { api, ApiError } from '../api/client'
import { useNav } from '../app/nav'
import { usePlayer, useStore } from '../app/store'
import { Button, Sheet } from './index'

export function RepostAction({ postId, active = false, onChanged, undoOnly = false }: {
  postId: string; active?: boolean; onChanged?: () => void; undoOnly?: boolean
}) {
  const nav = useNav(); const p = usePlayer(); const { refresh, patch } = useStore()
  const [open, setOpen] = useState(false)
  const titleId = useId()
  const dialog = useRef<HTMLDivElement>(null)
  const [reposted, setReposted] = useState(active)
  const [pending, setPending] = useState(false)
  const busy = useRef(false)
  const [note, setNote] = useState('')
  const mounted = useRef(true)
  const latest = useRef(p); latest.current = p
  useEffect(() => { mounted.current = true; return () => { mounted.current = false } }, [])
  const world = p.worldInstanceId
  useEffect(() => { setReposted(active) }, [active])
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
  const act = async (remove: boolean) => {
    if (busy.current || !world) return
    busy.current = true; setPending(true); setNote('')
    try {
      const result = await (remove ? api.undoRepost(postId, world) : api.repost(postId, world))
      if (!mounted.current || latest.current.id !== p.id || latest.current.worldInstanceId !== world) return
      setReposted(result.active)
      patch({ postsLeft: result.energyLeft, subs: Number(result.subscribers) })
      await refresh(); onChanged?.()
      setNote(remove ? 'Репост убран из канала' : result.subscriberDelta > 0 ? `Репост в канале · +${result.subscriberDelta} подписчиков` : 'Репост в канале')
      setOpen(false)
    } catch (cause) {
      setNote(cause instanceof ApiError && cause.status === 409 ? 'Репост недоступен. Проверь запас энергии.' : 'Не удалось подтвердить действие. Обнови ленту перед повтором.')
    } finally { busy.current = false; setPending(false) }
  }
  return <>
    <button type="button" className="text-button repost-action" disabled={pending} onClick={event => {
      event.stopPropagation(); undoOnly ? void act(true) : setOpen(true)
    }}>{undoOnly ? 'Убрать репост' : reposted ? '↻ Репост в канале' : '↻ Репост · ⚡1'}</button>
    {note && <div className="small muted" role="status">{note}</div>}
    {open && createPortal(<Sheet onClose={() => { if (!pending) setOpen(false) }}>
      <div ref={dialog} role="dialog" aria-modal="true" aria-labelledby={titleId}>
      <h3 id={titleId} tabIndex={-1}>Репост</h3>
      <p className="small muted">Обычный репост — небольшой прирост аудитории. Со своим текстом — обычная публикация и рост навыков.</p>
      <div className="repost-options">
        <Button secondary disabled={pending} onClick={() => void act(reposted)}>{reposted ? 'Отменить репост' : 'Репостнуть · ⚡1'}</Button>
        <Button secondary disabled={pending} onClick={() => { setOpen(false); nav.push('02-1_compose', { quotedPostId: postId }) }}>Добавить свой текст · ⚡1</Button>
      </div>
      {reposted && <p className="small muted">Энергия за отменённый репост не возвращается.</p>}
      {note && <p className="small" role="alert">{note}</p>}
      <button className="text-button repost-action" disabled={pending} onClick={() => setOpen(false)}>Закрыть</button>
      </div>
    </Sheet>, document.body)}
  </>
}
