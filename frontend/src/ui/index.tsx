import { useContext, useEffect, useState, type CSSProperties, type ReactNode } from 'react'
import { tap } from '../app/analytics'
import { AvatarWorld, AvatarPeople, type AvatarProfile } from './avatar'
import type { WorldId } from '../api/types'

/**
 * Круглая аватарка. seed это устойчивый признак того, кто на картинке:
 * у персонажа id из справочника, у игрока ник. Картинка собирается из него на месте,
 * поэтому отдельного файла или ссылки на аватар нигде не нужно.
 * Без сида рисуем букву — так показывается бот.
 */
export function Avatar({ seed, label, size = 40, style, world, profile, avatarStyle, mode }: { seed?: string; label?: string; size?: number; style?: CSSProperties; world?: WorldId; profile?: AvatarProfile; avatarStyle?: string; mode?: 'default' | 'profile' }) {
  const currentWorld = useContext(AvatarWorld)
  const people = useContext(AvatarPeople)
  const resolvedWorld = world ?? currentWorld
  const resolvedProfile = mode === 'default' ? undefined : profile ?? (seed ? people[seed] : undefined)
  const [portrait, setPortrait] = useState<{ key: string; uri: string }>()
  const portraitKey = JSON.stringify([seed, resolvedWorld, resolvedProfile, avatarStyle])
  useEffect(() => {
    let active = true
    if (seed) void import('./avatarRenderer').then(async ({ avatarUri }) => {
      const uri = await avatarUri(seed, resolvedWorld, resolvedProfile, avatarStyle)
      if (active) setPortrait({ key: portraitKey, uri })
    }).catch(() => { /* Keep a stable initial if a deferred resource cannot load. */ })
    return () => { active = false }
  }, [portraitKey])
  if (!seed || portrait?.key !== portraitKey) return <div style={{ width: size, height: size, borderRadius: '50%', background: 'var(--grad)', flex: 'none', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, fontSize: size * 0.4, ...style }}>{(label ?? seed ?? '?').replace(/^@+/, '').slice(0, 1).toUpperCase()}</div>
  return <img src={portrait.uri} alt="" width={size} height={size} style={{ borderRadius: '50%', flex: 'none', display: 'block', ...style }} />
}

export function Button({ children, secondary, onClick, disabled, style }: { children: ReactNode; secondary?: boolean; onClick?: () => void; disabled?: boolean; style?: CSSProperties }) {
  return <button className={`btn${secondary ? ' secondary' : ''}`} onClick={onClick} disabled={disabled} style={style}>{children}</button>
}

/** Кнопки внизу экрана: главная последней, вторичная над ней (как в макетах). */
export function BottomCTA({ main, onMain, second, onSecond, disabled, secondDisabled }: { main: ReactNode; onMain?: () => void; second?: ReactNode; onSecond?: () => void; disabled?: boolean; secondDisabled?: boolean }) {
  // id кнопки для аналитики: main / second на текущем экране (экран добавит analytics.ts)
  return <div className="bottom-cta">{second && <Button secondary disabled={secondDisabled} onClick={() => { tap('cta_second'); onSecond?.() }}>{second}</Button>}<Button onClick={() => { tap('cta_main'); onMain?.() }} disabled={disabled}>{main}</Button></div>
}

export function TopBar({ title, onBack, right, rightPlain = false, children }: { title?: ReactNode; onBack?: () => void; right?: ReactNode; rightPlain?: boolean; children?: ReactNode }) {
  return (
    <header className="top">
      {onBack && <button className="back" onClick={onBack} aria-label="Назад">‹</button>}
      {children ?? <b className="top-title">{title}</b>}
      {right !== undefined && <div className={rightPlain ? "top-action" : "pill"}>{right}</div>}
    </header>
  )
}

export function EnergyBadge({ value }: { value: number }) { return <span className="energy-badge" aria-label={`Осталось ${value} энергии`}>⚡ {value}</span> }

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
export function Whisper({ seed = 'mira', name = 'Мира', children }: { seed?: string; name?: string; children: ReactNode }) {
  return (
    <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end', margin: '8px 0 10px' }}>
      <Avatar seed={seed} size={30} mode="default" />
      <div style={{ background: '#251E33', border: '1px solid #9B6BFF77', borderRadius: '14px 14px 14px 4px', padding: '8px 11px', fontSize: 13, lineHeight: 1.35 }}>
        <div style={{ color: 'var(--lilac)', fontSize: 11, marginBottom: 2 }}>{name} подсказывает</div>{children}
      </div>
    </div>
  )
}

export function ExpandableText({ text }: { text: string }) {
  const [expanded, setExpanded] = useState(false)
  if (text.length < 220) return <>{text}</>
  return <div><div className={expanded ? '' : 'text-preview'}>{text}</div><button className="text-link read-more" aria-expanded={expanded} onClick={event => { event.stopPropagation(); setExpanded(!expanded) }}> {expanded ? 'Свернуть' : 'Читать дальше'}</button></div>
}

export function Comment({ seed, name, handle, text, star, me, onMore }: { seed: string; name: string; handle: string; text: ReactNode; star?: boolean; me?: boolean; onMore?: () => void }) {
  return (
    <div style={{ display: 'flex', gap: 10, marginBottom: 13 }}>
      <Avatar seed={seed} size={34} mode={me ? 'profile' : 'default'} />
      <div style={{ flex: 1 }}>
        <div className="row comment-heading" style={{ gap: 6 }}><b>{name}</b><span className="muted small">{handle}</span>{star && <span className="small" style={{ color: 'var(--accent)' }}>★ звезда</span>}{me && <span className="small muted">ты</span>}
          {onMore && <button onClick={onMore} aria-label="Пожаловаться" style={{ marginLeft: 'auto', background: 'none', border: 'none', color: 'var(--muted)' }}>⋯</button>}</div>
        <div style={{ marginTop: 6 }}>{typeof text === 'string' ? <ExpandableText text={text} /> : text}</div>
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
