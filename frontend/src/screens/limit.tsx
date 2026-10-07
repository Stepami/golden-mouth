import { useState } from 'react'
import { copyTextToClipboard, shareURL } from '@telegram-apps/sdk-react'
import { api } from '../api/client'
import { worldOf } from '../api/mock'
import { useNav } from '../app/nav'
import { usePlayer, useStore } from '../app/store'
import { inTelegram } from '../telegram'
import { Avatar, BottomCTA, Button, Card, Gift, Toggle, TopBar, fmt } from '../ui'
import { ChannelHead } from './onboarding'
import { track } from '../app/log'
import { TELEGRAM_BOT_USERNAME, telegramStartUrl } from '../app/telegramLinks'
import { register } from '.'

const LINK = telegramStartUrl('liza7')
const shareInvite = () => {
  track('invite_sent')
  const text = 'Играю в Реки: веду канал, а AI-звёзды реагируют на каждый пост. Заходи, +5 постов нам обоим'
  if (inTelegram) shareURL.ifAvailable(LINK, text); else window.open(`https://t.me/share/url?url=${encodeURIComponent(LINK)}&text=${encodeURIComponent(text)}`)
}

// 04-1: «звёзды отдыхают»; звёздам не пишем, бесплатное остаётся
function Limit() {
  const nav = useNav()
  return (
    <div className="screen no-tab">
      <ChannelHead />
      <div style={{ padding: '14px 20px', textAlign: 'center' }}>
        <Avatar seed="mira" size={72} style={{ margin: '2px auto 10px' }} />
        <div style={{ fontSize: 22, fontWeight: 800, marginBottom: 6 }}>Мира ответит утром</div>
        <div className="muted" style={{ marginBottom: 14 }}>Она сказала «надо подумать 👀». Посты на сегодня всё, звёзды отдыхают. В 9:00 будут новые</div>
        <Card style={{ textAlign: 'left', marginBottom: 12 }}><div className="small" style={{ color: 'var(--soft)', marginBottom: 4 }}>А пока бесплатно</div>отвечать на комментарии к своим постам · комментировать друзей · листать ленту</Card>
        <Gift style={{ textAlign: 'left', fontSize: 15, padding: '12px 14px' }}>Позови друга: +5 постов вам обоим прямо сегодня<div style={{ fontWeight: 600, fontSize: 12, marginTop: 2 }}>как только он сделает первый пост · до 3 друзей в день</div></Gift>
        <button onClick={() => nav.push('04-2_fake_door')} style={{ marginTop: 14, fontSize: 14, color: 'var(--muted2)', background: 'none', border: 'none' }}>Реки+: +10 постов и все миры ›</button>
      </div>
      <BottomCTA main="Позвать друга" onMain={() => nav.push('04-3_invite')} />
    </div>
  )
}

// 04-2: Реки+ тарифом; платёжки нет, «Хочу» пишет интерес и даёт +5 постов
function FakeDoor() {
  const nav = useNav(); const p = usePlayer(); const { patch } = useStore()
  const [sent, setSent] = useState(false)
  const take = async (interest: boolean) => { track(interest ? 'plus_interest' : 'plus_bonus_only'); if (interest) await api.plusInterest(); patch({ postsLeft: p.postsLeft + 5 }); setSent(true); setTimeout(() => nav.back(), 900) }
  const plan = (name: string, price: string, rows: [string, boolean][], hi?: boolean) => (
    <div className="card" style={{ flex: 1, padding: 12, ...(hi ? { border: '2px solid var(--accent)', background: 'linear-gradient(160deg,#2A1A2E,#1A1A24)' } : { border: '1px solid #33323F' }) }}>
      <b>{name}</b><div style={{ fontSize: 20, fontWeight: 900, margin: '2px 0 8px' }}>{price}</div>
      {rows.map(([t, on]) => <div key={t} className="small" style={{ marginBottom: 5, color: on ? undefined : '#55536A' }}>{on ? '✓' : '—'} {t}</div>)}
    </div>
  )
  return (
    <div className="screen no-tab">
      <ChannelHead />
      <div className="sheet-bg"><div className="sheet" style={{ textAlign: 'center' }}>
        <div style={{ fontSize: 22, fontWeight: 900, marginBottom: 4 }}>✨ Реки+</div><div className="muted small" style={{ marginBottom: 14 }}>Для тех, кому 10 постов в день мало</div>
        <div style={{ display: 'flex', gap: 8, textAlign: 'left', marginBottom: 12 }}>
          {plan('Бесплатно', '0 ₽', [['10 постов в день', true], ['+5 за друга', true], ['свой стиль канала', false], ['все миры сразу', false]])}
          {plan('Реки+', '349 ₽/мес', [['20 постов в день', true], ['+5 за друга', true], ['свой стиль канала', true], ['все миры сразу', true]], true)}
        </div>
        <div className="muted small" style={{ marginBottom: 12 }}>Реки+ ещё в работе, деньги не списываются. Нажми, и мы напишем в боте, когда откроем</div>
        <Card style={{ marginBottom: 12, background: 'linear-gradient(135deg,#1F2A26,#1A1A24)', border: '1px solid #6BFFB855' }}><b className="up">{sent ? '+5 постов уже у тебя' : 'А пока держи +5 постов'}</b></Card>
        <Button onClick={() => take(true)} disabled={sent} style={{ marginBottom: 8 }}>Хочу Реки+, напишите мне</Button>
        <button className="muted small" onClick={() => take(false)} disabled={sent} style={{ background: 'none', border: 'none' }}>Забрать только +5 постов</button>
      </div></div>
    </div>
  )
}

