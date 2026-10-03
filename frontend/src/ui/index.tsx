import type { CSSProperties, ReactNode } from 'react'

const AVATARS: Record<string, string> = {
  liza: 'pick0', mira: 'mira', sonya: 'sonya', eva: 'eva', yuna: 'yuna', katya: 'katya', dima: 'dima', dasha: 'dasha', critic: 'critic',
  pick0: 'pick0', pick4: 'pick4', pick7: 'pick7', pick8: 'pick8', pick10: 'pick10',
}

export function Avatar({ id, size = 40, style }: { id: string; size?: number; style?: CSSProperties }) {
  const file = AVATARS[id]
  if (!file) return <div style={{ width: size, height: size, borderRadius: '50%', background: 'var(--grad)', flex: 'none', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, fontSize: size * 0.4, ...style }}>{id.slice(0, 1).toUpperCase()}</div>
  return <img src={`${import.meta.env.BASE_URL}avatars/${file}.svg`} alt="" width={size} height={size} style={{ borderRadius: '50%', flex: 'none', objectFit: 'cover', display: 'block', ...style }} />
}

export function Button({ children, secondary, onClick, disabled, style }: { children: ReactNode; secondary?: boolean; onClick?: () => void; disabled?: boolean; style?: CSSProperties }) {
  return <button className={`btn${secondary ? ' secondary' : ''}`} onClick={onClick} disabled={disabled} style={style}>{children}</button>
}

/** Кнопки внизу экрана: главная последней, вторичная над ней (как в макетах). */
export function BottomCTA({ main, onMain, second, onSecond, disabled }: { main: ReactNode; onMain?: () => void; second?: ReactNode; onSecond?: () => void; disabled?: boolean }) {
  return <div className="bottom-cta">{second && <Button secondary onClick={onSecond}>{second}</Button>}<Button onClick={onMain} disabled={disabled}>{main}</Button></div>
}

export function TopBar({ title, onBack, right, children }: { title?: ReactNode; onBack?: () => void; right?: ReactNode; children?: ReactNode }) {
  return (
    <div className="top">
      {onBack && <button className="back" onClick={onBack} aria-label="Назад">‹</button>}
      {children ?? <b style={{ fontSize: 17 }}>{title}</b>}
      {right !== undefined && <div className="pill">{right}</div>}
    </div>
  )
}

export function Card({ children, style, onClick }: { children: ReactNode; style?: CSSProperties; onClick?: () => void }) {
  return <div className="card" style={style} onClick={onClick}>{children}</div>
}

export function Gift({ children, style }: { children: ReactNode; style?: CSSProperties }) {
  return <div className="gift" style={style}>🎁 {children}</div>
}

export function Chip({ children, on, idea, onClick }: { children: ReactNode; on?: boolean; idea?: boolean; onClick?: () => void }) {
  return <button className={`chip${on ? ' on' : ''}${idea ? ' idea' : ''}`} onClick={onClick}>{children}</button>
}

export function SkillBar({ name, level, progress }: { name: string; level?: number; progress: number }) {
  return (
    <div className="row" style={{ fontSize: 13, marginBottom: 6 }}>
      <span style={{ width: 70, color: 'var(--soft)' }}>{name}</span>
      <div className="bar"><i style={{ width: `${Math.round(progress * 100)}%` }} /></div>
      {level !== undefined && <b className="small" style={{ width: 46, textAlign: 'right' }}>ур. {level}</b>}
    </div>
  )
}

export function Ribbon({ icon, title, sub, onClick }: { icon: string; title: ReactNode; sub?: ReactNode; onClick?: () => void }) {
  return (
    <div onClick={onClick} className="row" style={{ background: '#22222D', borderRadius: 14, padding: '9px 12px', marginBottom: 8 }}>
      <div style={{ fontSize: 20 }}>{icon}</div>
      <div style={{ flex: 1, fontSize: 14 }}><b>{title}</b>{sub && <div className="small muted">{sub}</div>}</div>
      <span className="muted">›</span>
    </div>
  )
}

/** Подсказка звезды: фиолетовый пузырь с аватаркой. */
export function Whisper({ star = 'mira', name = 'Мира', children }: { star?: string; name?: string; children: ReactNode }) {
  return (
    <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end', margin: '8px 0 10px' }}>
      <Avatar id={star} size={30} />
      <div style={{ background: '#251E33', border: '1px solid #9B6BFF77', borderRadius: '14px 14px 14px 4px', padding: '8px 11px', fontSize: 13, lineHeight: 1.35 }}>
        <div style={{ color: 'var(--lilac)', fontSize: 11, marginBottom: 2 }}>{name} подсказывает</div>{children}
      </div>
    </div>
  )
}

