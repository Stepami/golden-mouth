import './entry-parity.css'
import leraPortrait from '../assets/onboarding-lera.jpg'
import welcomeSki from '../assets/welcome-ski.jpg'
import { GameHeader } from '../ui/GameHeader'
import { Typewriter } from '../ui/Typewriter'
import { displayHandle } from '../app/handle'
import { characterGuidanceFor } from '../app/characterGuidance'
import { WorldCover } from '../ui/WorldCover'
import { useEffect, useRef, useState } from 'react'
import { withWorldTemplates } from '../app/withWorldTemplates'
import { ageBand } from '../app/ageBand'
import { worldOf, contentOf, worldDocumentOf } from '../api/worldContent'
import { useNav } from '../app/nav'
import { useStore } from '../app/store'
import { Avatar, BottomCTA, Card, fmt } from '../ui'
import { track } from '../app/log'
import { register } from '.'
import { worldArt } from '../app/worldPrototype'
import { WorldOverview, WorldPrototypePreview } from './worldQuick'
import { ChooseWorld as World, ChannelSetup as Channel, VoiceSetup as WorldSetup, StarsSetup as FirstStar } from './worldWizard'

/** Точки прогресса онбординга и «назад», кроме первого шага. */
function Steps({ n, back = true }: { n: number; back?: boolean }) {
  const nav = useNav()
  return (
    <div className="row" style={{ padding: '10px 16px 0', height: 40 }}>
      {back && <button className="back" onClick={nav.back} style={{ background: 'none', border: 'none', fontSize: 24, color: 'var(--soft)' }} aria-label="Назад">‹</button>}
      <div style={{ marginLeft: 'auto' }} aria-label={`Шаг ${n} из 5`}>{[1, 2, 3, 4, 5].map(i => <i key={i} style={{ display: 'inline-block', width: i === n ? 18 : 6, height: 6, borderRadius: 3, marginLeft: 5, background: i <= n ? 'var(--accent)' : 'var(--line)' }} />)}</div>
    </div>
  )
}

const welcomeHeadlines = [
  ['Пиши.', 'Тебя услышат.'],
  ['Будь собой.', 'Или попробуй иначе.'],
  ['Собери своих.', 'По характеру.'],
  ['А что, если', 'первым написать?'],
]

function WelcomeHeadline() {
  const [index, setIndex] = useState(0)
  const [paused, setPaused] = useState(false)
  const [reduced, setReduced] = useState(() => matchMedia('(prefers-reduced-motion: reduce)').matches)
  useEffect(() => {
    const motion = matchMedia('(prefers-reduced-motion: reduce)')
    const update = () => setReduced(motion.matches)
    motion.addEventListener('change', update)
    return () => motion.removeEventListener('change', update)
  }, [])
  useEffect(() => {
    if (paused || reduced || new URLSearchParams(location.search).has('shot')) return
    const timer = window.setInterval(() => setIndex(i => (i + 1) % welcomeHeadlines.length), 3400)
    return () => window.clearInterval(timer)
  }, [paused, reduced])
  return <div className="welcome-thesis-wrap"><h1 className="welcome-headline"><span>{welcomeHeadlines[index].join("\n")}</span></h1>{!reduced && <button className="welcome-text-motion" aria-label={paused ? 'Продолжить смену тезисов' : 'Остановить смену тезисов'} aria-pressed={paused} onClick={() => setPaused(value => !value)}>{paused ? '▷' : 'Ⅱ'}</button>}</div>
}

// 01-1: welcome montage from the approved prototype.
function Invite() {
  const nav = useNav()
  return <main className="welcome-screen">
    <div className="welcome-brand">реки.</div>
    <div className="welcome-scene">
      <article className="welcome-social-post"><div className="row"><span className="welcome-self">ты</span><div><b>Твой пост</b><small className="muted">только что</small></div><span className="muted" style={{ marginLeft: 'auto' }}>···</span></div><p>Хочу в горы. Кто со мной? 🏔️</p><img src={welcomeSki} alt="Друзья на горнолыжном склоне" /><small className="muted">♡ 12　Макс и ещё 11</small></article>
      <article className="welcome-social-reply"><span className="welcome-max">М</span><div><b>Макс</b><p>С тобой — хоть на край света. Горы тоже подойдут 😏</p></div></article>
      <div className="welcome-notification"><span>↗</span><div><b>Макс написал тебе</b><small className="muted">Вдвоём поедем? 👀</small></div><b className="welcome-unread">1</b></div>
    </div>
    <div className="welcome-promise"><p className="small muted">Твой блог. Твоя компания.</p><WelcomeHeadline /><p className="muted">Здесь нет реальных собеседников — только ИИ. Пробуй общение без оглядки на знакомых.</p></div>
    <button className="btn" onClick={() => nav.push('01-2_age')}>Попробовать →</button>
  </main>
}

