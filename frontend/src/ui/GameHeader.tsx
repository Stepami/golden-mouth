import { useState } from 'react'
import { usePlayer } from '../app/store'
import { useNav } from '../app/nav'
import { worldOf } from '../api/worldContent'
import { worldArt } from '../app/worldPrototype'
import { Avatar, fmt } from './index'
import { Icon } from './Icon'
import { WorldSettings } from './WorldSettings'
import { EnergyHelp, WorldPicker } from './GameSheets'

export const SKILLS = [
  { id: 'brave', name: 'Смелость', icon: '🔥' },
  { id: 'humor', name: 'Юмор', icon: '😂' },
  { id: 'topic', name: 'В тему', icon: '🎯' },
] as const

/** Only values returned by the existing API. No invented day, timer or currency. */
export function GameHeader({ compact = false }: { compact?: boolean }) {
  const p = usePlayer(); const nav = useNav(); const w = worldOf(p.world)
  const [sheet, setSheet] = useState<'worlds' | 'energy' | 'settings' | null>(null)
  return <><header className={`game-header${compact ? ' compact' : ''}`}>
    <div className="game-topline"><button className="icon-button" aria-label="Выбрать мир" onClick={() => nav.push('01-3_world')}><Icon name="back" /></button>
      <button className="game-world" disabled={nav.current === '02-2_live'} onClick={() => setSheet('worlds')}>{worldArt[p.world] ? <img className="header-world-cover" src={worldArt[p.world].smallCover} alt="" /> : <Avatar seed={p.handle} size={36} />}<span><b>{w.title}</b><small>Сменить мир ▾</small></span></button>
      <button className="icon-button" aria-label="Настройки" onClick={() => setSheet('settings')}><Icon name="settings" /><small>Настройки</small></button>
    </div>
    {compact && <div className="compact-audience"><span>👥 <b>{fmt(p.subs)}</b> {w.title}</span></div>}
    {!compact && <div className="game-resources"><button className="game-resource" aria-label="Как работает энергия" onClick={() => setSheet('energy')}><span>⚡ <b>{p.postsLeft}</b><small> / {p.postsCap}</small></span><small>Энергия · обновится в 09:00 МСК</small></button><div className="game-audience" aria-label={`${fmt(p.subs)} подписчиков`}><span>👥 <b>{fmt(p.subs)}</b></span><small>{w.title}</small></div></div>}
    {!compact && <button className="game-skills" aria-label="Открыть историю и навыки" onClick={() => nav.push('05-13_progress')}>
      {SKILLS.map(({ id, icon, name }) => <span key={id}><span>{icon} {name} <b>ур. {p.skills[id].level}</b></span><i><em style={{ width: `${Math.max(0, Math.min(100, p.skills[id].progress * 100))}%` }} /></i></span>)}<Icon name="arrow" size={20} />
    </button>}
  </header>{sheet === 'worlds' && <WorldPicker onClose={() => setSheet(null)} />}{sheet === 'settings' && <WorldSettings onClose={() => setSheet(null)} />}{sheet === 'energy' && <EnergyHelp onClose={() => setSheet(null)} />}</>
}
