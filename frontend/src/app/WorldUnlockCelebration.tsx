import { useEffect, useRef, useState } from 'react'
import { useNav } from './nav'
import { useStore } from './store'
import { beginOwnWorld } from './worldDraft'
import { Button } from '../ui'

export function WorldUnlockCelebration() {
  const { player, unlockCelebration, dismissUnlockCelebration } = useStore()
  const nav = useNav()
  const [preview, setPreview] = useState(() => import.meta.env.DEV && new URLSearchParams(location.search).get('preview') === 'world-unlock')
  const dismiss = () => { if (preview) setPreview(false); else dismissUnlockCelebration() }
  const dialog = useRef<HTMLDivElement>(null)
  const close = useRef<HTMLButtonElement>(null)
  const safe = ['01-3_world', '01-6_home', '03-5_stage', '05-5_collection', '05-11_world_rules', '02-3_result', '02-4_flop', '02-5_viral'].includes(nav.current)
  const visible = preview || (!!player && unlockCelebration && safe)
  useEffect(() => {
    if (!visible) return
    const previous = document.activeElement as HTMLElement | null
    close.current?.focus()
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    const escape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') dismiss()
      if (event.key === 'Tab') {
        const buttons = dialog.current?.querySelectorAll<HTMLButtonElement>('button')
        if (!buttons?.length) return
        const first = buttons[0], last = buttons[buttons.length - 1]
        if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus() }
        else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus() }
      }
    }
    window.addEventListener('keydown', escape)
    return () => { window.removeEventListener('keydown', escape); document.body.style.overflow = previousOverflow; previous?.focus() }
  }, [visible]) // eslint-disable-line react-hooks/exhaustive-deps
  if (!visible) return null
  return <div className="sheet-bg unlock-overlay"><div ref={dialog} className="sheet unlock-celebration" role="dialog" aria-modal="true" aria-labelledby="world-unlock-title">
    <button ref={close} className="unlock-close" aria-label="Закрыть поздравление" onClick={dismiss}>×</button>
    <div className="unlock-sparkles" aria-hidden="true">✨ 🌍 ✨</div>
    <span className="badge">НОВАЯ ВОЗМОЖНОСТЬ</span>
    <h1 id="world-unlock-title">Свой мир открыт!</h1>
    <p>Придумай место, героев и историю, которая будет только твоей.</p>
    <Button onClick={() => { dismiss(); if (preview || !player) { nav.push('01-3_world'); return }; beginOwnWorld(player); nav.push('05-7b_own_world', {}) }}>Создать свой мир</Button>
    <Button secondary onClick={dismiss}>Продолжить историю</Button>
    <p className="small muted">Доступ сохранён. Создать мир можно и позже — в разделе «Миры».</p>
  </div></div>
}
