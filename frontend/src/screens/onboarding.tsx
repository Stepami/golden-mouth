import { useEffect, useRef, useState } from 'react'
import { withWorldTemplates } from '../app/withWorldTemplates'
import { ageBand } from '../app/ageBand'
import { worldOf, contentOf } from '../api/worldContent'
import { useNav } from '../app/nav'
import { useStore } from '../app/store'
import { Avatar, BottomCTA, Card, fmt } from '../ui'
import { track } from '../app/log'
import { register } from '.'
import { WorldOverview, WorldPrototypePreview } from './worldQuick'
import { ChooseWorld as World, ChannelSetup as Channel, VoiceSetup as WorldSetup, StarsSetup as FirstStar } from './worldWizard'

/** Точки прогресса онбординга и «назад», кроме первого шага. */
function Steps({ n, back = true }: { n: number; back?: boolean }) {
  const nav = useNav()
  return (
    <div className="row" style={{ padding: '10px 16px 0', height: 40 }}>
      {back && <button className="back" onClick={nav.back} style={{ background: 'none', border: 'none', fontSize: 24, color: 'var(--soft)' }} aria-label="Назад">‹</button>}
      <div style={{ marginLeft: 'auto' }} aria-label={`Шаг ${n} из 5`}>{[1, 2, 3, 4, 5].map(i => <i key={i} style={{ display: 'inline-block', width: i === n ? 18 : 6, height: 6, borderRadius: 3, marginLeft: 5, background: i <= n ? 'var(--accent)' : '#33323F' }} />)}</div>
    </div>
  )
}

// 01-1: сообщение друга в Telegram (вход по карточке)
function Invite() {
  const nav = useNav()
  return <div className="screen no-tab welcome"><div className="pad">
    <div className="brand">реки</div><h1>Твой блог.<br />Твоя история.</h1>
    <p className="muted">Пиши посты, знакомься с героями и меняй события своего мира.</p>
    <div className="welcome-post card" aria-label="Пример игрового поста">
      <div className="row"><div className="welcome-avatar">✦</div><div><b>Твой канал</b><div className="small muted">только что</div></div><span className="small muted" style={{ marginLeft: 'auto' }}>120 читателей</span></div>
      <p>На моей фотографии появилась улица, которой вчера не было. Кто-нибудь узнаёт эти окна? 🌲</p>
      <div className="row"><span className="react">👀 24</span><span className="react">✨ 12</span><span className="small muted">💬 3</span></div>
      <div className="welcome-reply"><b>Герой мира</b><div>Я помню этот дом. Но на карте его нет уже сорок лет.</div></div>
    </div>
    <p className="small muted">Фанаты и события виртуальные. Посты остаются внутри игры.</p>
  </div><BottomCTA main="Начать играть" onMain={() => nav.push('01-2_age')} /></div>
}

// 01-2: год рождения, согласие не отмечено заранее; младше 14 → 01-2b
function Age() {
  const nav = useNav()
  const currentYear = new Date().getFullYear()
  const years = Array.from({ length: 91 }, (_, i) => currentYear - 10 - i)
  const [year, setYear] = useState(2010)
  const [agree, setAgree] = useState(false)
  const wheel = useRef<HTMLDivElement>(null)
  useEffect(() => { if (wheel.current) wheel.current.scrollTop = years.indexOf(2010) * 38 }, [])
  const onWheel = () => { const el = wheel.current; if (el) setYear(years[Math.min(years.length - 1, Math.max(0, Math.round(el.scrollTop / 38)))]) }
  const age = new Date().getFullYear() - year
  return (
    <div className="screen no-tab">
      <Steps n={1} back={false} />
      <div style={{ padding: '4px 20px' }}>
        <div className="brand" style={{ fontSize: 26 }}>Реки</div>
        <div style={{ fontSize: 24, fontWeight: 800, margin: '14px 0 6px' }}>Год рождения</div>
        <div className="muted" style={{ marginBottom: 10 }}>Чтобы звёзды говорили с тобой как надо</div>
        <div ref={wheel} onScroll={onWheel} style={{ height: 230, overflowY: 'auto', scrollSnapType: 'y mandatory', background: '#14141B', borderRadius: 18, textAlign: 'center', padding: '96px 0' }}>
          {years.map(y => <div key={y} onClick={() => setYear(y)} style={{ scrollSnapAlign: 'center', height: 38, lineHeight: '38px', fontSize: y === year ? 22 : 18, fontWeight: y === year ? 800 : 400, color: y === year ? 'var(--text)' : 'var(--muted)', background: y === year ? '#22222D' : 'none', borderRadius: 10, margin: '0 12px' }}>{y}</div>)}
        </div>
        <Card style={{ marginTop: 18 }}>
          <b>Сохраним твою игру?</b>
          <div className="small muted" style={{ margin: '6px 0 10px' }}>Храним только Telegram ID и игру, сервер в России. Удалить всё можно в профиле в один тап. <span style={{ color: '#8CC8FF' }}>Подробнее</span></div>
          <label className="row" style={{ gap: 8 }}>
            <button onClick={() => setAgree(!agree)} aria-pressed={agree} style={{ width: 22, height: 22, borderRadius: 6, border: agree ? 'none' : '2px solid #55536A', background: agree ? 'var(--grad)' : 'none', color: '#fff', fontSize: 14 }}>{agree ? '✓' : ''}</button>
            <span className="small">Согласие на обработку данных</span>
          </label>
        </Card>
        {!agree && <div className="small muted" style={{ textAlign: 'center', marginTop: 10 }}>Отметь согласие, чтобы продолжить</div>}
      </div>
      <BottomCTA main="Дальше" disabled={!agree} onMain={() => { track('onboarding_step', { step: 'age', ok: age >= 14 }); nav.push(age < 14 ? '01-2b_under14' : '01-3_world', { group: age < 18 ? '14-17' : '18+', ageBand: ageBand(age) }) }} />
    </div>
  )
}

