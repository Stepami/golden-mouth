import { QuotedPost } from '../ui/QuotedPost'
import { RepostAction } from '../ui/RepostAction'
import {PushNotifications} from '../app/push/PushNotifications'
import {InstallApp} from '../app/install/InstallApp'
import { LinkProfile } from '../app/LinkProfile'
import { displayHandle } from '../app/handle'
import { Streak } from '../ui/Streak'
import { ExpandableText } from '../ui'
import { AvatarPersonalization } from '../ui/AvatarPersonalization'
import { setAvatarPersonalization, useAvatarPersonalization } from '../app/avatarPersonalization'
import { contentOf, worldOf } from '../api/worldContent'
import { useCallback, useEffect, useRef, useState } from 'react'
import { api } from '../api/client'
import { dm as dmMock, friends as friendsMock } from '../api/mock'
import type { DmMessage, Drama, FeedItem } from '../api/types'
import { useNav } from '../app/nav'
import { useApi } from '../app/useApi'
import { usePostPages } from '../app/usePostPages'
import { usePostListGestures } from '../app/usePostListGestures'
import { runtimeConfig } from '../app/runtimeConfig'
import { SkillHelp } from '../ui/SkillHelp'
import { usePlayer, useStore } from '../app/store'
import { Avatar, Bubble, Button, Card, Chip, Gift, Segmented, SkillBar, Sheet, TopBar, fmt, short } from '../ui'
import { beginOwnWorld } from '../app/worldDraft'
import { ChannelHead, Home } from './onboarding'
import { register } from '.'
import { TextField } from '../ui/TextField'

const SKILL_NAMES = { brave: 'смелость', humor: 'юмор', topic: 'в тему' } as const

function postDate(value: string) {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString('ru-RU', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })
}

// 03-1: личка со звездой; память видна и правится; ~5 ответов звезды в день
function Dm() {
  const nav = useNav(); const p = usePlayer()
  const star = contentOf(p.world).stars[0]
  const d = useApi(() => api.dm(star.id), { ...dmMock, star }, [star.id], `dm:${p.worldInstanceId}:${star.id}`)
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
      <TopBar onBack={nav.canBack ? nav.back : undefined}><Avatar seed={star.avatarSeed} size={36} mode="default" /><div><b>{d.star.name}</b><div className="small" style={{ color: 'var(--accent)' }}>{d.star.role} · {d.status}</div></div></TopBar>
      <div className="pad">
        <Card style={{ marginBottom: 12, background: 'linear-gradient(135deg,#2A1A2E,#1E1A33)' }}>
          <div className="row"><b style={{ flex: 1 }}>Отношения с Мирой +2 → +3</b><span className="small" style={{ color: 'var(--gift)' }}>👑</span></div>
          <div className="small muted" style={{ marginTop: 2 }}>На +3 её карточка попадёт в коллекцию</div>
          <button onClick={() => setShowMem(true)} className="small" style={{ marginTop: 8, background: 'none', border: 'none', color: 'var(--lilac)' }}>Мира помнит: {memory.join(', ')} ›</button>
        </Card>
        {msgs.map((m, i) => <div key={i} style={{ position: 'relative' }}><Bubble me={m.from === 'me'}>{m.text}</Bubble></div>)}
        {closed ? <Card style={{ marginTop: 10, textAlign: 'center' }}><span className="muted small">Мира ответит, когда появится энергия. Она восстанавливается каждые 10 минут.</span></Card> : <>
          <div className="small muted" style={{ margin: '10px 0 8px' }}>💡 ИДЕИ ИЛИ СВОИМИ СЛОВАМИ · бесплатно · Мира ответит ещё {left} {left === 1 ? 'раз' : 'раза'} сегодня</div>
          <TextField label="Сообщение" value={text} onChange={setText} ideas={d.ideas} maxLength={300} />
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
        <Avatar seed="mira" mode="default" size={120} style={{ margin: '0 auto 12px' }} /><b style={{ fontSize: 22 }}>Мира</b><div className="small muted">лидер BLESK</div>
        <div style={{ marginTop: 12, fontStyle: 'italic' }}>«ты из тех, кто падает и встаёт. уважаю»</div>
      </div>
      <div style={{ fontSize: 22, fontWeight: 800 }}>Мира теперь на твоей стороне</div>
      <div className="muted" style={{ marginTop: 6 }}>Отношения +3. Она чаще делает репосты и зовёт на коллабы</div>
      <div className="bottom-cta"><Button onClick={() => nav.canBack ? nav.back() : nav.tab(4)}>В коллекцию</Button></div>
    </div>
  )
}

