import { useEffect, useState } from 'react'
import { api } from '../api/client'
import { dm as dmMock, feed, firstPost, friends as friendsMock, worldOf } from '../api/mock'
import type { DmMessage, Drama } from '../api/types'
import { useNav } from '../app/nav'
import { useApi } from '../app/useApi'
import { usePlayer, useStore } from '../app/store'
import { Avatar, Bubble, Button, Card, Chip, Gift, Segmented, SkillBar, Sheet, TopBar, fmt, short } from '../ui'
import { ChannelHead } from './onboarding'
import { register } from '.'

const SKILL_NAMES = { brave: 'смелость', humor: 'юмор', topic: 'в тему' } as const

// 03-1: личка со звездой; память видна и правится; ~5 ответов звезды в день
function Dm() {
  const nav = useNav(); const p = usePlayer()
  const d = useApi(() => api.dm('mira'), dmMock)
  const [msgs, setMsgs] = useState<DmMessage[]>(d.messages)
  const [left, setLeft] = useState(d.repliesLeft)
  useEffect(() => { setMsgs(d.messages); setMemory(d.memory); setLeft(d.repliesLeft) }, [d])
  const [text, setText] = useState('')
  const [memory, setMemory] = useState<string[]>(d.memory)
  const [showMem, setShowMem] = useState(false)
  const closed = p.postsLeft <= 0
  const send = async (t: string) => {
    if (!t.trim() || closed || left <= 0) return
    setMsgs(m => [...m, { from: 'me', text: t }]); setText('')
    const r = await api.dmSend(d.star.id, t); setLeft(r.repliesLeft)
    if (r.reply) setMsgs(m => [...m, { from: 'star', text: r.reply! }])
  }
  return (
    <div className="screen">
      <TopBar onBack={nav.canBack ? nav.back : undefined}><Avatar seed={d.star.id} size={36} /><div><b>{d.star.name}</b><div className="small" style={{ color: 'var(--accent)' }}>{d.star.role} · {d.status}</div></div></TopBar>
      <div className="pad">
        <Card style={{ marginBottom: 12, background: 'linear-gradient(135deg,#2A1A2E,#1E1A33)' }}>
          <div className="row"><b style={{ flex: 1 }}>Отношения с Мирой +2 → +3</b><span className="small" style={{ color: 'var(--gift)' }}>👑</span></div>
          <div className="small muted" style={{ marginTop: 2 }}>На +3 её карточка попадёт в коллекцию</div>
          <button onClick={() => setShowMem(true)} className="small" style={{ marginTop: 8, background: 'none', border: 'none', color: 'var(--lilac)' }}>Мира помнит: {memory.join(', ')} ›</button>
        </Card>
        {msgs.map((m, i) => <div key={i} style={{ position: 'relative' }}><Bubble me={m.from === 'me'}>{m.text}</Bubble>{m.from === 'star' && <button onClick={() => nav.push('06-1_report', { text: m.text })} aria-label="Пожаловаться" style={{ position: 'absolute', right: 0, top: 6, background: 'none', border: 'none', color: 'var(--muted)' }}>⋯</button>}</div>)}
        {closed ? <Card style={{ marginTop: 10, textAlign: 'center' }}><span className="muted small">Мира ушла на репетицию. Ответит утром, в 9:00</span></Card> : <>
          <div className="small muted" style={{ margin: '10px 0 8px' }}>💡 ИДЕИ ИЛИ СВОИМИ СЛОВАМИ · бесплатно · Мира ответит ещё {left} {left === 1 ? 'раз' : 'раза'} сегодня</div>
          {d.ideas.map(i => <Chip key={i} idea onClick={() => send(i.replace(/[«»]/g, ''))}>{i}</Chip>)}
        </>}
      </div>
      {!closed && <div className="bottom-cta" style={{ flexDirection: 'row', bottom: 78 }}>
        <div className="card" style={{ flex: 1, padding: '12px 14px' }}><input value={text} onChange={e => setText(e.target.value)} placeholder="Ответить Мире своими словами…" maxLength={300} /></div>
        <button className="card" onClick={() => send(text)} style={{ padding: '14px 18px', background: 'var(--grad)', border: 'none' }} aria-label="Отправить">➤</button>
      </div>}
      {showMem && <Sheet onClose={() => setShowMem(false)}>
        <div style={{ fontSize: 20, fontWeight: 800, marginBottom: 4 }}>Что Мира помнит о тебе</div>
        <div className="small muted" style={{ marginBottom: 12 }}>Она опирается на это в разговорах. Удали, что не хочешь, чтобы она помнила</div>
        {memory.map(m => <div key={m} className="why"><span>{m}</span><button onClick={() => { setMemory(x => x.filter(y => y !== m)); api.forget(d.star.id, m) }} style={{ background: 'none', border: 'none', color: 'var(--accent)' }}>удалить</button></div>)}
        <Button secondary style={{ marginTop: 14 }} onClick={() => setShowMem(false)}>Готово</Button>
      </Sheet>}
    </div>
  )
}

