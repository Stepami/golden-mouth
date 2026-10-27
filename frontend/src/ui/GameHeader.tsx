import { usePlayer } from '../app/store'
import { useNav } from '../app/nav'
import { worldOf } from '../api/worldContent'
import { Avatar, fmt } from './index'
import { Icon } from './Icon'

export const SKILLS = [
  { id: 'brave', name: 'Смелость', icon: '🔥' },
  { id: 'humor', name: 'Юмор', icon: '😂' },
  { id: 'topic', name: 'В тему', icon: '🎯' },
] as const

/** Only values returned by the existing API. No invented day, timer or currency. */
export function GameHeader({ compact = false }: { compact?: boolean }) {
  const p = usePlayer(); const nav = useNav(); const w = worldOf(p.world)
  return <header className={`game-header${compact ? ' compact' : ''}`}>
    <div className="game-topline"><button className="icon-button" aria-label="Выбрать мир" onClick={() => nav.push('01-3_world')}><Icon name="back" /></button>
      <button className="game-world" onClick={() => nav.push('01-3_world')}><Avatar seed={p.handle} size={36} /><span><b>{w.title}</b><small>Сменить мир ▾</small></span></button>
      <button className="icon-button" aria-label="Настройки" onClick={() => nav.push('05-8_settings')}><Icon name="settings" /></button>
    </div>
    {!compact && <div className="game-resources"><div className="game-resource"><span>⚡ <b>{p.postsLeft}</b><small> / {p.postsCap}</small></span><small>Энергия · обновится в 09:00 МСК</small></div><div className="game-audience" aria-label={`${fmt(p.subs)} подписчиков`}><span>👥 <b>{fmt(p.subs)}</b></span><small>{w.title}</small></div></div>}
    <button className="game-skills" aria-label="Открыть историю и навыки" onClick={() => nav.push('05-13_progress')}>
      {SKILLS.map(({ id, icon, name }) => <span key={id}><span>{icon} {name} <b>ур. {p.skills[id].level}</b></span><i><em style={{ width: `${Math.max(0, Math.min(100, p.skills[id].progress * 100))}%` }} /></i></span>)}<Icon name="arrow" size={20} />
    </button>
  </header>
}