// 03-3: карточка поста в общей ленте. Открывается с клавиатуры (Enter/Space) и ведёт
// только на свой postId с доступным ответом. Чужие карточки остаются в ленте.
function FeedPostCard({ item, onChanged }: { item: FeedItem; onChanged: () => void }) {
  const nav = useNav()
  const open = item.postId ? () => nav.push('02-6_thread', { postId: item.postId, returnTo: 'feed' }) : undefined
  return (
    <Card style={{ marginBottom: 12, position: 'relative' }}>
      <div
        role={open ? 'button' : undefined}
        tabIndex={open ? 0 : undefined}
        onClick={open}
        onKeyDown={open ? event => { if (event.target !== event.currentTarget) return; if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); open() } } : undefined}
        style={open ? { cursor: 'pointer' } : undefined}
      >
        <div className="row"><Avatar seed={item.avatar!} world={item.world} size={36} mode={item.starId ? 'default' : 'profile'} /><div style={{ flex: 1, paddingRight: 24 }}><b>{item.author}</b>{item.starId && <span className="small" style={{ color: 'var(--accent)' }}> ★ звезда</span>} <span className="small muted">{item.role}</span><div className="tiny muted">{item.ago && postDate(item.ago)}{item.demo && ' · демо'}</div></div></div>
        {item.repostId && <div className="small muted">↻ Репост</div>}
        <div style={{ marginTop: 12 }}><ExpandableText text={item.text} /></div>
        <QuotedPost repostId={item.repostId} />
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 8, marginTop: 8 }}>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 4 }}>{item.reactions?.map(r => <span key={r.emoji} className="react">{r.emoji} {short(r.count)}</span>)}</div>
          {item.commentCount != null && item.views != null && <span className="small muted" style={{ whiteSpace: 'nowrap' }}>💬 {item.commentCount} · 👁 {short(item.views)}</span>}
        </div>
      </div>
      {item.authorId?.startsWith('Star_') && !item.repostId && item.postId && <RepostAction postId={item.postId} onChanged={onChanged} />}
    </Card>
  )
}