// 03-2: карточка звезды на +3
function StarCard() {
  const nav = useNav()
  return (
    <div className="screen no-tab" style={{ background: 'radial-gradient(circle at 50% 40%,#3A1F4A,#0E0E13 70%)', textAlign: 'center', padding: '50px 26px' }}>
      <span className="badge">НОВАЯ КАРТОЧКА · 2/12</span>
      <div style={{ margin: '24px auto', width: 230, borderRadius: 22, padding: 18, background: 'linear-gradient(160deg,#3A2147,#21324A)', boxShadow: '0 20px 60px #0008' }}>
        <Avatar seed="mira" size={120} style={{ margin: '0 auto 12px' }} /><b style={{ fontSize: 22 }}>Мира</b><div className="small muted">лидер BLESK</div>
        <div style={{ marginTop: 12, fontStyle: 'italic' }}>«ты из тех, кто падает и встаёт. уважаю»</div>
      </div>
      <div style={{ fontSize: 22, fontWeight: 800 }}>Мира теперь на твоей стороне</div>
      <div className="muted" style={{ marginTop: 6 }}>Отношения +3. Она чаще делает репосты и зовёт на коллабы</div>
      <div className="bottom-cta"><Button onClick={() => nav.canBack ? nav.back() : nav.tab(4)}>В коллекцию</Button></div>
    </div>
  )
}

// 03-3: лента мира
function Feed() {
  const nav = useNav()
  const items = useApi(api.feed, feed)
  const p = usePlayer(); const w = worldOf(p.world)
  return (
    <div className="screen">
      <TopBar title={`Лента ${w.gen}`} right={<button onClick={() => nav.push('05-11_world_rules')} style={{ background: 'none', border: 'none', color: 'inherit', fontSize: 13 }}>📜 Как устроен мир</button>} />
      <div className="pad">
        {items.map((f, i) => f.kind === 'news'
          ? <Card key={i} style={{ marginBottom: 12, border: '1px solid #FFB86B44' }}><span className="badge">НОВОСТИ АГЕНТСТВА</span><div style={{ marginTop: 6 }}>{f.text}</div></Card>
          : <Card key={i} style={{ marginBottom: 12 }}>
              <div className="row"><Avatar seed={f.avatar!} size={36} /><div style={{ flex: 1 }}><b>{f.author}</b> <span className="small muted">{f.role}</span><div className="tiny muted">{f.ago}</div></div><button onClick={() => nav.push('06-1_report', { text: f.text })} aria-label="Пожаловаться" style={{ background: 'none', border: 'none', color: 'var(--muted)' }}>⋯</button></div>
              {f.repost && <div className="small" style={{ background: '#20202B', borderRadius: 10, padding: 8, margin: '8px 0' }}><b>Лиза · трейни</b> <span className="muted">· в реках 🌊</span><div>{f.repost}</div></div>}
              <div style={{ marginTop: 8 }}>{f.text}</div>
              <div style={{ marginTop: 8 }}>{f.reactions?.map(r => <span key={r.emoji} className="react">{r.emoji} {short(r.count)}</span>)}</div>
            </Card>)}
      </div>
    </div>
  )
}

// 03-4: друзья; гонки по умолчанию нет, заруба по вызову
function Friends() {
  const nav = useNav()
  const friends = useApi(api.friends, friendsMock)
  return (
    <div className="screen">
      <TopBar title="Друзья" right={`${friends.length} в Реках`} />
      <div className="pad">
        {friends.map(f => (
          <Card key={f.id} style={{ marginBottom: 10 }}>
            <div className="row"><Avatar seed={f.avatar} size={40} /><div style={{ flex: 1 }}><b>{f.name}</b><div className="small muted">{f.world} · {fmt(f.subs)} подписчиков</div></div><button className="react" onClick={() => nav.push('03-4b_friend_channel', { id: f.id })} style={{ border: 'none' }}>→ канал</button></div>
            <div className="small" style={{ marginTop: 8, background: '#20202B', borderRadius: 10, padding: 8 }}>{f.lastPost}</div>
            <div className="small" style={{ marginTop: 8, color: 'var(--gift)' }}>💬 Прокомментируй: {f.pronoun} фанаты увидят и придут к тебе</div>
          </Card>
        ))}
        <Card style={{ background: 'linear-gradient(135deg,#2A1A2E,#1E1A33)' }}>
          <div className="row"><div style={{ fontSize: 26 }}>⚔️</div><div style={{ flex: 1 }}><b>Заруба на 3 дня</b><div className="small muted">Только если оба согласны. Считаем качество постов, а не число подписчиков</div></div></div>
          <div style={{ display: 'flex', gap: 8, marginTop: 10 }}>
            <Button style={{ padding: 10, fontSize: 14, flex: 1 }} onClick={() => nav.push('03-4c_challenge')}>Вызвать друга</Button>
            <Button secondary style={{ padding: 10, fontSize: 14, flex: 1 }} onClick={() => nav.push('04-3_invite')}>Сыграй мою историю</Button>
          </div>
        </Card>
      </div>
    </div>
  )
}

