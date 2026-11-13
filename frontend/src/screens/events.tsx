import { worldOf } from '../api/worldContent'
import { useNav } from '../app/nav'
import { usePlayer } from '../app/store'
import { Avatar, BottomCTA, Button, Card, Chip, Gift, SkillBar, fmt } from '../ui'
import { ChannelHead } from './onboarding'
import { TELEGRAM_BOT_USERNAME } from '../app/telegramLinks'
import { register } from '.'
import { ChooseWorld as NewWorld } from './worldWizard'
import { withWorldTemplates } from '../app/withWorldTemplates'

function TgBot({ time, children }: { time: string; children: React.ReactNode }) {
  return (
    <div className="tg screen no-tab">
      <div className="top" style={{ background: '#0E1621', borderColor: '#1A2533' }}><span style={{ fontSize: 22, color: '#8CC8FF' }}>‹</span><Avatar label="R" size={36} /><div><b>Реки</b><div className="small" style={{ color: '#6C8EA8' }}>бот</div></div></div>
      <div style={{ padding: 16 }}><div className="small" style={{ textAlign: 'center', color: '#6C8EA8', marginBottom: 10 }}>сегодня</div>{children}<div className="small" style={{ color: '#6C8EA8', marginTop: 8 }}>{time}</div></div>
    </div>
  )
}


const EveningPush = () => <TgBot time="18:40">
  <div className="tgb">🌩 <b>Про тебя пишут.</b> Фансайт BLESK считает, что ты «пиаришься на Мире». Уже 200 комментариев</div><button className="tgbtn">Ответить фансайту</button>
  <div className="small" style={{ color: '#6C8EA8', marginTop: 24, textAlign: 'center' }}>Не больше 3 сообщений в день. С 22:00 до 9:00 тишина</div>
</TgBot>

const Morning = () => <TgBot time="09:00">
  <div className="tgb">☀️ <b>Мира ответила</b> про коллаб: «ладно, снимаем. но если упадёшь, это в тизер» 😏</div>
  <div className="tgb">✍️ 10 новых постов · 🔥 серия 3 дня: +2 поста</div><button className="tgbtn">Открыть Реки</button>
  <div className="small" style={{ color: '#6C8EA8', marginTop: 24, textAlign: 'center' }}>Пропустишь день: серия встанет на паузу, но не сгорит</div>
</TgBot>

function NewStar() {
  const nav = useNav()
  return (
    <div className="screen">
      <ChannelHead />
      <div style={{ padding: '20px 18px' }}>
        <div style={{ textAlign: 'center', margin: '6px 0 16px' }}><span className="badge">ДЕНЬ 3 · НОВАЯ ЗВЕЗДА</span>
          <Avatar seed="yuna" mode="default" size={86} style={{ margin: '14px auto 8px' }} /><div style={{ fontSize: 22, fontWeight: 800 }}>Юна</div><div className="muted">трейни-соперница · из того же агентства</div></div>
        <div className="small muted" style={{ textAlign: 'center', marginBottom: 12 }}>Подписалась на тебя и уже оставила комментарий</div>
        <Card style={{ marginBottom: 12 }}><div className="row"><Avatar seed="yuna" mode="default" size={32} /><b>Юна</b><span className="small muted">трейни</span><span className="small" style={{ color: 'var(--accent)' }}>★ звезда</span></div><div style={{ marginTop: 6 }}>мило. на прослушивании посмотрим, кто уверенно, а кто нет 🙂</div></Card>
        <div className="small muted" style={{ textAlign: 'center' }}>Соперница: с ней можно враждовать или подружиться</div>
      </div>
      <div className="bottom-cta" style={{ bottom: 78 }}><Button onClick={() => nav.push('03-1_dm')}>Ответить Юне</Button></div>
    </div>
  )
}

/** «Пока тебя не было»: после пропуска сводка и одно событие-пост без лимита. */
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
          <Card key={n} style={{ marginBottom: 8 }}><div className="row"><Avatar seed={a} mode="default" size={32} /><div className="small"><b>{n}</b> {t}</div></div></Card>
        ))}
        <Card style={{ margin: '12px 0', border: '1px solid #6BFFB866' }}><span className="badge" style={{ color: '#6BFFB8' }}>СОБЫТИЕ · БЕЗ ЛИМИТА</span><div style={{ fontWeight: 800, margin: '6px 0 2px' }}>Вернись громко</div><div className="small muted">Один пост про твоё возвращение не тратит лимит</div></Card>
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
      <Avatar seed={p.handle} size={96} style={{ margin: '22px auto 8px' }} /><div style={{ fontSize: 22, fontWeight: 800 }}>{p.name}</div>
      <div style={{ fontSize: 40, fontWeight: 900, background: 'linear-gradient(90deg,#FF6B8B,#FFB86B)', WebkitBackgroundClip: 'text', color: 'transparent', marginTop: 8 }}>{fmt(p.subs)}</div><div className="muted">подписчиков за 4 дня</div>
      <Card style={{ marginTop: 18, textAlign: 'left' }}>
        <SkillBar name="смелость" progress={.72} /><SkillBar name="юмор" progress={.64} /><SkillBar name="в тему" progress={.58} />
        <div className="small muted" style={{ marginTop: 6 }}>типаж: {p.type}</div>
        <div className="small muted" style={{ marginTop: 6 }}>ПЕРЕЖИТО</div><div>🌩 хейт-волна фансайта</div>
      </Card>
      <div className="muted small" style={{ marginTop: 26 }}>а ты кем будешь в Сцене? · t.me/{TELEGRAM_BOT_USERNAME}</div>
    </div>
  )
}


register({
  '05-1_evening_push': { component: EveningPush, title: 'Бот: вечерний тизер' },
  '05-3_morning': { component: Morning, title: 'Бот: утро' },
  '05-3b_while_away': { component: WhileAway, title: 'Пока тебя не было' },
  '05-4_new_star': { component: NewStar, title: 'Новая звезда' },
  '05-6_stats_card': { component: StatsCard, title: 'Карточка статов' },
  '05-7_new_world': { component: withWorldTemplates(NewWorld), title: 'Новая линия мира' },
}, { '05-4_new_star': 0 })
