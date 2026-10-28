import { useEffect, useState } from 'react'
import { api } from '../api/client'
import type { WorldDocument } from '../api/types'
import { useStore } from '../app/store'
import { useNav } from '../app/nav'
import { worldArt } from '../app/worldPrototype'
import { optionalWorldTemplate } from '../api/worldContent'
import { Avatar, Button, Sheet, fmt } from './index'

export function WorldPicker({ onClose }: { onClose: () => void }) {
  const { player: p, setPlayer } = useStore()
  const nav = useNav()
  const [worlds, setWorlds] = useState<WorldDocument[]>()
  const [error, setError] = useState('')
  const [attempt, setAttempt] = useState(0)
  const [all, setAll] = useState(false)
  const [switching, setSwitching] = useState(false)
  useEffect(() => {
    let active = true
    setError(''); setWorlds(undefined)
    void api.ownedWorlds().then(value => { if (active) setWorlds(value) }, () => {
      if (active) setError('Не удалось загрузить миры. Попробуй ещё раз.')
    })
    return () => { active = false }
  }, [attempt])
  if (!p) return null
  const ordered = worlds && [...worlds.filter(w => w.id === p.worldInstanceId), ...worlds.filter(w => w.id !== p.worldInstanceId)]
  const enter = async (id: string) => {
    if (switching) return
    if (id === p.worldInstanceId) { onClose(); return }
    setSwitching(true); setError('')
    try {
      const next = await api.me(id)
      if (next.worldInstanceId !== id) throw new Error('World unavailable')
      setPlayer(next); onClose(); nav.replace('03-5_stage')
    } catch { setError('Не удалось открыть мир. Попробуй ещё раз.') }
    finally { setSwitching(false) }
  }
  return <Sheet title={all ? 'Все мои миры' : 'Сменить мир'} className="world-picker-sheet" onClose={switching ? undefined : onClose}>
    <div className="world-picker" aria-busy={!worlds && !error || switching}>
      {!worlds && !error && <p role="status">Загружаем миры…</p>}
      {ordered?.slice(0, all ? undefined : 3).map(w => {
        const current = w.id === p.worldInstanceId
        const art = worldArt[w.id] ?? worldArt[optionalWorldTemplate(w)?.id ?? '']
        return <button className="picker-row" key={w.id} aria-current={current ? 'true' : undefined} disabled={switching} onClick={() => void enter(w.id)}>
          {art ? <img src={art.smallCover} alt="" /> : <Avatar seed={w.avatar} size={44} />}
          <span><strong>{w.title}</strong><small className="picker-player">{current ? p.name : w.name} · {current ? p.role : w.role}</small><small className="picker-subs">👥 {fmt(current ? p.subs : w.subscribers)}</small>{current && <small className="picker-current">Сейчас здесь</small>}</span><span aria-hidden="true">→</span>
        </button>
      })}
      {worlds?.length === 0 && <p>Пока нет начатых миров. Выбери первый в каталоге.</p>}
      {switching && <p role="status">Открываем мир…</p>}
      {error && <p role="alert">{error}</p>}
    </div>
    <footer className="picker-footer">
      {error && !worlds && <Button disabled={switching} onClick={() => setAttempt(value => value + 1)}>Повторить</Button>}
      {!all && ordered && ordered.length > 3 && <Button disabled={switching} onClick={() => setAll(true)}>Все мои миры · {ordered.length}</Button>}
      <button className="picker-catalog" disabled={switching} onClick={() => { onClose(); nav.push('01-3_world') }}>Каталог новых миров →</button>
    </footer>
  </Sheet>
}

/** Existing daily reset, not the prototype's unimplemented regeneration. */
export function EnergyHelp({ onClose }: { onClose: () => void }) {
  const { player: p } = useStore()
  if (!p) return null
  return <Sheet title="Как работает энергия" className="energy-help-sheet" onClose={onClose}>
    <div className="energy-help-compact">
      <div className="energy-help-rate"><span aria-hidden="true">⚡</span><strong>{p.postsLeft}<small>сейчас доступно</small></strong></div>
      <div className="energy-help-rules"><span>Обновление запаса</span><b>09:00 МСК</b><span>Пост или ответ</span><b>−1 ⚡</b><span>Чтение ленты</span><b>бесплатно</b></div>
      <p>Один запас на все миры.<br />Энергия обновляется каждый день.</p>
      <Button onClick={onClose}>Понятно</Button>
    </div>
  </Sheet>
}
