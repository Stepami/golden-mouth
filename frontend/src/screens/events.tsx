import { useState } from 'react'
import { api } from '../api/client'
import { events, worldOf } from '../api/mock'
import type { GameEvent } from '../api/types'
import { useNav } from '../app/nav'
import { useApi } from '../app/useApi'
import { usePlayer } from '../app/store'
import { Avatar, BottomCTA, Button, Card, Chip, Gift, SkillBar, fmt } from '../ui'
import { ChannelHead } from './onboarding'
import { track } from '../app/log'
import { register } from '.'

function TgBot({ time, children }: { time: string; children: React.ReactNode }) {
  return (
    <div className="tg screen no-tab">
      <div className="top" style={{ background: '#0E1621', borderColor: '#1A2533' }}><span style={{ fontSize: 22, color: '#8CC8FF' }}>‹</span><Avatar id="R" size={36} /><div><b>Реки</b><div className="small" style={{ color: '#6C8EA8' }}>бот</div></div></div>
      <div style={{ padding: 16 }}><div className="small" style={{ textAlign: 'center', color: '#6C8EA8', marginBottom: 10 }}>сегодня</div>{children}<div className="small" style={{ color: '#6C8EA8', marginTop: 8 }}>{time}</div></div>
    </div>
  )
}

/** Событие дня: это пост в канале, лимит не тратит; идеи или «своими словами ×1,3». */
function EventCard({ e, border }: { e: GameEvent; border: string }) {
  const nav = useNav()
  const [text, setText] = useState('')
  const [pick, setPick] = useState<string | null>(null)
  const go = () => { track('event_answered', { kind: e.kind, own: !!text.trim() }); api.answerEvent(e.id, text || pick || ''); nav.replace(e.kind === 'hate' ? '05-2b_hate_result' : '03-5_stage', { answer: text || pick }) }
  return (
    <div className="screen">
      <ChannelHead />
      <div className="pad">
        <Card style={{ border: `1px solid ${border}` }}>
          <span className="badge" style={e.kind === 'collab' ? { color: '#6BFFB8' } : undefined}>{e.badge}</span>
          <div style={{ fontSize: 20, fontWeight: 800, margin: '8px 0 4px' }}>{e.title}</div>
          <div className="small muted" style={{ marginBottom: 10 }}>{e.text}</div>
          <div className="small" style={{ color: 'var(--lilac)', marginBottom: 8 }}>{e.note}</div>
          {e.ideas.map(i => <Chip key={i} idea on={pick === i} onClick={() => { setPick(i); setText('') }}>{i}</Chip>)}
          <div className="card" style={{ background: '#20202B', marginTop: 4 }}><input value={text} onChange={x => { setText(x.target.value); setPick(null) }} placeholder={e.kind === 'hate' ? 'Ответить своими словами · ×1,3' : 'Написать пост своими словами · ×1,3'} maxLength={500} /></div>
          <div className="small muted" style={{ marginTop: 10 }}>{e.reward}</div>
        </Card>
      </div>
      {(pick || text.trim()) && <div className="bottom-cta" style={{ bottom: 78 }}><Button onClick={go}>Опубликовать</Button></div>}
    </div>
  )
}
const Collab = () => { const e = useApi(api.eventToday, events.collab); return <EventCard e={e} border="#6BFFB866" /> }
const Hate = () => <EventCard e={events.hate} border="#FF6B8B66" />

const EveningPush = () => <TgBot time="18:40">
  <div className="tgb">🌩 <b>Про тебя пишут.</b> Фансайт BLESK считает, что ты «пиаришься на Мире». Уже 200 комментариев</div><button className="tgbtn">Ответить фансайту</button>
  <div className="small" style={{ color: '#6C8EA8', marginTop: 24, textAlign: 'center' }}>Не больше 3 сообщений в день. С 22:00 до 9:00 тишина</div>
</TgBot>

const Morning = () => <TgBot time="09:00">
  <div className="tgb">☀️ <b>Мира ответила</b> про коллаб: «ладно, снимаем. но если упадёшь, это в тизер» 😏</div>
  <div className="tgb">✍️ 10 новых постов · 🔥 серия 3 дня: +2 поста</div><button className="tgbtn">Открыть Реки</button>
  <div className="small" style={{ color: '#6C8EA8', marginTop: 24, textAlign: 'center' }}>Пропустишь день: серия встанет на паузу, но не сгорит</div>
</TgBot>

