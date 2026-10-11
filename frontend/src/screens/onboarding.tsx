import { useEffect, useRef, useState } from 'react'
import { api } from '../api/client'
import { ageBand, type AgeBand } from '../app/ageBand'
import { worldOf, worlds } from '../api/mock'
import { contentOf } from '../api/worldContent'
import type { Address, Drama, Player, WorldId } from '../api/types'
import { useNav } from '../app/nav'
import { useStore } from '../app/store'
import { Avatar, BottomCTA, Card, Chip, Dots, Segmented, fmt } from '../ui'
import { track } from '../app/log'
import { pendingReferral, clearReferral } from '../app/referral'
import { register } from '.'

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
const H1 = ({ children }: { children: React.ReactNode }) => <div style={{ fontSize: 26, fontWeight: 800, margin: '6px 0' }}>{children}</div>

// 01-1: сообщение друга в Telegram (вход по карточке)
function Invite() {
  const nav = useNav()
  return (
    <div className="screen no-tab">
      <div className="pad">
        <div className="brand" style={{ fontSize: 28 }}>Реки</div>
        <div className="small muted" style={{ marginTop: 8 }}>Симулятор жизни блогера 🎮</div>
        <H1>Примерь жизнь блогера, которого обсуждают все</H1>
        <p className="muted">Пиши посты, собирай виртуальных фанатов, знакомься со звёздами своего мира и попадай в интриги 👀</p>
        <Card><b>120 подписчиков — начало твоей истории</b><p className="small muted">Впереди — путь к миллиону в выдуманном мире. Каждый пост вызывает реакции звёзд.</p></Card>
        <p className="small muted">Только думаешь о первом посте, уже ведёшь блог или просто хочешь примерить другую жизнь? Здесь можно попробовать любой образ.</p>
        <div className="small muted">Посты остаются в игре — публиковать их в настоящих соцсетях не нужно.</div>
      </div>
      <BottomCTA main="Начать свою историю 🎮" onMain={() => nav.push('01-2_age')} />
    </div>
  )
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

// 01-3: миры со сложностью; закрытые показывают условие
function World() {
  const nav = useNav()
  const [pick, setPick] = useState<WorldId>('stage')
  const w = worlds.find(x => x.id === pick)!
  return (
    <div className="screen no-tab">
      <Steps n={2} />
      <div style={{ padding: '4px 20px' }}>
        <H1>Где начнётся твоя история?</H1>
        <div className="muted" style={{ marginBottom: 12 }}>Выбери жизнь, которую хочется примерить. Сложность ● влияет на риск и скорость роста.</div>
        {worlds.map(x => (
          <div key={x.id} className="card row" onClick={() => !x.lock && setPick(x.id)} style={{ gap: 12, marginBottom: 9, padding: '11px 14px', outline: x.id === pick ? '2px solid var(--accent)' : 'none', opacity: x.lock ? .6 : 1 }}>
            <div style={{ fontSize: 28 }}>{x.emoji}</div>
            <div style={{ flex: 1 }}><b style={{ fontSize: 16 }}>{x.title}</b> <span style={{ marginLeft: 4 }}><Dots n={x.difficulty} /></span>
              <div className="small muted">{x.description}</div><div className="small" style={{ color: 'var(--accent)', marginTop: 2 }}>{x.summary}</div></div>
            {x.lock && <div className="small" style={{ color: 'var(--gift)', textAlign: 'right' }}>🔒 {x.lock.skill === 'humor' ? '😂 юмор' : x.lock.skill === 'brave' ? '🔥 смелость' : '🎯 в тему'} {x.lock.level}</div>}
          </div>
        ))}
        <div className="card row" style={{ gap: 12, padding: '11px 14px', border: '1px dashed #9B6BFF77', background: 'transparent' }}>
          <div style={{ fontSize: 24 }}>✨</div><div className="small muted"><b style={{ color: '#fff' }}>Свой мир</b> — придумай место, роль и героев своей истории. Описание голосом или текстом. Откроется на 10k, когда друг сделает первый пост по твоей ссылке, или с Реки+</div>
        </div>
      </div>
      <BottomCTA main={`В ${worldOf(w.id).acc}`} onMain={() => { track('onboarding_step', { step: 'world', world: pick }); nav.push('01-4_channel', { ...nav.params, world: pick }) }} />
    </div>
  )
}

// 01-4: роли и стиль аватара выбранного мира, название, ник и обращение
function Channel() {
  const nav = useNav()
  const world = (nav.params.world as WorldId) ?? 'stage'
  const profile = contentOf(world)
  const [role, setRole] = useState(0)
  const [name, setName] = useState(profile.name)
  const [handle, setHandle] = useState(profile.handle)
  const [addr, setAddr] = useState<Address>('nick')
  return (
    <div className="screen no-tab">
      <Steps n={3} />
      <div style={{ padding: '4px 20px' }}>
        <div style={{ fontSize: 24, fontWeight: 800, margin: '6px 0' }}>Кем будешь в {profile.where}?</div>
        <div className="muted" style={{ marginBottom: 14 }}>Поменять можно позже</div>
        {profile.roles.map((r, i) => <Chip key={r} on={i === role} onClick={() => setRole(i)}>{r}</Chip>)}
        <div className="label" style={{ marginTop: 12 }}>Название и ник</div>
        <div className="small muted" style={{ marginBottom: 8 }}>Стиль мира: {profile.avatarLabel}. Аватарка собирается из ника</div>
        <div className="card" style={{ marginBottom: 8 }}><input value={name} onChange={e => setName(e.target.value)} maxLength={32} /></div>
        <div className="card" style={{ marginBottom: 4 }}><input value={handle} onChange={e => setHandle(e.target.value)} maxLength={24} /></div>
        <div className="label" style={{ marginTop: 12 }}>Как к тебе обращаться</div>
        {([['she', 'она'], ['he', 'он'], ['nick', 'по нику']] as const).map(([id, t]) => <Chip key={id} on={addr === id} onClick={() => setAddr(id)}>{t}</Chip>)}
        <div className="label" style={{ marginTop: 6 }}>Так тебя увидят фанаты</div>
        <div className="card row" style={{ gap: 12 }}><Avatar seed={handle} size={48} />
          <div><b>{name}</b><div className="muted small">{handle} · 120 подписчиков</div><div className="small" style={{ marginTop: 4 }}>{profile.roles[role]}</div></div></div>
      </div>
      <BottomCTA main="Дальше" disabled={!name.trim() || !handle.trim()} onMain={() => { track('onboarding_step', { step: 'channel', address: addr }); nav.push('01-4b_world_setup', { ...nav.params, role: profile.roles[role], name: name.trim(), handle: handle.trim(), address: addr }) }} />
    </div>
  )
}

function WorldSetup() {
  const nav = useNav()
  const profile = contentOf((nav.params.world as string) ?? 'stage')
  const [voice, setVoice] = useState((nav.params.voice as string) ?? profile.voices[0])
  const [drama, setDrama] = useState<Drama>((nav.params.drama as Drama) ?? profile.drama)
  return <div className="screen no-tab">
    <Steps n={4} />
    <div className="pad">
      <H1>Как устроен мир</H1><div className="muted">{profile.description}</div>
      <Card style={{ margin: '14px 0' }}>{profile.rules.map(rule => <p className="small" key={rule}>✓ {rule}</p>)}</Card>
      <div className="label">Голос канала</div>
      <div className="card" style={{ marginBottom: 10 }}><textarea aria-label="Голос канала" value={voice} onChange={event => setVoice(event.target.value)} maxLength={200} rows={2} /></div>
      {profile.voices.map(value => <Chip key={value} on={value === voice} onClick={() => setVoice(value)}>{value}</Chip>)}
      <Card style={{ marginTop: 10 }}><b>Градус драмы</b><p className="small muted">Выбери тон историй. Безопасность 14+ работает всегда</p>
        <Segmented value={drama} onChange={setDrama} options={[{ id: 'soft', label: 'мягко' }, { id: 'mid', label: 'средне' }, { id: 'hot', label: 'остро' }, { id: 'off', label: 'без хейта' }]} />
      </Card>
    </div>
    <BottomCTA main="Дальше" disabled={!voice.trim()} onMain={() => { track('onboarding_step', { step: 'world_settings', world: (nav.params.world as string) ?? 'stage' }); nav.push('01-5_first_star', { ...nav.params, voice: voice.trim(), drama }) }} />
  </div>
}

// 01-5: первая звезда из трёх
function FirstStar() {
  const nav = useNav()
  const { setPlayer } = useStore()
  const profile = contentOf((nav.params.world as string) ?? 'stage')
  const stars = profile.stars
  const [pick, setPick] = useState(stars[0].id)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const created = useRef<Player | null>(null)
  const s = stars.find(x => x.id === pick)!
  const go = async () => {
    if (saving) return
    setSaving(true); setError('')
    const p = nav.params as Record<string, string>
    try {
      const pl = created.current ?? await api.onboarding({ birthGroup: (p.group as '14-17' | '18+') ?? '14-17', ageBand: p.ageBand as AgeBand | undefined, world: p.world ?? 'stage', role: p.role ?? profile.roles[0], name: p.name ?? profile.name, handle: p.handle ?? profile.handle, address: p.address ?? 'nick', star: pick, referralCode: pendingReferral() })
      created.current = pl
      clearReferral()
      const settings = { voice: p.voice ?? profile.voices[0], drama: (p.drama as Drama) ?? profile.drama }
      await api.settings(settings)
      try { localStorage.setItem(`reki:first-star:${pl.id}`, pick) } catch { /* optional storage */ }
      track('onboarding_step', { step: 'done', star: pick }); setPlayer({ ...pl, settings: { ...pl.settings, ...settings } }); nav.replace('01-6_home')
    } catch { setError('Не получилось сохранить настройки. Попробуй ещё раз.') } finally { setSaving(false) }
  }
  return (
    <div className="screen no-tab">
      <Steps n={5} />
      <div style={{ padding: '4px 20px' }}>
        <div style={{ fontSize: 24, fontWeight: 800, margin: '6px 0' }}>Кто подпишется первым?</div>
        <div className="muted" style={{ marginBottom: 18 }}>Персонажи этого мира вымышленные, у каждого свой характер</div>
        {stars.map(x => (
          <button key={x.id} className="card star-choice" disabled={saving || created.current !== null} onClick={() => setPick(x.id)} style={{ display: 'flex', gap: 12, marginBottom: 12, outline: x.id === pick ? '2px solid var(--accent)' : 'none' }}>
            <Avatar seed={x.id} size={52} />
            <div><b>{x.name}</b> <span className="muted small">{x.role}</span><div className="small" style={{ color: 'var(--accent)', margin: '2px 0' }}>{x.trait}</div><div className="small muted">{x.about}</div></div>
          </button>
        ))}
      </div>
      {error && <div className="pad" role="alert">{error}</div>}
      <BottomCTA main={saving ? 'Сохраняем…' : `${s.name}, подписывайся`} disabled={saving} onMain={go} />
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
  const nav = useNav()
  const { player: p } = useStore()
  const profile = contentOf(p?.world ?? 'stage')
  let selected = profile.stars[0].id
  try { selected = localStorage.getItem(`reki:first-star:${p?.id}`) ?? selected } catch { /* optional storage */ }
  const star = profile.stars.find(value => value.id === selected) ?? profile.stars[0]
  return (
    <div className="screen">
      <ChannelHead />
      <div className="pad">
        <div className="small muted" style={{ marginBottom: 10 }}>Твой блог. Твои знакомства. Твой путь к миллиону.</div>
        <div className="card" style={{ padding: 18, border: '1px solid #FF6B8B66', background: 'linear-gradient(160deg,#2A1A2E,#181821)' }}>
          <div className="row" style={{ marginBottom: 12 }}><Avatar seed={star.id} size={44} /><div><b>{star.name} — первая звезда в твоих подписчиках 👀</b><div className="small muted">{star.role} · подписка на твой блог</div></div></div>
          <p className="small muted">{profile.firstContext}</p>
          <div style={{ fontSize: 20, fontWeight: 800, marginBottom: 16 }}>{profile.firstPrompt}</div>
          {(p?.firstPostInviteBonus ?? 0) > 0 && <div className="gift" style={{ marginBottom: 12 }}>🎁 Первый пост даст ещё {p?.firstPostInviteBonus} ходов. Начни с любой идеи!</div>}
          <button className="btn" onClick={() => nav.push('02-1_compose')}>Написать первый пост</button>
          <div style={{ textAlign: 'center', marginTop: 10, fontSize: 14, color: 'var(--lilac)' }}>Не знаешь, что написать? Начни с подсказки ✨</div>
        </div>
        <div className="card small" style={{ marginTop: 12, color: 'var(--soft)' }}>
          Пиши посты → получай реакции → влияй на свою историю
        </div>
      </div>
    </div>
  )
}

register({
  '01-1_invite': { component: Invite, title: 'Приглашение от друга' },
  '01-2_age': { component: Age, title: 'Год рождения и согласие' },
  '01-2b_under14': { component: Under14, title: 'Младше 14' },
  '01-3_world': { component: World, title: 'Выбор мира' },
  '01-4_channel': { component: Channel, title: 'Канал' },
  '01-4b_world_setup': { component: WorldSetup, title: 'Мир, голос канала и драма' },
  '01-5_first_star': { component: FirstStar, title: 'Первая звезда' },
  '01-6_home': { component: Home, title: 'Первый экран' },
}, { '01-6_home': 0 })
