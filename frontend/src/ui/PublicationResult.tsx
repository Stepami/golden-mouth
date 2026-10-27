import { useState } from 'react'
import type { PostResult } from '../api/types'
import type { SavedResult } from '../app/resultHistory'
import { SKILLS } from './GameHeader'
import { fmt } from './index'

export function PublicationResult({ result, skills, expanded = false }: { result: PostResult; skills?: SavedResult['skills']; expanded?: boolean }) {
  const [open, setOpen] = useState(expanded)
  const signed = (value: number) => `${value >= 0 ? '+' : '−'}${fmt(Math.abs(value))}`
  return <section className={`publication-result${result.delta < 0 ? ' negative' : ''}`}>
    <h2 className={result.delta < 0 ? 'down' : 'up'}>👥 {signed(result.delta)} подписчиков</h2>
    {skills && <div className="result-skill-deltas">{SKILLS.map(({ id, icon, name }) => {
      const change = skills[id]
      if (!change || change.after <= change.before) return null
      return <span key={id}>{icon} {name} <b className="up">+{Number(((change.after - change.before) * 100).toFixed(1)).toLocaleString('ru-RU')}% ур.</b></span>
    })}</div>}
    <button className="text-button result-expand" aria-expanded={open} onClick={() => setOpen(!open)}>{open ? 'Свернуть ↑' : 'Все изменения ↓'}</button>
    {open && <div className="result-breakdown"><p>{result.headline}</p>{result.whys.map((why, index) => <div className="why" key={`${why.label}:${index}`}><span>{why.label}</span><b>{why.value}</b></div>)}{result.ribbons.map((ribbon, index) => <p key={index}>{ribbon.icon} <b>{ribbon.title}</b>{ribbon.sub && <span className="muted"> · {ribbon.sub}</span>}</p>)}</div>}
  </section>
}
