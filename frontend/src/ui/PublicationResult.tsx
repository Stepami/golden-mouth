import { useState } from 'react'
import type { PostResult } from '../api/types'
import type { SavedResult } from '../app/resultHistory'
import { useNav } from '../app/nav'
import { SKILLS } from './GameHeader'
import { Icon } from './Icon'
import { fmt } from './index'
import './results-parity.css'

/** Same saved API result in the publication peek, latest post and history. */
export function PublicationResult({ result, skills, expanded = false, onDiscussion }: { result: PostResult; skills?: SavedResult['skills']; expanded?: boolean; onDiscussion?: () => void }) {
  const nav = useNav()
  const [open, setOpen] = useState(expanded)
  const signed = (value: number) => `${value >= 0 ? '+' : '−'}${fmt(Math.abs(value))}`
  const changes = SKILLS.flatMap(skill => {
    const change = skills?.[skill.id]
    return change && Number.isFinite(change.before) && Number.isFinite(change.after) && change.after !== change.before
      ? [{ ...skill, gain: change.after - change.before }] : []
  })
  return <details className={`publication-result results-parity${result.delta < 0 ? ' negative' : ''}`} open={open} onToggle={event => setOpen(event.currentTarget.open)}>
    <summary aria-label={`Результат публикации: ${signed(result.delta)} подписчиков. ${open ? 'Свернуть' : 'Все изменения'}`}>
      <strong className={`results-outcome-audience ${result.delta < 0 ? 'down' : 'up'}`}>👥 {signed(result.delta)} подписчиков</strong>
      {changes.length > 0 && <span className="results-skill-deltas">{changes.map(({ id, icon, name, gain }) => <span key={id}>{icon} {name} <b className={gain < 0 ? 'down' : 'up'}>{signed(Number((gain * 100).toFixed(1)))}% ур.</b></span>)}</span>}
      <span className="results-expand">{open ? 'Свернуть ↑' : 'Все изменения ↓'}</span>
    </summary>
    <div className="results-breakdown">
      {result.headline && <p className="results-headline">{result.headline}</p>}
      {result.whys.length > 0 && <dl className="results-reasons">{result.whys.map((why, index) => <div key={`${why.label}:${index}`}><dt>{why.label}</dt><dd>{why.value}</dd></div>)}</dl>}
      {result.ribbons.map((ribbon, index) => <p className="results-ribbon" key={index}><span aria-hidden="true">{ribbon.icon}</span><span><b>{ribbon.title}</b>{ribbon.sub && <small>{ribbon.sub}</small>}</span></p>)}
      {!!result.completedWants?.length && <div className="results-completed">{result.completedWants.map(want => <p key={want.id}>✓ {want.text}</p>)}</div>}
      <div className="results-outcome-footer">{Number.isFinite(result.views) && <span className="muted" aria-label={`${fmt(result.views)} просмотров`}><Icon name="views" size={18} /> {fmt(result.views)}</span>}<button className="text-button" onClick={onDiscussion ?? (() => nav.push('02-6_thread', { postId: result.postId }))}>В комментарии →</button></div>
    </div>
  </details>
}
