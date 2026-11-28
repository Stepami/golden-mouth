import { useGuestSession } from '../app/landing/guestSession'
import { acceptedRegistrationConsent, readGuestHandoff } from '../app/landing/guestHandoff'
import { landingContext, pendingLandingWorld } from '../app/landing/experiment'
import { Resources } from '../ui/Resources'
import { DailyTasks } from '../ui/DailyTasks'
import { Streak } from '../ui/Streak'
import { displayHandle } from '../app/handle'
import { usePresetGoal } from '../ui/StoryGoal'
import { WorldContext } from '../ui/WorldContext'
import { WorldCover } from '../ui/WorldCover'
import approvedIntro from '../assets/intro-school-approved.png'
import { useEffect, useRef, useState } from 'react'
import { withWorldTemplates } from '../app/withWorldTemplates'
import { ageBand } from '../app/ageBand'
import { getWorldDraft, setWorldDraft } from '../app/worldDraft'
import { worldOf, contentOf, optionalWorldTemplate } from '../api/worldContent'
import type { Player } from '../api/types'
import { needsRegistration } from '../api/auth'
import { useNav } from '../app/nav'
import { useStore } from '../app/store'
import { Avatar, BottomCTA, Button, Card, EnergyBadge, fmt } from '../ui'
import { track } from '../app/log'
import { tap, trackLandingAuth } from '../app/analytics'
import { register } from '.'
import { worldArt } from '../app/worldPrototype'
import { NextStoryChannelHint, NextStoryChannelOffer } from '../app/NextStory'
import { useNextStory } from '../app/GameOnboardingState'
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

/** Exact portrait windows from the approved sketch; no regenerated character artwork. */
function IntroPortrait({ character }: { character: 'masha' | 'gosha' }) {
  const masha = character === 'masha'
  return <svg className={`intro-portrait intro-portrait-${character}`} viewBox={masha ? '85 626 116 116' : '142 931 168 168'} role="img" aria-label={masha ? 'Гермиона' : 'Гарри в круглых очках с волшебной палочкой'}>
    <image href={approvedIntro} width="1036" height="1518" />
  </svg>
}

// 01-1: approved school mini-dialogue, preserving the existing welcome layout.
function Invite() {
  const nav = useNav()
  return <div className="screen no-tab welcome"><div className="pad">
    <div className="brand">реки</div><h1>Твой пост<br />запускает историю.</h1>
    <p className="muted">Веди блог в вымышленном мире.<br />Пиши посты — персонажи ответят.</p>
    <div className="welcome-post card" aria-label="Пример игрового поста">
      <div className="row"><IntroPortrait character="masha" /><div><b>Гермиона · Спасти Хогвартс</b><div className="small muted">Пример игрового поста</div></div><span className="small muted" style={{ marginLeft: 'auto' }}>10k читателей</span></div>
      <p>Хогвартс закрыт. Совет выбирает между школой и отелем. Какую идею вы готовы поддержать?</p>
      <div className="row"><span className="react">🔥 2,4k</span><span className="react">💀 890</span><span className="react">🫶 1,2k</span></div>
      <div className="welcome-reply"><IntroPortrait character="gosha" /><div><b>Гарри · выпускник Хогвартса</b><div>Хочу вернуть школу. Готов публично поддержать проект, но за остальных обещать не буду.</div></div></div>
    </div>
    <p className="small muted welcome-disclaimer">Персонажи и фанаты виртуальные.<br />Посты остаются внутри игры.</p>
    <p className="welcome-world-choice">Выбери мир или создай свой</p>
  </div><BottomCTA main="Начать играть" onMain={() => nav.push('01-2_age', { fromLanding: !!landingContext() })} /></div>
}