export function Comment({ avatar, name, handle, text, star, me, onMore }: { avatar: string; name: string; handle: string; text: ReactNode; star?: boolean; me?: boolean; onMore?: () => void }) {
  return (
    <div style={{ display: 'flex', gap: 10, marginBottom: 13 }}>
      <Avatar id={avatar} size={34} />
      <div style={{ flex: 1 }}>
        <div className="row" style={{ gap: 6 }}><b>{name}</b><span className="muted small">{handle}</span>{star && <span className="small" style={{ color: 'var(--accent)' }}>★ звезда</span>}{me && <span className="small muted">ты</span>}
          <button onClick={onMore} aria-label="Пожаловаться" style={{ marginLeft: 'auto', background: 'none', border: 'none', color: 'var(--muted)' }}>⋯</button></div>
        <div style={{ marginTop: 2 }}>{text}</div>
      </div>
    </div>
  )
}

export function Bubble({ children, me }: { children: ReactNode; me?: boolean }) {
  return (
    <div style={{ display: 'flex', justifyContent: me ? 'flex-end' : 'flex-start', marginBottom: 8 }}>
      <div style={{ maxWidth: '78%', padding: '9px 12px', borderRadius: 18, ...(me ? { background: 'linear-gradient(135deg,#FF6B8B,#9B6BFF)', borderBottomRightRadius: 6 } : { background: '#22222D', borderBottomLeftRadius: 6 }) }}>{children}</div>
    </div>
  )
}

export function Toggle({ on, onChange }: { on: boolean; onChange?: (v: boolean) => void }) {
  return (
    <button onClick={() => onChange?.(!on)} aria-pressed={on} style={{ width: 42, height: 26, borderRadius: 13, border: 'none', background: on ? '#2E9E6F' : '#33323F', position: 'relative', flex: 'none' }}>
      <i style={{ position: 'absolute', [on ? 'right' : 'left']: 3, top: 3, width: 20, height: 20, borderRadius: '50%', background: '#fff' }} />
    </button>
  )
}

export function Segmented<T extends string>({ options, value, onChange }: { options: { id: T; label: string }[]; value: T; onChange: (v: T) => void }) {
  return (
    <div style={{ display: 'flex', gap: 4, background: '#14141B', borderRadius: 11, padding: 3 }}>
      {options.map(o => (
        <button key={o.id} onClick={() => onChange(o.id)} style={{ flex: 1, padding: '7px 0', borderRadius: 9, border: 'none', fontSize: 13, background: o.id === value ? 'var(--grad)' : 'none', color: o.id === value ? '#fff' : 'var(--muted2)' }}>{o.label}</button>
      ))}
    </div>
  )
}

export function Sheet({ children, onClose }: { children: ReactNode; onClose?: () => void }) {
  return <div className="sheet-bg" onClick={onClose}><div className="sheet" onClick={e => e.stopPropagation()}>{children}</div></div>
}

/** Сложность мира точками ●●○ */
export function Dots({ n, of = 3 }: { n: number; of?: number }) {
  return <span>{Array.from({ length: of }, (_, i) => <i key={i} style={{ display: 'inline-block', width: 7, height: 7, borderRadius: '50%', marginRight: 3, background: i < n ? 'var(--accent)' : '#33323F' }} />)}</span>
}

/** Числа как в макетах: 2 310, 48k */
export const fmt = (n: number) => n >= 100000 ? `${Math.round(n / 1000)}k` : n.toLocaleString('ru-RU').replace(/ /g, ' ')
export const plural = (n: number, one: string, few: string, many: string) => {
  const a = Math.abs(n) % 100, b = a % 10
  return a > 10 && a < 20 ? many : b > 1 && b < 5 ? few : b === 1 ? one : many
}
/** Короткие числа для реакций и охвата: 840, 3,2k, 14k, 1,2M */
export const short = (n: number) => n < 1000 ? String(n) : n < 1e6 ? `${(n / 1000).toFixed(n < 10000 ? 1 : 0).replace('.', ',').replace(',0', '')}k` : `${(n / 1e6).toFixed(1).replace('.', ',').replace(',0', '')}M`
