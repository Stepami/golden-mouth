import type { Player } from '../api/types'
import { Card } from '.'

export function Streak({ streak }: { streak: Player['streak'] }) {
  const days = streak.days
  const multiYear = days > 360
  const yearly = days > 30 && !multiYear
  const monthly = days > 7 && !yearly && !multiYear
  const completed = multiYear ? Math.min(3, Math.floor(days / 360))
    : yearly ? Math.min(12, Math.floor(days / 30))
      : monthly ? (days === 30 ? 4 : Math.min(3, Math.floor(days / 7))) : days
  const now = new Date()
  const weekdays = ['пн', 'вт', 'ср', 'чт', 'пт', 'сб', 'вс']
  const months = ['янв', 'фев', 'мар', 'апр', 'май', 'июн', 'июл', 'авг', 'сен', 'окт', 'ноя', 'дек']
  const labels = multiYear
    ? Array.from({ length: 3 }, (_, i) => String(now.getFullYear() - Math.max(0, completed - 1) + i))
    : yearly ? months.map((_, i) => months[(now.getMonth() - Math.max(0, completed - 1) + i + 12) % 12])
      : monthly ? ['1', '2', '3', '4']
        : weekdays.map((_, i) => weekdays[((now.getDay() + 6) % 7 - Math.max(0, days - 1) + i + 7) % 7])
  const current = completed < labels.length ? completed : -1
  const unit = multiYear ? 'year' : yearly ? 'month' : monthly ? 'week' : 'day'
  const amount = multiYear ? Math.floor(days / 360) : yearly ? Math.floor(days / 30) : monthly ? completed : days
  const duration = new Intl.NumberFormat('ru', { style: 'unit', unit, unitDisplay: 'long' }).format(amount)
  return <Card style={{ marginBottom: 10 }}>
    <div className="row" style={{ justifyContent: 'space-between', marginBottom: 8 }}><b>Серия {duration}</b><span className="small" style={{ color: 'var(--gift)' }}>{streak.nextReward}</span></div>
    {(monthly || yearly || multiYear) && <div className="small muted" style={{ marginBottom: 6 }}>{multiYear ? 'Годы' : yearly ? 'Месяцы' : 'Недели'}</div>}
    <div className={yearly ? 'streak-months' : undefined} style={yearly ? undefined : { display: 'flex', gap: 6 }} role="img" aria-label={`${multiYear ? 'Годы' : yearly ? 'Месяцы' : monthly ? 'Недели' : 'Дни'}: ${completed} из ${labels.length}`}>
      {labels.map((label, i) => <div key={label} style={{ flex: 1, minWidth: 0, textAlign: 'center' }}>
        <div className={yearly ? 'streak-month-cell' : undefined} style={{
          height: yearly ? 'auto' : 30,
          width: multiYear ? 30 : undefined,
          margin: multiYear ? '0 auto' : undefined,
          aspectRatio: yearly || multiYear ? '1 / 1' : undefined,
          borderRadius: yearly ? 5 : 9,
          display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: yearly ? undefined : 14,
          ...(i < completed ? { background: 'var(--grad)' } : i === current ? { border: '2px dashed var(--accent)' } : { background: '#20202B' }),
        }}>{i < completed ? '🔥' : i === current && !monthly && !yearly && !multiYear ? '?' : ''}</div>
        <div className={`small muted${yearly ? ' streak-month-label' : ''}`} style={{ marginTop: 3 }}>{label}</div>
      </div>)}
    </div>
  </Card>
}