function Under14() {
  const nav = useNav()
  return (
    <div className="screen no-tab" style={{ padding: '60px 26px', textAlign: 'center' }}>
      <div className="brand" style={{ fontSize: 26 }}>Реки</div>
      <div style={{ fontSize: 64, margin: '40px 0 14px' }}>🌱</div>
      <div style={{ fontSize: 24, fontWeight: 800, marginBottom: 10 }}>Пока рановато</div>
      <div className="muted">Реки для тех, кому есть 14. Мы ничего не сохранили. Возвращайся, когда исполнится 14, мы будем ждать</div>
      <BottomCTA main="Понятно" onMain={() => nav.replace('01-1_invite')} />
    </div>
  )
}

// 01-6: первый экран: цель, квест, что такое Реки / навыки / коллекция
export function ChannelHead() {
  const p = useStore().player!
  const w = worldOf(p.world)
  return (
    <div className="top">
      <Avatar seed={p.handle} size={36} />
      <div><b>{p.name}</b><div className="muted small">{fmt(p.subs)} подписчиков · {w.emoji} {w.title}</div></div>
      <div className="pill">✍️ осталось {p.postsLeft}</div>
    </div>
  )
}
export function Home() {
  const nav = useNav(); const p = useStore().player!; const profile = contentOf(p.world)
  return <div className="screen"><ChannelHead /><div className="pad">
    <div className="card first-story"><div className="small eyebrow">Твоя история начинается</div><p className="first-story-context">{profile.firstContext}</p><p className="muted first-story-prompt">{profile.firstPrompt}</p>
      {p.firstPostInviteBonus > 0 && <p>🎁 Первый пост даст ещё {p.firstPostInviteBonus} ходов</p>}
      <button className="btn" onClick={() => nav.push('02-1_compose')}>Написать первый пост</button>
    </div><p className="small muted first-story-footer">Твои посты меняют историю</p>
  </div></div>
}

register({
  '01-1_invite': { component: Invite, title: 'Приглашение от друга' },
  '01-2_age': { component: Age, title: 'Год рождения и согласие' },
  '01-2b_under14': { component: Under14, title: 'Младше 14' },
  '01-3_world': { component: withWorldTemplates(World), title: 'Выбор мира' },
  '01-3b_world_overview': { component: withWorldTemplates(WorldOverview), title: 'Обзор мира' },
  '05-7c_world_preview': { component: WorldPrototypePreview, title: 'Демо своего мира' },
  '01-4_channel': { component: withWorldTemplates(Channel), title: 'Канал' },
  '01-4b_world_setup': { component: withWorldTemplates(WorldSetup), title: 'Мир, голос канала и драма' },
  '01-5_first_star': { component: withWorldTemplates(FirstStar), title: 'Настройка героев' },
  '01-6_home': { component: Home, title: 'Первый экран' },
}, { '01-6_home': 0 })