// 03-4b: канал друга; комментарий бесплатно; личка друга закрыта
function FriendChannel() {
  const nav = useNav(); const p = usePlayer()
  const [c, setC] = useState('')
  const [mine, setMine] = useState(['баннер решает, остальное мелочи 😌'])
  return (
    <div className="screen no-tab">
      <TopBar onBack={nav.back} right="друг"><Avatar seed="katya" size={36} /><div><b>Катя · ахаха</b><div className="muted small">5 120 подписчиков · 🌸 Академия</div></div></TopBar>
      <div className="pad">
        <Card style={{ marginBottom: 10 }}>{(['brave', 'humor', 'topic'] as const).map((k, i) => <SkillBar key={k} name={SKILL_NAMES[k]} progress={[.5, .8, .6][i]} />)}<div className="small muted" style={{ marginTop: 6 }}>коллекция 4/12 · 🔥 3 дня</div></Card>
        <Card style={{ marginBottom: 10 }}>
          <div className="tiny muted">сегодня, 19:02</div><div style={{ margin: '6px 0' }}>на фестивале мой клуб занял последнее место. зато у нас был лучший баннер 🏳️</div>
          <span className="react">😂 310</span><span className="react">🫶 88</span><span className="small muted" style={{ float: 'right' }}>👁 6,1k · 💬 41</span>
        </Card>
        {mine.map((t, i) => <div key={i} className="row" style={{ gap: 10, marginBottom: 10 }}><Avatar seed={p.name} size={30} /><div><b>Лиза · трейни</b> <span className="small muted">ты</span><div>{t}</div></div></div>)}
        <Gift style={{ marginBottom: 10 }}>Твой комментарий видят 5 120 её подписчиков. Обычно +20–60 к тебе</Gift>
        <div className="small muted">Видно только то, что Катя выложила в игре. Её личка со звёздами закрыта</div>
      </div>
      <div className="bottom-cta" style={{ flexDirection: 'row' }}>
        <div className="card" style={{ flex: 1, padding: '12px 14px' }}><input value={c} onChange={e => setC(e.target.value)} placeholder="Комментарий · бесплатно" maxLength={300} /></div>
        <button className="card" onClick={() => { if (c.trim()) { setMine(m => [...m, c]); setC('') } }} style={{ padding: '14px 18px', background: 'var(--grad)', border: 'none' }}>🔥</button>
      </div>
    </div>
  )
}

// 03-5: канал: баннер этапа, навыки, коллекция, посты
function Stage() {
  const nav = useNav(); const p = usePlayer()
  const [banner, setBanner] = useState(true)
  return (
    <div className="screen">
      <ChannelHead />
      <div className="pad">
        {banner && <Card style={{ marginBottom: 10, background: 'linear-gradient(135deg,#2A1A2E,#1E1A33)', border: '1px solid #FF6B8B55', position: 'relative' }}>
          <button onClick={() => setBanner(false)} className="muted" style={{ position: 'absolute', right: 14, top: 10, background: 'none', border: 'none' }}>✕</button>
          <span className="badge">НОВЫЙ ЭТАП · 1 000 ПОДПИСЧИКОВ</span>
          <div style={{ fontSize: 18, fontWeight: 800, margin: '6px 0 4px' }}>👀 Взгляды</div>
          <div className="small muted">Тебя заметили. Теперь фанаты и звёзды пишут честно: хвалят и критикуют по делу. Только про посты</div>
        </Card>}
        <Card style={{ marginBottom: 10 }}>
          <div className="row" style={{ justifyContent: 'space-between', marginBottom: 8 }}><b>Навыки · из 10</b><span className="small" style={{ color: 'var(--lilac)' }}>как поднять ›</span></div>
          {(Object.keys(SKILL_NAMES) as (keyof typeof SKILL_NAMES)[]).map(k => <SkillBar key={k} name={SKILL_NAMES[k]} level={p.skills[k].level} progress={p.skills[k].progress} />)}
        </Card>
        <Card style={{ marginBottom: 10 }} onClick={() => nav.tab(4)}><div className="row"><div style={{ fontSize: 22 }}>🃏</div><div style={{ flex: 1 }}><b>Коллекция {p.collection.filter(c => c.open).length}/12</b><div className="small muted">Первый залёт</div></div><span className="muted">›</span></div></Card>
        <Card onClick={() => nav.push('02-6_thread')}><div className="small muted" style={{ marginBottom: 6 }}>вчера · в реках 🌊</div>{firstPost.text}<div style={{ marginTop: 8 }}><span className="react">😂 412</span><span className="react">🫶 96</span><span className="small muted" style={{ float: 'right' }}>👁 48k</span></div></Card>
      </div>
      <div className="bottom-cta" style={{ bottom: 78 }}><Button onClick={() => nav.push(p.postsLeft > 0 ? '02-1_compose' : '04-1_limit')}>Новый пост</Button></div>
    </div>
  )
}