// 01-2: возраст, согласие не отмечено заранее; младше 14 → 01-2b
function Age() {
  const nav = useNav()
  const ages = Array.from({ length: 92 }, (_, i) => i + 8)
  const [age, setAge] = useState(18)
  const [agree, setAgree] = useState(false)
  const wheel = useRef<HTMLDivElement>(null)
  useEffect(() => { if (wheel.current) wheel.current.scrollTop = (18 - 8) * 44 }, [])
  const selectAge = (value: number) => {
    setAge(value)
    wheel.current?.scrollTo({ top: (value - 8) * 44, behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth' })
  }
  return <main className="age-screen">
    <div className="welcome-brand">реки.</div>
    <button className="age-back icon-button" aria-label="Назад" onClick={nav.back}>‹</button>
    <div className="age-copy"><p className="small muted">Знакомство · 1 / 1</p><h1>Сколько<br />тебе лет?</h1><p className="muted">Дальше — выбор первого мира.</p></div>
    <div className="age-wheel">
      <div ref={wheel} className="age-wheel-list" role="listbox" aria-label="Твой возраст" tabIndex={0}
        onKeyDown={event => { if (['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) { event.preventDefault(); selectAge(event.key === 'Home' ? 8 : event.key === 'End' ? 99 : Math.max(8, Math.min(99, age + (event.key === 'ArrowDown' ? 1 : -1)))) } }}
        onScroll={() => { if (wheel.current) setAge(Math.max(8, Math.min(99, 8 + Math.round(wheel.current.scrollTop / 44)))) }}>
        {ages.map(value => <button key={value} role="option" aria-selected={value === age} tabIndex={-1} onClick={() => selectAge(value)}>{value}</button>)}
      </div><span className="age-wheel-unit" aria-hidden="true">лет</span>
    </div>
    <label className="age-consent"><input type="checkbox" checked={agree} onChange={event => setAgree(event.target.checked)} /><span>Даю согласие на обработку персональных данных.</span></label>
    <button className="btn" disabled={!agree} onClick={() => { track('onboarding_step', { step: 'age', ok: age >= 14 }); nav.push(age < 14 ? '01-2b_under14' : '01-2c_guide', { group: age < 18 ? '14-17' : '18+', ageBand: ageBand(age) }) }}>Продолжить →</button>
  </main>
}

function LeraGuide() {
  const nav = useNav()
  const [step, setStep] = useState(0)
  const finish = () => nav.push('01-3_world', nav.params)
  const title = step ? 'Теперь выбери мир' : 'Привет, я Лера!'
  const paragraphs = step
    ? ['В каждом мире — своя сюжетная линия и уникальные персонажи. Выбери, куда хочешь попасть.']
    : ['Коротко расскажу про Реки.', 'Здесь твой блог читают только ИИ-персонажи. Пиши то, что хочется, знакомься и пробуй разные роли.', 'Время здесь идёт быстрее: можно за вечер набрать аудиторию и посмотреть, каково это — когда тебя знают.']
  return <main className="lera-screen">
    <div className="lera-top"><button className="back" aria-label="Назад" onClick={() => step ? setStep(0) : nav.back()}>‹</button><span>{step + 1} / 2</span><button onClick={finish}>Пропустить</button></div>
    <div className="lera-person"><img src={leraPortrait} alt="Лера" /><span className="muted">Лера</span></div>
    <div className="lera-speech" key={step}><h1><Typewriter text={title} /></h1>{paragraphs.map((text, index) => <p key={index} className={index ? 'lera-detail' : undefined}><Typewriter text={text} /></p>)}</div>
    <div className="lera-dots" aria-label={`Шаг ${step + 1} из 2`}>{[0, 1].map(n => <i key={n} className={n === step ? 'active' : ''} />)}</div>
    <button className="btn" onClick={() => step ? finish() : setStep(1)}>{step ? 'Выбрать мир →' : 'Вперёд →'}</button>
  </main>
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
export function ChannelHead() { return <GameHeader /> }
export function Home() {
  const nav = useNav(); const p = useStore().player!; const profile = contentOf(p.world); const guidance = characterGuidanceFor(p, worldDocumentOf(p.world))
  return <div className="screen no-tab entry-parity entry-opening"><header><button className="back" aria-label="Назад к мирам" onClick={() => nav.replace('01-3_world')}>‹</button><div><b>{worldOf(p.world).title}</b><small>Завязка истории</small></div></header>
    {worldArt[p.world] && <WorldCover world={p.world} className="entry-opening-cover" priority alt={`Мир «${worldOf(p.world).title}»`} />}
    <div className="entry-identity"><Avatar seed={p.handle} size={42} /><b>Ты — {p.name}</b></div>
    <h1>{guidance?.title || p.role || 'Здесь начинается твоя история.'}</h1><p className="entry-opening-context"><Typewriter text={guidance?.context ?? profile.firstContext} /></p>
    {guidance && <><section className="entry-opening-goal"><strong>Твоя главная цель</strong><h2>{guidance.goal}</h2><p>{guidance.why}</p><h3>Что предстоит развить</h3>{guidance.growth.map(text => <p key={text}>{text}</p>)}</section><section className="entry-opening-goal"><strong>Первый шаг</strong><p>{guidance.firstStep}</p></section></>}
    {p.firstPostInviteBonus > 0 && <p className="small muted">Первый пост даст ещё {p.firstPostInviteBonus} энергии.</p>}
    <button className="btn" onClick={() => nav.push('02-1_compose', { firstStory: true })}>Написать первый пост →</button>
  </div>
}

register({
  '01-1_invite': { component: Invite, title: 'Приглашение от друга' },
  '01-2c_guide': { component: LeraGuide, title: 'Знакомство с Лерой' },
  '01-2_age': { component: Age, title: 'Возраст и согласие' },
  '01-2b_under14': { component: Under14, title: 'Младше 14' },
  '01-3_world': { component: withWorldTemplates(World, true), title: 'Выбор мира' },
  '01-3b_world_overview': { component: withWorldTemplates(WorldOverview), title: 'Обзор мира' },
  '05-7c_world_preview': { component: WorldPrototypePreview, title: 'Демо своего мира' },
  '01-4_channel': { component: withWorldTemplates(Channel), title: 'Канал' },
  '01-4b_world_setup': { component: withWorldTemplates(WorldSetup), title: 'Мир, голос канала и драма' },
  '01-5_first_star': { component: withWorldTemplates(FirstStar), title: 'Настройка героев' },
  '01-6_home': { component: Home, title: 'Первый экран' },
}, { '01-3_world': 5 })
