import { useLayoutEffect, useRef, useState } from 'react'

/** Готовая идея заполняет только полностью пустое поле. Очистка обратима. */
export function TextField({ label, value, onChange, ideas = [], maxLength = 2000, rows = 3, placeholder, disabled = false, fromIdea = false, visibleLength, onInteract }: {
  visibleLength?: number; onInteract?: () => void
  label: string; value: string; onChange: (text: string, fromIdea?: boolean) => void; ideas?: string[]; maxLength?: number; rows?: number; placeholder?: string; disabled?: boolean; fromIdea?: boolean
}) {
  const input = useRef<HTMLTextAreaElement>(null)
  const wasRevealing = useRef(false)
  const [undo, setUndo] = useState<{ text: string; fromIdea: boolean } | null>(null)
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
  return <div className="text-field">
    <label className="label">{label}<textarea ref={input} aria-label={label} value={visibleLength === undefined ? value : value.slice(0, visibleLength)} onFocus={finishReveal} onPointerDown={finishReveal} onBeforeInput={finishReveal} onPaste={finishReveal} onChange={e => onChange(e.target.value)} maxLength={maxLength} rows={rows} placeholder={placeholder} disabled={disabled} /></label>
    <div className="row small" style={{ margin: '6px 0 10px' }}>
      <button type="button" className="chip" disabled={disabled || value.length === 0} onClick={() => { setUndo({ text: value, fromIdea }); onChange(''); input.current?.focus() }}>Очистить</button>
      {undo !== null && value.length === 0 && <button type="button" className="chip" onClick={() => { onChange(undo.text, undo.fromIdea); setUndo(null); input.current?.focus() }}>Вернуть текст</button>}
    </div>
    {ideas.length > 0 && <><div className="small muted">{value.length ? 'Очисти поле, чтобы выбрать другую идею' : 'Готовые идеи — выбери и допиши'}</div>
      {ideas.map(idea => <button type="button" className="opt" key={idea} disabled={disabled || value.length > 0} onClick={() => { if (!disabled && value.length === 0) { onChange(idea.replace(/[«»]/g, ''), true); input.current?.focus() } }}>{idea}</button>)}</>}
  </div>
}