// 05-5: «Я»: статы, серия неделей, навыки, коллекция, следующий мир
function Me() {
  const nav = useNav(); const p = usePlayer()
  const DAYS = ['пн', 'вт', 'ср', 'чт', 'пт', 'сб', 'вс']
  const today = p.streak.week.indexOf(false)
  return (
    <div className="screen">
      <div className="top" style={{ border: 'none' }}><b style={{ fontSize: 18 }}>Я</b><button className="pill" onClick={() => nav.push('05-8_settings')} style={{ border: 'none' }}>⚙ Настройки</button></div>
      <div style={{ padding: '0 16px', textAlign: 'center' }}><Avatar seed={p.name} size={64} style={{ margin: '0 auto 6px' }} /><b style={{ fontSize: 18 }}>{p.name}</b><div className="muted small">{worldOf(p.world).emoji} {worldOf(p.world).title} · типаж: {p.type}</div></div>
      <div className="pad">
        <div style={{ display: 'flex', gap: 8, marginBottom: 10 }}>
          {[[`✍️ ${p.postsLeft}`, 'постов осталось'], [`🎯 ${p.wants.filter(w => w.done).length}/${p.wants.length}`, 'желаний дня'], [fmt(6210), 'подписчиков']].map(([v, l]) => <Card key={l} style={{ flex: 1, padding: 10, textAlign: 'center' }}><div style={{ fontWeight: 800, fontSize: 16 }}>{v}</div><div className="small muted">{l}</div></Card>)}
        </div>
        <Card style={{ marginBottom: 10 }}>
          <div className="row" style={{ justifyContent: 'space-between', marginBottom: 8 }}><b>Серия {p.streak.days} дня</b><span className="small" style={{ color: 'var(--gift)' }}>{p.streak.nextReward}</span></div>
          <div style={{ display: 'flex', gap: 6 }}>{DAYS.map((d, i) => <div key={d} style={{ flex: 1, textAlign: 'center' }}><div style={{ height: 30, borderRadius: 9, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 14, ...(p.streak.week[i] ? { background: 'var(--grad)' } : i === today ? { border: '2px dashed var(--accent)' } : { background: '#20202B' }) }}>{p.streak.week[i] ? '🔥' : i === today ? '?' : ''}</div><div className="small muted" style={{ marginTop: 3 }}>{d}</div></div>)}</div>
        </Card>
        <Card style={{ marginBottom: 10 }}>
          <div className="row" style={{ justifyContent: 'space-between', marginBottom: 8 }}><b>Навыки · из 10</b><span className="small" style={{ color: 'var(--lilac)' }}>как поднять ›</span></div>
          {(Object.keys(SKILL_NAMES) as (keyof typeof SKILL_NAMES)[]).map(k => <SkillBar key={k} name={SKILL_NAMES[k]} level={p.skills[k].level} progress={p.skills[k].progress} />)}
        </Card>
        <Card style={{ marginBottom: 10 }}>
          <div className="row" style={{ justifyContent: 'space-between', marginBottom: 8 }}><b>Коллекция {p.collection.filter(c => c.open).length}/12</b><span className="small" style={{ color: 'var(--lilac)' }}>все ›</span></div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(6,1fr)', gap: 6 }}>{p.collection.map(c => <div key={c.id} className={`slot${c.open ? ' on' : ''}`} onClick={() => c.emoji === '👑' && nav.push('03-2_star_card')} style={{ fontSize: c.open ? 22 : 14 }}>{c.open ? c.emoji : '?'}</div>)}</div>
        </Card>
        {p.nextWorld && <Card><div className="row"><div style={{ fontSize: 22 }}>🔒</div><div style={{ flex: 1 }}><b>Мир «{p.nextWorld.title}»</b><div className="small muted">откроется на {p.nextWorld.need}</div></div><button onClick={() => nav.push('04-3_invite')} className="small" style={{ color: 'var(--gift)', background: 'none', border: 'none' }}>позвать друга ›</button></div></Card>}
      </div>
    </div>
  )
}

