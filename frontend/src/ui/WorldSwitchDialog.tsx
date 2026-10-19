import { useEffect, useRef } from 'react'
import { Button } from './index'

export function WorldSwitchDialog({ title, onConfirm, onCancel }: { title: string; onConfirm: () => void; onCancel: () => void }) {
  const dialog = useRef<HTMLDialogElement>(null)
  useEffect(() => {
    const previous = document.body.style.overflow
    const element = dialog.current
    document.body.style.overflow = 'hidden'
    element?.showModal()
    return () => { element?.close(); document.body.style.overflow = previous }
  }, [])
  return <dialog ref={dialog} className="world-switch-dialog" aria-labelledby="world-switch-title" onCancel={e => { e.preventDefault(); onCancel() }}>
    <h2 id="world-switch-title">Настроить другой мир?</h2>
    <p className="muted">Настройка «{title || 'Твой мир'}» сохранится на этом устройстве в текущей сессии. Когда снова выберешь этот мир, сможешь продолжить.</p>
    <Button onClick={onConfirm}>Настроить другой мир</Button>
    <Button secondary onClick={onCancel}>Остаться в текущем</Button>
  </dialog>
}
