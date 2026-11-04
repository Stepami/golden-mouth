import { useEffect, useRef, useState } from 'react'
import { SkillHelp } from './SkillHelp'

export function useEditorSkillHelp(selector: string) {
  const [open, setOpen] = useState<{ example: number | null } | null>(null)
  const restore = useRef<(() => void) | null>(null)
  const show = (example: number | null = null) => {
    const input = document.querySelector<HTMLTextAreaElement>(selector)
    const start = input?.selectionStart, end = input?.selectionEnd, direction = input?.selectionDirection
    const scroll = input?.scrollTop
    restore.current = () => { if (input?.isConnected) { input.focus({ preventScroll: true }); if (start !== undefined && end !== undefined) input.setSelectionRange(start,end,direction ?? undefined); if (scroll !== undefined) input.scrollTop = scroll } }
    setOpen({ example })
  }
  useEffect(() => {
    if (open || !restore.current) return
    const frame = requestAnimationFrame(() => { restore.current?.(); restore.current = null })
    return () => cancelAnimationFrame(frame)
  }, [open])
  const close = () => setOpen(null)
  return { show, sheet: open && <SkillHelp initialExample={open.example} editor onClose={close} /> }
}