function HateResult() {
  const nav = useNav()
  return (
    <div className="screen no-tab">
      <ChannelHead />
      <div style={{ padding: '20px 18px', textAlign: 'center' }}>
        <div style={{ fontSize: 56, marginTop: 6 }}>🌤</div>
        <div style={{ fontSize: 26, fontWeight: 900, margin: '6px 0' }}>Волна прошла</div>
        <div className="muted" style={{ marginBottom: 16 }}>Твой ответ: «{(nav.params.answer as string) ?? 'я тут ради сцены, а не ради Миры'}». Фансайт удалил пост, фанаты за тебя</div>
        <div style={{ display: 'flex', gap: 10, marginBottom: 14 }}>
          <Card style={{ flex: 1 }}><div style={{ fontSize: 24, fontWeight: 800 }} className="up">+410</div><div className="small muted">подписчиков</div></Card>
          <Card style={{ flex: 1 }}><div style={{ fontSize: 24, fontWeight: 800 }} className="up">+12</div><div className="small muted">опыта смелости</div></Card>
        </div>
        <Card style={{ textAlign: 'left', border: '1px solid #FFB86B66', marginBottom: 12 }}><span className="badge">НОВАЯ КАРТОЧКА</span><div style={{ marginTop: 6 }}><b>Хейт пережит</b><div className="small muted">4/12 в коллекции</div></div></Card>
        <Gift>Покажи друзьям, как держать удар: друг зайдёт = +5 постов вам обоим</Gift>
      </div>
      <BottomCTA second="Дальше" onSecond={() => nav.replace('03-5_stage')} main="Поделиться" onMain={() => nav.push('02-7_share')} />
    </div>
  )
}

function NewStar() {
  const nav = useNav()
  return (
    <div className="screen">
      <ChannelHead />
      <div style={{ padding: '20px 18px' }}>
        <div style={{ textAlign: 'center', margin: '6px 0 16px' }}><span className="badge">ДЕНЬ 3 · НОВАЯ ЗВЕЗДА</span>
          <Avatar id="yuna" size={86} style={{ margin: '14px auto 8px' }} /><div style={{ fontSize: 22, fontWeight: 800 }}>Юна</div><div className="muted">трейни-соперница · из того же агентства</div></div>
        <div className="small muted" style={{ textAlign: 'center', marginBottom: 12 }}>Подписалась на тебя и уже оставила комментарий</div>
        <Card style={{ marginBottom: 12 }}><div className="row"><Avatar id="yuna" size={32} /><b>Юна</b><span className="small muted">трейни</span><span className="small" style={{ color: 'var(--accent)' }}>★ звезда</span></div><div style={{ marginTop: 6 }}>мило. на прослушивании посмотрим, кто уверенно, а кто нет 🙂</div></Card>
        <div className="small muted" style={{ textAlign: 'center' }}>Соперница: с ней можно враждовать или подружиться</div>
      </div>
      <div className="bottom-cta" style={{ bottom: 78 }}><Button onClick={() => nav.push('03-1_dm')}>Ответить Юне</Button></div>
    </div>
  )
}

/** «Пока тебя не было»: после пропуска сводка и одно событие-пост без лимита. «Канал отдохнул» только после 2+ дней. */
function WhileAway() {
  const nav = useNav(); const p = usePlayer()
  const days = (nav.params.days as number) ?? 2
  return (
    <div className="screen no-tab">
      <ChannelHead />
      <div className="pad">
        <div style={{ fontSize: 24, fontWeight: 800, margin: '6px 0 4px' }}>Пока тебя не было</div>
        <div className="muted" style={{ marginBottom: 12 }}>{days === 1 ? 'Вчера' : `${days} дня`} в Сцене было без тебя. Вот что случилось</div>
        {[['mira', 'Мира', 'выложила тизер камбэка и спросила, где ты 👀'], ['yuna', 'Юна', 'прошла прослушивание и намекнула на тебя в посте'], ['sonya', 'Соня', 'ответила на твой комментарий 💛']].map(([a, n, t]) => (
          <Card key={n} style={{ marginBottom: 8 }}><div className="row"><Avatar id={a} size={32} /><div className="small"><b>{n}</b> {t}</div></div></Card>
        ))}
        <Card style={{ margin: '12px 0', border: '1px solid #6BFFB866' }}><span className="badge" style={{ color: '#6BFFB8' }}>СОБЫТИЕ · БЕЗ ЛИМИТА</span><div style={{ fontWeight: 800, margin: '6px 0 2px' }}>Вернись громко</div><div className="small muted">Один пост про твоё возвращение не тратит лимит</div></Card>
        {days >= 2 && <Gift>Канал отдохнул: первые 3 поста сегодня дают ×2</Gift>}
        <div className="small muted" style={{ textAlign: 'center', marginTop: 10 }}>Серия {p.streak.days} дня на паузе, не сгорела</div>
      </div>
      <BottomCTA main="Написать пост" onMain={() => nav.replace('02-1_compose')} />
    </div>
  )
}