// 03-3: лента мира
function Feed() {
  const nav = useNav()
  const p = usePlayer(); const w = worldOf(p.world)
  const key = `feed-keyset-v1:${p.id}:${p.worldInstanceId ?? p.world}`
  const loadPage = useCallback((cursor: string | undefined, signal: AbortSignal) => api.feed(p.worldInstanceId, cursor, signal), [p.worldInstanceId])
  const list = usePostPages<FeedItem>(key, loadPage)
  const [arrivals, setArrivals] = useState({ key, ids: [] as string[], note: '' })
  const newIds = arrivals.key === key ? arrivals.ids : []
  const refreshNote = arrivals.key === key ? arrivals.note : ''
  const refresh = async () => {
    const seen = new Set(list.items.map(item => item.postId))
    setArrivals({ key, ids: [], note: '' })
    const page = await list.refresh()
    if (!page) return
    const ids = page.items.flatMap(item => item.postId && !seen.has(item.postId) ? [item.postId] : [])
    setArrivals({ key, ids, note: ids.length ? `Новых постов: ${ids.length}` : 'Пока нет новых постов' })
  }
  const gestures = usePostListGestures(key, list, refresh)
  return (
    <div className="screen post-list-screen" ref={gestures.screenRef}>
      <div className="post-list-pull" role="status" aria-hidden={!gestures.distance && !(list.pending === 'first' && list.ready)} style={{ height: list.pending === 'first' && list.ready ? 44 : gestures.distance }}>{list.pending === 'first' ? 'Обновляем…' : gestures.releaseToRefresh ? 'Отпусти, чтобы обновить' : 'Потяни вниз, чтобы обновить'}</div>
      <TopBar title={`Лента ${w.gen}`} right={<button onClick={() => nav.push('05-11_world_rules')} style={{ background: 'none', border: 'none', color: 'inherit', fontSize: 13 }}>📜 Как устроен мир</button>} />
      <div className="pad">
        {list.items.map((f, i) => f.kind === 'news'
          ? <Card key={`news-${i}`} style={{ marginBottom: 12, border: '1px solid #FFB86B44' }}><span className="badge">НОВОСТИ МИРА</span><div style={{ marginTop: 6 }}>{f.text}</div></Card>
          : <div className={newIds.includes(f.postId ?? "") ? "feed-arrival" : undefined} key={f.postId ?? `post-${i}`}><FeedPostCard item={f} onChanged={() => { void list.refresh() }} /></div>)}
        {list.pending === 'first' && !list.ready && <Card>Загружаем ленту…</Card>}
        {list.error === 'first' && <Card>Не получилось обновить ленту. Потяни вниз от начала списка, чтобы повторить.</Card>}
        {list.ready && list.items.length === 0 && <Card>В ленте пока нет постов.</Card>}
        {(refreshNote && !list.pending) && <div role="status" className="small muted refresh-note">{refreshNote}</div>}
        <div className="post-list-tail" ref={gestures.tailRef} role="status">{list.pending === 'more' ? 'Загружаем посты…' : list.error === 'more' ? 'Не получилось загрузить посты. Потяни вниз от начала списка, чтобы обновить.' : list.ready && !list.hasNext && list.items.length > 0 ? 'Все посты загружены' : ''}</div>
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
        {mine.map((t, i) => <div key={i} className="row" style={{ gap: 10, marginBottom: 10 }}><Avatar seed={p.handle} size={30} /><div><b>Лиза · трейни</b> <span className="small muted">ты</span><div>{t}</div></div></div>)}
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
  const [skillHelp, setSkillHelp] = useState(false)
  const cacheKey = `channel-keyset-v1:${p.id}:${p.worldInstanceId ?? p.world}`
  const [banner, setBanner] = useState(true)
  const loadPage = useCallback((cursor: string | undefined, signal: AbortSignal) => api.channelPosts(p.worldInstanceId, cursor, signal), [p.worldInstanceId])
  const list = usePostPages<FeedItem>(cacheKey, loadPage)
  const gestures = usePostListGestures(cacheKey, list, list.refresh)
  const pullIndicator = <div className="post-list-pull" role="status" aria-hidden={!gestures.distance && !(list.pending === 'first' && list.ready)} style={{ height: list.pending === 'first' && list.ready ? 44 : gestures.distance }}>{list.pending === 'first' ? 'Обновляем…' : gestures.releaseToRefresh ? 'Отпусти, чтобы обновить' : 'Потяни вниз, чтобы обновить'}</div>
  if (list.ready && list.items.length === 0 && !list.error) return <div ref={gestures.screenRef} className="post-list-screen">{pullIndicator}<Home /></div>
  return (
    <div className="screen channel-screen post-list-screen" ref={gestures.screenRef}>
      {pullIndicator}
      <ChannelHead />
      <div className="pad">
        <Streak streak={p.streak} />
        {banner && p.subs >= 1000 && <Card style={{ marginBottom: 10, background: 'linear-gradient(135deg,#2A1A2E,#1E1A33)', border: '1px solid #FF6B8B55', position: 'relative' }}>
          <button onClick={() => setBanner(false)} className="muted" style={{ position: 'absolute', right: 14, top: 10, background: 'none', border: 'none' }}>✕</button>
          <span className="badge">НОВЫЙ ЭТАП · 1 000 ПОДПИСЧИКОВ</span>
          <div style={{ fontSize: 18, fontWeight: 800, margin: '6px 0 4px' }}>👀 Взгляды</div>
          <div className="small muted">Тебя заметили. Теперь фанаты и звёзды пишут честно: хвалят и критикуют по делу. Только про посты</div>
        </Card>}
        <Card style={{ marginBottom: 10 }}>
          <div className="row" style={{ justifyContent: 'space-between', marginBottom: 8 }}><b>Навыки · из 10</b><button className="text-button small" onClick={() => setSkillHelp(true)}>как поднять ›</button></div>
          {(Object.keys(SKILL_NAMES) as (keyof typeof SKILL_NAMES)[]).map(k => <SkillBar key={k} name={SKILL_NAMES[k]} level={p.skills[k].level} progress={p.skills[k].progress} />)}
        </Card>
        {runtimeConfig.features.collection && <Card style={{ marginBottom: 10 }} onClick={() => nav.tab(4)}><div className="row"><div style={{ fontSize: 22 }}>🃏</div><div style={{ flex: 1 }}><b>Коллекция {p.collection.filter(c => c.open).length}/12</b></div><span className="muted">›</span></div></Card>}
        <Button secondary style={{ marginBottom: 10 }} onClick={() => nav.push('04-3_invite')}>Позвать друга</Button>
        {list.pending === 'first' && !list.ready && <Card><span className="small muted">загружаем посты…</span></Card>}
        {list.error === 'first' && <Card><div className="small">Не получилось загрузить канал. Потяни вниз от начала списка, чтобы повторить.</div></Card>}
        {list.items.map(post => <Card key={post.postId} onClick={() => nav.push('02-6_thread', { postId: post.postId, returnTo: 'channel' })} style={{ marginBottom: 10 }}><div className="small muted" style={{ marginBottom: 6 }}>{postDate(post.ago)} · в реках 🌊</div>{post.repostId && <div className="small muted">↻ Репост</div>}{post.text}<QuotedPost repostId={post.repostId} />{post.repostId && !post.text.trim() && <RepostAction postId={post.postId} undoOnly onChanged={() => { void list.refresh() }} />}<div style={{ marginTop: 8 }}>{post.reactions.map(reaction => <span className="react" key={reaction.emoji}>{reaction.emoji} {short(reaction.count)}</span>)}<span className="small muted" style={{ float: 'right' }}>💬 {post.commentCount} · 👁 {short(post.views)}</span></div></Card>)}
        <div className="post-list-tail" ref={gestures.tailRef} role="status">{list.pending === 'more' ? 'Загружаем посты…' : list.error === 'more' ? 'Не получилось загрузить посты. Потяни вниз от начала списка, чтобы обновить.' : list.ready && !list.hasNext ? 'Все посты загружены' : ''}</div>
      </div>
      <div className="bottom-cta" style={{ bottom: 78 }}><Button onClick={() => nav.push(p.postsLeft > 0 ? '02-1_compose' : '04-1_limit')}>Новый пост</Button></div>
      {skillHelp && <SkillHelp onClose={() => setSkillHelp(false)} />}
    </div>
  )
}

// 05-5: «Я»: статы, серия неделей, навыки, коллекция, следующий мир
function Me() {
  const nav = useNav(); const p = usePlayer()
  const [skillHelp, setSkillHelp] = useState(false)
  const [linkProfile, setLinkProfile] = useState(false)
  const { refresh } = useStore()
  useEffect(() => { void refresh() }, [refresh])
  return (
    <div className="screen">
      <TopBar title="Я" rightPlain right={<button className="header-action" onClick={() => nav.push('05-8_settings')}>⚙ Настройки</button>} />
      <div style={{ padding: '0 16px', textAlign: 'center' }}><Avatar seed={p.handle} size={64} style={{ margin: '0 auto 6px' }} /><b style={{ fontSize: 18 }}>{p.name}</b>{p.handle && <div className="small muted" style={{ overflowWrap: 'anywhere' }}>{displayHandle(p.handle)}</div>}<div className="small muted profile-world-name">{worldOf(p.world).emoji} {worldOf(p.world).title}</div></div>
      <div className="pad">
        <div className="profile-stats">
          {[[`⚡ ${p.postsLeft}`, 'энергии'], ...(p.energyWallet ? [[`💎 ${p.energyWallet.crystals}`, 'кристаллы']] : []), [fmt(p.subs), 'подписчиков']].map(([v, l]) => <Card key={l} style={{ padding: 14, textAlign: 'center' }}><div style={{ fontWeight: 800, fontSize: 16 }}>{v}</div><div className="small muted">{l}</div></Card>)}
        </div>
        <Button secondary onClick={() => setLinkProfile(true)}>🔑 Привязать профиль</Button><p className="small muted energy-help">Пост и ответ расходуют энергию. Энергия восстанавливается по одной каждые 10 минут.</p>
        <div className="card own-world-unlock" style={{ marginTop: 0, marginBottom: 12 }}><b>{p.hitOwnWorldMilestone ? '✨ Свой мир открыт' : '✨ Создай свой мир'}</b>{p.hitOwnWorldMilestone ? <>
          <p className="small muted">Доступ подтверждён. Можно придумать свою историю.</p>
          <button className="text-link" onClick={() => { beginOwnWorld(p); nav.push('05-7b_own_world', {}) }}>Создать свой мир →</button>
        </> : <>
          <div className="own-unlock-goal"><b>Пригласи 1 друга</b><span>0/1</span></div>
          <progress aria-label="Друзья с первым постом до открытия своего мира" value={0} max={1} />
          <p className="small muted">Мир откроется, когда друг опубликует первый пост.</p>
          <Button onClick={() => nav.push('04-3_invite')}>Пригласить друга</Button>
          <p className="small muted own-unlock-alternative">Или набери 500 подписчиков в одном мире.</p>
        </>}</div>
        <div className="profile-world-settings"><Button secondary onClick={() => nav.push('05-11_world_rules')}>Настройки мира</Button></div>
        <Card style={{ marginBottom: 10 }}>
          <div className="row" style={{ justifyContent: 'space-between', marginBottom: 8 }}><b>Навыки · из 10</b><button className="text-button small" onClick={() => setSkillHelp(true)}>как поднять ›</button></div>
          {(Object.keys(SKILL_NAMES) as (keyof typeof SKILL_NAMES)[]).map(k => <SkillBar key={k} name={SKILL_NAMES[k]} level={p.skills[k].level} progress={p.skills[k].progress} />)}
        </Card>
        {runtimeConfig.features.collection && <Card style={{ marginBottom: 10 }}>
          <div className="row" style={{ justifyContent: 'space-between', marginBottom: 8 }}><b>Коллекция {p.collection.filter(c => c.open).length}/12</b><span className="small" style={{ color: 'var(--lilac)' }}>все ›</span></div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(6,1fr)', gap: 6 }}>{p.collection.map(c => <div key={c.id} className={`slot${c.open ? ' on' : ''}`} onClick={() => c.emoji === '👑' && nav.push('03-2_star_card')} style={{ fontSize: c.open ? 22 : 14 }}>{c.open ? c.emoji : '?'}</div>)}</div>
        </Card>}
      </div>
      {skillHelp && <SkillHelp onClose={() => setSkillHelp(false)} />}
      {linkProfile && <LinkProfile player={p} refresh={refresh} onClose={() => setLinkProfile(false)} />}
    </div>
  )
}

// 05-8: настройки: градус драмы, голос канала, обращение, данные, удалить аккаунт
function Settings() {
  const nav = useNav(); const p = usePlayer(); const { patch, resetPlayer } = useStore()
  const personalizeAvatar = useAvatarPersonalization(p.id)
  const [drama, setDrama] = useState<Drama>(p.drama)
  const [del, setDel] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [loggingOut, setLoggingOut] = useState(false)
  const deletePending = useRef(false)
  const [deleteError, setDeleteError] = useState('')
  const deleteAccount = async () => {
    if (deletePending.current) return
    deletePending.current = true; setDeleting(true); setDeleteError('')
    try { await api.deleteAccount(); resetPlayer(); nav.reset('01-2_age') }
    catch { setDeleteError('Не получилось удалить аккаунт. Попробуй ещё раз.') }
    finally { deletePending.current = false; setDeleting(false) }
  }
  const logOut = async () => {
    setLoggingOut(true); setError('')
    try { await api.logout() }
    catch { setError('Не получилось выйти. Попробуй ещё раз.') }
    finally { setLoggingOut(false) }
  }
  const [panel, setPanel] = useState<'support' | 'data' | 'address' | null>(null)
  const [address, setAddress] = useState(p.address)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const openPanel = (value: typeof panel) => {
    // Reopening starts from saved preferences, discarding a cancelled edit.
    if (value === 'address') setAddress(p.address)
    setError(''); setPanel(value)
  }
  const row = (t: string, r: string, last = false, onClick?: () => void) => <button type="button" onClick={onClick} disabled={!onClick || saving} className="row" style={{ width: '100%', textAlign: 'left', color: 'inherit', background: 'none', border: 'none', padding: '12px 0', borderBottom: last ? 'none' : '1px solid var(--line2)' }}><div style={{ flex: 1 }}>{t}</div><span className="muted">{r}</span></button>
  const setD = async (d: Drama) => {
    if (saving) return
    setSaving(true); setError('')
    try { await api.updateWorld({ drama: d }); patch({ drama: d }); setDrama(d); setPanel(null) }
    catch { setError('Не получилось сохранить. Попробуй ещё раз.') }
    finally { setSaving(false) }
  }
  return (
    <div className="screen no-tab">
      <TopBar onBack={nav.back} title="Настройки" />
      <div className="pad">
        <Card style={{ padding: '2px 14px', marginBottom: 12 }}>
          <PushNotifications key={p.id} playerId={p.id} />
          <div style={{ padding: '12px 0' }}><div>🌩 Градус драмы</div><div className="small muted" style={{ margin: '2px 0 8px' }}>Сколько хейта и скандалов в мире. Фильтр 14+ работает всегда</div>
            <Segmented value={drama} onChange={setD} options={[{ id: 'soft', label: 'мягко' }, { id: 'mid', label: 'средне' }, { id: 'hot', label: 'остро' }, { id: 'off', label: 'без хейта' }]} /></div>
        </Card>
        <Card style={{ padding: '2px 14px', marginBottom: 12 }}>
          <InstallApp entry="settings" />
          {row('🎙 Голос канала', `${p.voice} ›`, false, () => nav.push('05-9_voice'))}{row('🙂 Как к тебе обращаться', `${{ she: 'она', he: 'он', nick: 'по нику' }[p.address]} ›`, false, () => openPanel('address'))}
          <AvatarPersonalization enabled={personalizeAvatar} onChange={enabled => setAvatarPersonalization(p.id, enabled)} />
          <div className="row" style={{ marginBottom: 12 }}><Avatar seed={p.handle} size={48} /><span className="small muted">Выбор сохраняется на этом устройстве.</span></div>
          {row('✨ Реки+', '349 ₽ ›', false, () => nav.push('04-2_fake_door'))}{row('📋 Правила публикаций', '›', false, () => nav.push('06-4_rules'))}{row('🔒 Мои данные и правила', 'скоро ›', false, () => openPanel('data'))}{row('✉ Поддержка', 'скоро ›', true, () => openPanel('support'))}
        </Card>
        <button onClick={logOut} disabled={loggingOut} className="card" style={{ width: '100%', textAlign: 'center', marginBottom: 12 }}>{loggingOut ? 'Выходим…' : 'Выйти'}</button>
        <button onClick={() => setDel(true)} className="card" style={{ width: '100%', textAlign: 'center', color: 'var(--accent)', background: 'transparent', border: '1px solid #FF6B8B55' }}>Удалить аккаунт и всё</button>
        <div className="small muted" style={{ textAlign: 'center', marginTop: 8 }}>Удалим канал, переписку и коллекцию. Вернуть будет нельзя</div>
      </div>
      {error && !panel && <div className="pad small" role="alert">{error}</div>}
      {panel && <Sheet onClose={() => { if (!saving) setPanel(null) }}>
        <div style={{ fontSize: 20, fontWeight: 800, marginBottom: 12 }}>{({ support: 'Поддержка', data: 'Мои данные и правила', address: 'Как к тебе обращаться' })[panel]}</div>
        {panel === 'address' && <><Segmented value={address} onChange={value => setAddress(value as typeof address)} options={[{ id: 'she', label: 'она' }, { id: 'he', label: 'он' }, { id: 'nick', label: 'по нику' }]} /><Button disabled={saving} style={{ marginTop: 14, marginBottom: 8 }} onClick={async () => { if (saving) return; setSaving(true); setError(''); try { await api.settings({ address }); patch({ address }); setPanel(null) } catch { setError('Не получилось сохранить. Попробуй ещё раз.') } finally { setSaving(false) } }}>Сохранить</Button></>}
        {(panel === 'support' || panel === 'data') && <p className="muted">Скоро будет. Готовим этот раздел.</p>}
        {error && <p className="small" role="alert">{error}</p>}
        <Button secondary disabled={saving} onClick={() => setPanel(null)}>Закрыть</Button>
      </Sheet>}
      {del && <Sheet onClose={() => { if (!deleting) setDel(false) }}>
        <div style={{ fontSize: 20, fontWeight: 800, marginBottom: 6 }}>Удалить всё?</div>
        <div className="muted" style={{ marginBottom: 16 }}>Канал, переписка со звёздами, коллекция и прогресс удалятся навсегда</div>
        {deleteError && <p role="alert">{deleteError}</p>}
        <Button disabled={deleting} style={{ background: '#C0392B', marginBottom: 8 }} onClick={deleteAccount}>{deleting ? 'Удаляем…' : 'Удалить навсегда'}</Button><Button secondary disabled={deleting} onClick={() => setDel(false)}>Отмена</Button>
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