// 01-2: new accounts are 18+; returning profiles keep their existing age band.
function Age() {
  const nav = useNav()
  const currentYear = new Date().getFullYear()
  const years = Array.from({ length: 91 }, (_, i) => currentYear - 10 - i)
  const initialYear = typeof nav.params.year === 'number' ? nav.params.year : currentYear - 18
  const [year, setYear] = useState(initialYear)
  const preaccepted = acceptedRegistrationConsent()
  const [agree, setAgree] = useState(nav.params.agree === true || preaccepted)
  const wheel = useRef<HTMLDivElement>(null)
  const viewed = useRef(false)
  const fromLanding = nav.params.fromLanding === true
  useEffect(() => {
    if (fromLanding && !viewed.current) {
      viewed.current = true
      trackLandingAuth('landing_age_view')
    }
  }, [fromLanding])
  useEffect(() => { if (wheel.current) wheel.current.scrollTop = years.indexOf(initialYear) * 38 }, [])
  const onWheel = () => { const el = wheel.current; if (el) setYear(years[Math.min(years.length - 1, Math.max(0, Math.round(el.scrollTop / 38)))]) }
  const age = currentYear - year
  const proceed = () => {
    track('onboarding_step', { step: 'age', ok: age >= 18 })
    if (age < 18) {
      nav.replace('01-2b_under14', { year, agree, fromLanding, returnToCreation: nav.params.returnToCreation })
      return
    }
    if (fromLanding) trackLandingAuth('landing_age_continue')
    const draft = getWorldDraft()
    if (draft && !draft.editingId) setWorldDraft({ ...draft, birthYear: year, group: '18+', ageBand: ageBand(age) })
    if (nav.params.returnToCreation) { nav.back(); return }
    const world = pendingLandingWorld()
    const handoff = readGuestHandoff()
    if (preaccepted && needsRegistration() && world && handoff?.worldId === world && handoff.dialogue) {
      nav.replace('guest_import', { birthYear: year })
      return
    }
    nav.replace('01-2_age', { year, agree, fromLanding })
    nav.push(world ? '01-3b_world_overview' : '01-3_world', {
      ...(world ? { world, fromLanding: true } : {}), group: '18+', ageBand: ageBand(age), birthYear: year,
    })
  }
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
        {!preaccepted && <Card style={{ marginTop: 18 }}>
          <b>Сохраним твою игру?</b>
          <label className="row" style={{ gap: 8, marginTop: 10 }}>
            <button onClick={() => { if (fromLanding) trackLandingAuth('landing_age_consent', { auth_outcome: agree ? 'unchecked' : 'checked' }); setAgree(!agree) }} aria-pressed={agree} style={{ width: 22, height: 22, borderRadius: 6, border: agree ? 'none' : '2px solid #55536A', background: agree ? 'var(--grad)' : 'none', color: '#fff', fontSize: 14 }}>{agree ? '✓' : ''}</button>
            <span className="small">Согласие на обработку данных</span>
          </label>
        </Card>}
        {!agree && <div className="small muted" style={{ textAlign: 'center', marginTop: 10 }}>Отметь согласие, чтобы продолжить</div>}
      </div>
      <BottomCTA main="Дальше" disabled={!agree} onMain={proceed} />
    </div>
  )
}

function Under18() {
  const nav = useNav()
  return (
    <div className="screen no-tab" style={{ padding: '60px 26px', textAlign: 'center' }}>
      <div className="brand" style={{ fontSize: 26 }}>Реки</div>
      <div style={{ fontSize: 64, margin: '40px 0 14px' }}>🌱</div>
      <div style={{ fontSize: 24, fontWeight: 800, marginBottom: 10 }}>Игра доступна с 18 лет</div>
      <div className="muted">Играть в Реки можно с 18 лет. Если год рождения выбран по ошибке, его можно исправить.</div>
      <BottomCTA main="Исправить возраст" onMain={() => nav.replace('01-2_age', { year: nav.params.year, agree: nav.params.agree, fromLanding: nav.params.fromLanding, returnToCreation: nav.params.returnToCreation })} />
    </div>
  )
}

// 01-6: первый экран: цель, квест, что такое Реки / навыки / коллекция
export function ChannelHead({ hideDaily = false }: { hideDaily?: boolean } = {}) {
  const guest = useGuestSession(); const profile = useStore()?.player; const nav = useNav()
  const p = guest?.player ?? profile!
  const w = guest?.world ?? worldOf(p.world)
  const identity = <><Avatar seed={p.handle} avatarStyle={guest?.world.avatarStyle} size={36} />
    <div className="channel-header-copy"><b>{p.name}</b>{p.handle && <div className="muted small" style={{ overflowWrap: 'anywhere' }}>{displayHandle(p.handle)}</div>}<div className="muted small">{fmt(p.subs)} подписчиков · {w.emoji} {w.title}</div></div></>
  return <>
    <div className="top channel-header">
      {guest ? <div className="channel-author">{identity}</div> : <button className="channel-author" onClick={() => nav.tab(4)}>{identity}</button>}
      {guest ? <EnergyBadge value={p.postsLeft} /> : <Resources player={profile!} />}
    </div>
    {!guest && !hideDaily && nav.current !== '02-2_live' && <DailyTasks inline={nav.current === '03-5_stage'} />}
  </>
}

export function Home({ firstPostConfirmed = false }: { firstPostConfirmed?: boolean } = {}) {
  const guest = useGuestSession()
  return guest ? <HomeView player={guest.player} title={guest.world.title} context={guest.world.firstContext} goal={guest.goal} firstPostConfirmed guest /> : <LiveHome firstPostConfirmed={firstPostConfirmed} />
}

