import type { Player } from '../api/types'
import { Card } from '.'

export function Streak({ streak }: { streak: Player['streak'] }) {
  const days = ['пн', 'вт', 'ср', 'чт', 'пт', 'сб', 'вс']
  const today = streak.week.indexOf(false)
  return <Card style={{ marginBottom: 10 }}>
    <div className="row" style={{ justifyContent: 'space-between', marginBottom: 8 }}><b>Серия {streak.days} дня</b><span className="small" style={{ color: 'var(--gift)' }}>{streak.nextReward}</span></div>
    <div style={{ display: 'flex', gap: 6 }}>{days.map((day, i) => <div key={day} style={{ flex: 1, textAlign: 'center' }}><div style={{ height: 30, borderRadius: 9, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 14, ...(streak.week[i] ? { background: 'var(--grad)' } : i === today ? { border: '2px dashed var(--accent)' } : { background: '#20202B' }) }}>{streak.week[i] ? '🔥' : i === today ? '?' : ''}</div><div className="small muted" style={{ marginTop: 3 }}>{day}</div></div>)}</div>
  </Card>
}