// 05-8: настройки: градус драмы, голос канала, обращение, данные, удалить аккаунт
function Settings() {
  const nav = useNav(); const p = usePlayer(); const { patch } = useStore()
  const [drama, setDrama] = useState<Drama>(p.settings.drama)
  const [del, setDel] = useState(false)
  const row = (t: string, r: string, last = false, onClick?: () => void) => <div onClick={onClick} className="row" style={{ padding: '12px 0', borderBottom: last ? 'none' : '1px solid var(--line2)' }}><div style={{ flex: 1 }}>{t}</div><span className="muted">{r}</span></div>
  const setD = (d: Drama) => { setDrama(d); patch({ settings: { ...p.settings, drama: d } }); api.settings({ drama: d }) }
  return (
    <div className="screen no-tab">
      <TopBar onBack={nav.back} title="Настройки" />
      <div className="pad">
        <Card style={{ padding: '2px 14px', marginBottom: 12 }}>
          {row('🔔 Уведомления', `${p.settings.notifications} ›`)}{row('🌙 Тихие часы', `${p.settings.quiet} ›`)}
          <div style={{ padding: '12px 0' }}><div>🌩 Градус драмы</div><div className="small muted" style={{ margin: '2px 0 8px' }}>Сколько хейта и скандалов в мире. Фильтр 14+ работает всегда</div>
            <Segmented value={drama} onChange={setD} options={[{ id: 'soft', label: 'мягко' }, { id: 'mid', label: 'средне' }, { id: 'hot', label: 'остро' }, { id: 'off', label: 'без хейта' }]} /></div>
        </Card>
        <Card style={{ padding: '2px 14px', marginBottom: 12 }}>
          {row('🎙 Голос канала', `${p.settings.voice} ›`, false, () => nav.push('05-9_voice'))}{row('🙂 Как к тебе обращаться', `${{ she: 'она', he: 'он', nick: 'по нику' }[p.address]} ›`)}
          {row('✨ Реки+', '349 ₽ ›', false, () => nav.push('04-2_fake_door'))}{row('📜 Как устроен мир', '›', false, () => nav.push('05-11_world_rules'))}{row('🔒 Мои данные и правила', '›')}{row('✉ Поддержка', '›', true)}
        </Card>
        <button onClick={() => setDel(true)} className="card" style={{ width: '100%', textAlign: 'center', color: 'var(--accent)', background: 'transparent', border: '1px solid #FF6B8B55' }}>Удалить аккаунт и всё</button>
        <div className="small muted" style={{ textAlign: 'center', marginTop: 8 }}>Удалим канал, переписку и коллекцию. Вернуть будет нельзя</div>
      </div>
      {del && <Sheet onClose={() => setDel(false)}>
        <div style={{ fontSize: 20, fontWeight: 800, marginBottom: 6 }}>Удалить всё?</div>
        <div className="muted" style={{ marginBottom: 16 }}>Канал, переписка со звёздами, коллекция и прогресс удалятся навсегда</div>
        <Button style={{ background: '#C0392B', marginBottom: 8 }} onClick={async () => { await api.deleteAccount(); setDel(false); nav.replace('01-2_age') }}>Удалить навсегда</Button><Button secondary onClick={() => setDel(false)}>Отмена</Button>
      </Sheet>}
    </div>
  )
}

register({
  '03-1_dm': { component: Dm, title: 'Личка со звездой' },
  '03-2_star_card': { component: StarCard, title: 'Карточка звезды' },
  '03-3_feed': { component: Feed, title: 'Лента мира' },
  '03-4_friends': { component: Friends, title: 'Друзья' },
  '03-4b_friend_channel': { component: FriendChannel, title: 'Канал друга' },
  '03-5_stage': { component: Stage, title: 'Канал и этап' },
  '05-5_collection': { component: Me, title: 'Я' },
  '05-8_settings': { component: Settings, title: 'Настройки' },
}, { '03-1_dm': 2, '03-3_feed': 1, '03-4_friends': 3, '03-5_stage': 0, '05-5_collection': 4 })