function StatsCard() {
  const p = usePlayer()
  return (
    <div className="screen no-tab" style={{ background: 'linear-gradient(160deg,#2A1236,#0E0E13 55%,#12203A)', padding: '50px 26px', textAlign: 'center' }}>
      <div className="brand" style={{ fontSize: 22 }}>Реки</div><div className="small" style={{ color: 'var(--soft)', letterSpacing: '.08em', marginTop: 4 }}>МОИ СТАТЫ · {worldOf(p.world).emoji} {worldOf(p.world).title.toUpperCase()}</div>
      <Avatar id={p.avatar} size={96} style={{ margin: '22px auto 8px' }} /><div style={{ fontSize: 22, fontWeight: 800 }}>{p.name}</div>
      <div style={{ fontSize: 40, fontWeight: 900, background: 'linear-gradient(90deg,#FF6B8B,#FFB86B)', WebkitBackgroundClip: 'text', color: 'transparent', marginTop: 8 }}>{fmt(6210)}</div><div className="muted">подписчиков за 4 дня</div>
      <Card style={{ marginTop: 18, textAlign: 'left' }}>
        <SkillBar name="смелость" progress={.72} /><SkillBar name="юмор" progress={.64} /><SkillBar name="в тему" progress={.58} />
        <div className="small muted" style={{ marginTop: 6 }}>типаж: {p.type}</div>
        <div className="small muted" style={{ marginTop: 10 }}>ЛУЧШАЯ ЗВЕЗДА</div><div>👑 Мира · отношения +4</div>
        <div className="small muted" style={{ marginTop: 6 }}>ПЕРЕЖИТО</div><div>🌩 хейт-волна фансайта</div>
      </Card>
      <div className="muted small" style={{ marginTop: 26 }}>а ты кем будешь в Сцене? · t.me/reki_game</div>
    </div>
  )
}

function NewWorld() {
  const nav = useNav()
  return (
    <div className="screen no-tab" style={{ background: 'radial-gradient(circle at 50% 35%,#24344A,#0E0E13 70%)' }}>
      <div style={{ padding: '60px 26px', textAlign: 'center' }}>
        <span className="badge">10 000 ПОДПИСЧИКОВ</span><div style={{ fontSize: 70, margin: '20px 0 8px' }}>🌸</div>
        <div style={{ fontSize: 28, fontWeight: 900 }}>Открыта новая линия: Академия</div><div className="muted" style={{ margin: '8px 0 22px' }}>Аниме-школа: клубы, соперницы, фестиваль. Новые звёзды и события</div>
        <Card style={{ textAlign: 'left', marginBottom: 10 }}>🎤 Канал тот же: все 10 000 подписчиков с тобой, Сцена никуда не делась</Card>
        <Card style={{ textAlign: 'left', marginBottom: 10 }}>🌸 Новые звёзды и события Академии приходят в твою ленту рядом со Сценой</Card>
        <Card style={{ textAlign: 'left' }}>🃏 В коллекции 12 новых слотов</Card>
      </div>
      <BottomCTA second="Позже" onSecond={() => nav.replace('03-5_stage')} main="Заглянуть в Академию" onMain={() => nav.replace('03-3_feed')} />
    </div>
  )
}

register({
  '05-0_event_collab': { component: Collab, title: 'Событие: коллаб' },
  '05-1_evening_push': { component: EveningPush, title: 'Бот: вечерний тизер' },
  '05-2_event': { component: Hate, title: 'Событие: хейт-волна' },
  '05-2b_hate_result': { component: HateResult, title: 'Итог хейт-волны' },
  '05-3_morning': { component: Morning, title: 'Бот: утро' },
  '05-3b_while_away': { component: WhileAway, title: 'Пока тебя не было' },
  '05-4_new_star': { component: NewStar, title: 'Новая звезда' },
  '05-6_stats_card': { component: StatsCard, title: 'Карточка статов' },
  '05-7_new_world': { component: NewWorld, title: 'Новая линия мира' },
}, { '05-0_event_collab': 0, '05-2_event': 0, '05-4_new_star': 0 })
