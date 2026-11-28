import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import { api } from '../api/client'
import type { WorldDocument } from '../api/types'
import { displayHandle } from '../app/handle'
import { Avatar, Button } from '../ui'

export function WorldCharacterPicker({ currentWorldId, onChoose, onClose }: {
  currentWorldId?: string
  onChoose: (world: WorldDocument) => void
  onClose: () => void
}) {
  const dialog = useRef<HTMLDialogElement>(null)
  const [worlds, setWorlds] = useState<WorldDocument[] | null>(null)
  const [failed, setFailed] = useState(false)
  const [attempt, setAttempt] = useState(0)
  useLayoutEffect(() => {
    const element = dialog.current
    const overflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    element?.showModal()
    return () => { element?.close(); document.body.style.overflow = overflow }
  }, [])
  useEffect(() => {
    let active = true
    api.ownedWorlds().then(list => { if (active) setWorlds(list) }).catch(() => { if (active) setFailed(true) })
    return () => { active = false }
  }, [attempt])

  const sorted = worlds && [...worlds].sort((a, b) => Number(b.id === currentWorldId) - Number(a.id === currentWorldId))
  let content
  if (failed) {
    content = <><p role="alert">Не получилось загрузить твои истории.</p><Button onClick={() => { setFailed(false); setAttempt(value => value + 1) }}>Попробовать снова</Button></>
  } else if (!sorted) {
    content = <p className="muted" role="status">Загружаем истории…</p>
  } else if (!sorted.length) {
    content = <p className="muted">Здесь появятся персонажи из твоих историй.</p>
  } else {
    content = <div className="world-copy-list">
      {sorted.map(world => <button type="button" key={world.id} className="world-resume card" onClick={() => onChoose(world)}>
        <Avatar seed={world.avatar} avatarStyle={world.avatarStyle} size={42} />
        <div>
          <b>{world.name}</b><p className="small muted">{[displayHandle(world.avatar), world.role].filter(Boolean).join(' · ')}</p>
          <p className="small">{world.emoji} {world.title}</p>
        </div>
      </button>)}
    </div>
  }
  return <dialog ref={dialog} className="world-switch-dialog world-help-dialog" aria-labelledby="world-character-picker-title"
    onCancel={event => { event.preventDefault(); onClose() }} onClick={event => { if (event.target === event.currentTarget) onClose() }}>
    <div className="world-help-sheet">
      <h2 id="world-character-picker-title">Из какой истории взять персонажа?</h2>
      {content}
      <button type="button" className="btn secondary" onClick={onClose} autoFocus>Отмена</button>
    </div>
  </dialog>
}