// 04-2b: сообщение бота тем, кто нажал «Хочу» (пишет бэкенд, экран для справки)
function PlusPush() {
  return (
    <div className="tg screen no-tab">
      <div className="top" style={{ background: '#0E1621', borderColor: '#1A2533' }}><span style={{ fontSize: 22, color: '#8CC8FF' }}>‹</span><Avatar label="R" size={36} /><div><b>Реки</b><div className="small" style={{ color: '#6C8EA8' }}>бот</div></div></div>
      <div style={{ padding: 16 }}>
        <div className="small" style={{ textAlign: 'center', color: '#6C8EA8', marginBottom: 10 }}>сегодня</div>
        <div className="tgb">✨ <b>Реки+ открыт.</b> Обещали сказать тебе первым делом<br /><br />✍️ +10 постов в день<br />🎨 свой стиль канала: фон, рамка аватарки, шрифт<br />🌍 все миры сразу, без прокачки<br /><br />Первая неделя 99 ₽, дальше 349 ₽ в месяц. Отменить можно в настройках</div>
        <button className="tgbtn">Подключить Реки+</button><button className="tgbtn">Не сейчас</button>
        <div className="small" style={{ color: '#6C8EA8', marginTop: 8 }}>12:10</div>
      </div>
    </div>
  )
}

// 04-3: позвать друзей; первый друг открывает «Свой мир»; «Сыграй мою историю»
function Invite() {
  const nav = useNav(); const p = usePlayer()
  const [story, setStory] = useState(true)
  const copy = () => { if (inTelegram) copyTextToClipboard(LINK).catch(() => {}); else navigator.clipboard?.writeText(LINK) }
  return (
    <div className="screen no-tab">
      <TopBar onBack={nav.back} title="Позвать друзей" right="сегодня 1/3" />
      <div className="pad">
        <Card style={{ marginBottom: 12, textAlign: 'center' }}><div style={{ fontSize: 36 }}>🎁</div><b style={{ fontSize: 18 }}>+5 постов тебе и другу сразу</b><div className="small muted" style={{ marginTop: 4 }}>как только друг сделает первый пост</div></Card>
        <div className="card row" style={{ marginBottom: 12 }}><div style={{ fontSize: 22 }}>🔓</div><div className="small" style={{ flex: 1 }}><b>Первый друг откроет «Свой мир»</b><div className="muted">не нужно ждать 10k</div></div></div>
        <div className="card row" style={{ marginBottom: 12 }}><div className="small" style={{ flex: 1 }}><b>Сыграй мою историю</b><div className="muted">Друг начнёт с твоей завязки: Сцена, Мира злится на новенького. Твою переписку не видит. Потом сравните, кто как разрулил</div></div><Toggle on={story} onChange={setStory} /></div>
        <div className="label">Друг увидит твою карточку</div>
        <div style={{ background: 'linear-gradient(160deg,#2A1236,#141420 60%,#12203A)', borderRadius: 16, padding: 14, textAlign: 'center', marginBottom: 12 }}>
          <Avatar seed={p.handle} size={44} style={{ margin: '0 auto 6px' }} /><b>{p.name}</b>
          <div style={{ fontSize: 28, fontWeight: 900, background: 'linear-gradient(90deg,#FF6B8B,#FFB86B)', WebkitBackgroundClip: 'text', color: 'transparent' }}>{fmt(2310)}</div>
          <div className="small">подписчиков за 2 дня · {worldOf(p.world).emoji} {worldOf(p.world).title}</div><div className="small muted" style={{ marginTop: 4 }}>а ты кем будешь?</div>
        </div>
        <Card style={{ color: 'var(--muted)' }}><span className="small">Твоя ссылка: t.me/{TELEGRAM_BOT_USERNAME}?startapp=liza7 · до 3 бонусов в день</span></Card>
      </div>
      <BottomCTA second="Скопировать ссылку" onSecond={copy} main="Отправить в чат" onMain={shareInvite} />
    </div>
  )
}

// 04-4: друг пришёл по карточке
function FriendJoined() {
  const nav = useNav(); const p = usePlayer()
  return (
    <div className="screen no-tab">
      <ChannelHead />
      <div className="sheet-bg" style={{ alignItems: 'center' }}><div className="sheet" style={{ textAlign: 'center', padding: 24 }}>
        <div className="row" style={{ justifyContent: 'center', gap: 0, marginBottom: 12 }}><Avatar seed={p.handle} size={64} style={{ border: '3px solid #1A1A24' }} /><Avatar seed="katya" size={64} style={{ marginLeft: -14, border: '3px solid #1A1A24' }} /></div>
        <div style={{ fontSize: 22, fontWeight: 900 }}>Катя пришла по твоей карточке!</div>
        <div className="muted" style={{ margin: '6px 0 16px' }}>Она завела канал в Академии и сделала первый пост</div>
        <Gift style={{ fontSize: 20, padding: 14 }}>+5 постов тебе и ей</Gift>
        <div className="small muted" style={{ marginTop: 12 }}>Вернётся завтра: откроется карточка «Дуэт»</div>
        <Button style={{ marginTop: 16 }} onClick={() => nav.replace('03-4b_friend_channel')}>К каналу Кати</Button>
      </div></div>
    </div>
  )
}

register({
  '04-1_limit': { component: Limit, title: 'Лимит: звёзды отдыхают' },
  '04-2_fake_door': { component: FakeDoor, title: 'Реки+' },
  '04-2b_plus_push': { component: PlusPush, title: 'Бот: Реки+ открыт' },
  '04-3_invite': { component: Invite, title: 'Позвать друзей' },
  '04-4_friend_joined': { component: FriendJoined, title: 'Друг пришёл' },
})