function LiveHome({ firstPostConfirmed }: { firstPostConfirmed: boolean }) {
  const p = useStore().player!; const profile = contentOf(p.world); const goal = usePresetGoal(p.world)
  const world = worldOf(p.world)
  const custom = !optionalWorldTemplate(world)
  const worldId = p.worldInstanceId ?? p.world
  const story = useNextStory()
  const startingNextStory = story?.phase === 'post' && story.worldId === worldId
  const starter = startingNextStory ? profile.ideas.find(idea => idea.trim()) : undefined
  const prompt = custom || startingNextStory && !starter ? profile.firstPrompt.trim() : undefined
  const context = custom ? profile.firstContext.trim() || profile.description.trim() : profile.firstContext
  return <HomeView player={p} title={world.title} context={context} goal={goal} firstPostConfirmed={firstPostConfirmed || startingNextStory} custom={custom} starter={starter} prompt={prompt} offer={<NextStoryChannelOffer player={p} />} hint={<NextStoryChannelHint player={p} />} />
}

function HomeView({ player: p, title, context, goal, firstPostConfirmed, guest = false, custom = false, starter, prompt, offer, hint }: {
  player: Pick<Player, 'world' | 'streak'>; title: string; context: string; goal: ReturnType<typeof usePresetGoal>; firstPostConfirmed: boolean; guest?: boolean; custom?: boolean; starter?: string; prompt?: string; offer?: React.ReactNode; hint?: React.ReactNode
}) {
  const nav = useNav()
  const session = useGuestSession()
  const suggested = starter || prompt
  if (session?.published) return <div className="screen channel-screen game-columns"><ChannelHead /><WorldContext world={p.world} /><div className="pad">
    <Card><p>{session.post.text}</p></Card>
  </div><BottomCTA main="Посмотреть реакции" onMain={() => nav.push('02-6_thread', { focusReply: true })} /></div>
  const openEditor = () => { if (!guest) tap('first_post_open_editor'); nav.push('02-1_compose', { firstPostConfirmed }) }
  if (custom) return <div className="screen first-story-screen game-columns">
    <ChannelHead hideDaily />
    <div className="pad">
      <section className="card first-story own-world-kickoff">
        <h1>Завязка</h1>
        {context && <p className="first-story-context">{context}</p>}
        {prompt && <p className="muted">{prompt}</p>}
        <Button onClick={openEditor}>Написать первый пост</Button>
      </section>
    </div>
    <WorldContext world={p.world}><DailyTasks inline={nav.current === '03-5_stage'} /></WorldContext>
    <div className="pad"><Streak streak={p.streak} />{offer}</div>
  </div>
  return <div className="screen first-story-screen game-columns"><ChannelHead hideDaily /><WorldContext world={p.world}>{!guest && <DailyTasks inline={nav.current === '03-5_stage'} />}</WorldContext><div className="pad">
    {!suggested && <Streak streak={p.streak} />}
    {offer}
    <div className="card first-story">
      {worldArt[p.world] && <WorldCover world={p.world} className="first-story-cover" priority alt={`Мир «${title}»`} />}
      {goal && <div className="first-story-goal"><div className="small eyebrow">Твой путь</div><h1>{goal.goal}</h1></div>}
      {suggested && <div className="story-first-step next-story-idea"><b className="small">{starter ? "Идея для поста" : "Первый шаг"}</b><p>{suggested}</p></div>}
      <div className="small eyebrow">Сегодня всё начинается</div><p className="first-story-context">{context}</p>
      {!suggested && goal && <div className="story-first-step"><b className="small">Первый шаг</b><p>{goal.firstStep}</p></div>}
    </div>{!suggested && <p className="small muted first-story-footer">Можно начать своими словами — это твоя история.</p>}
  </div><div className="bottom-cta" style={guest ? undefined : { bottom: 78 }}>{session?.hint ?? hint}<button className="btn" onClick={openEditor}>Написать первый пост</button></div></div>
}

register({
  '01-1_invite': { component: Invite, title: 'Приглашение от друга' },
  '01-2_age': { component: Age, title: 'Год рождения и согласие' },
  '01-2b_under14': { component: Under18, title: 'Младше 18' },
  '01-3_world': { component: withWorldTemplates(World, true), title: 'Выбор мира' },
  '01-3b_world_overview': { component: withWorldTemplates(WorldOverview), title: 'Обзор мира' },
  '05-7c_world_preview': { component: WorldPrototypePreview, title: 'Демо своего мира' },
  '01-4_channel': { component: withWorldTemplates(Channel), title: 'Канал' },
  '01-4b_world_setup': { component: withWorldTemplates(WorldSetup), title: 'Мир, голос канала и драма' },
  '01-5_first_star': { component: withWorldTemplates(FirstStar), title: 'Настройка героев' },
  '01-6_home': { component: Home, title: 'Первый экран' },
}, { '01-6_home': 0, '01-3_world': 5 })
