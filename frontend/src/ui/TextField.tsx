import { useId, useLayoutEffect, useRef, useState } from 'react'
import { Avatar } from '.'

type Mention = { id: string; name: string; handle: string; avatarSeed: string }
const mentionAtEnd = /(?:^|[^\p{L}\p{N}._%+@-])@([\p{L}\p{N}_-]*)$/u

/** Готовая идея заполняет только полностью пустое поле. Очистка обратима. */
export function TextField({ label, value, onChange, ideas = [], maxLength = 2000, rows = 3, placeholder, disabled = false, fromIdea = false, visibleLength, onInteract, mentions }: {
  mentions?: readonly Mention[]
  visibleLength?: number; onInteract?: () => void
  label: string; value: string; onChange: (text: string, fromIdea?: boolean) => void; ideas?: string[]; maxLength?: number; rows?: number; placeholder?: string; disabled?: boolean; fromIdea?: boolean
}) {
  const input = useRef<HTMLTextAreaElement>(null)
  const wasRevealing = useRef(false)
  const [undo, setUndo] = useState<{ text: string; fromIdea: boolean } | null>(null)
  const menuId = useId()
  const [caret, setCaret] = useState<number | null>(null)
  const [active, setActive] = useState(0)
  const [dismissed, setDismissed] = useState(false)
  const match = !dismissed && !disabled && visibleLength === undefined && mentions?.length && caret !== null
    ? value.slice(0, caret).match(mentionAtEnd) : null
  const query = match?.[1].toLowerCase() ?? ''
  const choices = match ? (mentions ?? []).filter(item =>
    item.name.toLowerCase().split(/\s+/).some(part => part.startsWith(query)) || item.handle.toLowerCase().startsWith(query),
  ).slice(0, 5) : []
  const selected = Math.min(active, choices.length - 1)
  const updateCaret = (el: HTMLTextAreaElement) => {
    if (!mentions?.length) return
    const next = el.selectionStart === el.selectionEnd ? el.selectionStart : null
    if (next !== caret) setActive(0)
    setCaret(next)
  }
  const insertMention = (item: Mention) => {
    if (!match || caret === null) return
    const start = caret - match[1].length - 1
    const rest = value.slice(caret).replace(/^[\p{L}\p{N}_-]*/u, '')
    const prefix = value.slice(0, start) + '@' + item.handle
    const spacer = !rest || !/^[\s.,!?;:)\]]/.test(rest) ? ' ' : ''
    const next = prefix + spacer + rest
    if (next.length > maxLength) return
    setCaret(null)
    onChange(next)
    requestAnimationFrame(() => {
      const el = input.current
      if (!el) return
      el.focus()
      el.setSelectionRange(prefix.length + (spacer || rest.startsWith(' ') ? 1 : 0), prefix.length + (spacer || rest.startsWith(' ') ? 1 : 0))
    })
  }

  const finishReveal = () => {
    onInteract?.()
    if (visibleLength !== undefined && input.current) {
      input.current.value = value
      input.current.setSelectionRange(value.length, value.length)
      input.current.scrollTop = input.current.scrollHeight
    }
  }
  useLayoutEffect(() => {
    if (input.current && (visibleLength !== undefined || wasRevealing.current)) {
      input.current.scrollTop = input.current.scrollHeight
      input.current.scrollIntoView({ block: 'nearest' })
    }
    wasRevealing.current = visibleLength !== undefined
  }, [visibleLength])
  return <div className="text-field" onBlur={event => { if (!event.currentTarget.contains(event.relatedTarget)) setCaret(null) }}>
    <label className="label">{label}<textarea ref={input} aria-label={label} value={visibleLength === undefined ? value : value.slice(0, visibleLength)} onFocus={finishReveal} onPointerDown={finishReveal} onBeforeInput={finishReveal} onPaste={finishReveal}
      onSelect={event => updateCaret(event.currentTarget)}
      onChange={event => { updateCaret(event.currentTarget); setActive(0); setDismissed(false); onChange(event.currentTarget.value) }}
      aria-controls={choices.length ? menuId : undefined} aria-activedescendant={selected >= 0 ? `${menuId}-${selected}` : undefined}
      onKeyDown={event => {
        if (event.nativeEvent.isComposing || !choices.length) return
        if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
          event.preventDefault()
          setActive((selected + (event.key === 'ArrowDown' ? 1 : -1) + choices.length) % choices.length)
        } else if (event.key === 'Enter' && !event.shiftKey) {
          event.preventDefault()
          insertMention(choices[selected])
        } else if (event.key === 'Escape') {
          event.stopPropagation()
          setDismissed(true)
        }
      }} maxLength={maxLength} rows={rows} placeholder={placeholder} disabled={disabled} /></label>
    {choices.length > 0 && <div id={menuId} className="mention-menu" role="listbox" aria-label="Персонажи мира">
      <div className="mention-menu-label" aria-hidden="true">Персонажи мира</div>
      {choices.map((item, index) => <button key={item.id} id={`${menuId}-${index}`} type="button" role="option" aria-selected={index === selected}
        className="mention-choice" onPointerDown={e => e.preventDefault()} onClick={() => insertMention(item)}>
        <Avatar seed={item.avatarSeed} label={item.name} size={32} mode="default" />
        <span><b>{item.name}</b><small>@{item.handle}</small></span>
      </button>)}
    </div>}
    <div className="row small" style={{ margin: '6px 0 10px' }}>
      <button type="button" className="chip" disabled={disabled || value.length === 0} onClick={() => { setUndo({ text: value, fromIdea }); onChange(''); input.current?.focus() }}>Очистить</button>
      {undo !== null && value.length === 0 && <button type="button" className="chip" onClick={() => { onChange(undo.text, undo.fromIdea); setUndo(null); input.current?.focus() }}>Вернуть текст</button>}
    </div>
    {ideas.length > 0 && <><div className="small muted">{value.length ? 'Очисти поле, чтобы выбрать другую идею' : 'Готовые идеи — выбери и допиши'}</div>
      {ideas.map(idea => <button type="button" className="opt" key={idea} disabled={disabled || value.length > 0} onClick={() => { if (!disabled && value.length === 0) { onChange(idea.replace(/[«»]/g, ''), true); input.current?.focus() } }}>{idea}</button>)}</>}
  </div>
}
