import { useEffect, useState } from 'react'
import { hapticFeedback, shareStory, shareURL } from '@telegram-apps/sdk-react'
import { api } from '../api/client'
import { firstPost, ideas, liveComment, results } from '../api/mock'
import type { CommentT, PostResult } from '../api/types'
import { useNav } from '../app/nav'
import { usePlayer, useStore } from '../app/store'
import { inTelegram } from '../telegram'
import { Avatar, BottomCTA, Button, Comment, Gift, Ribbon, TopBar, Whisper, fmt, plural } from '../ui'
import { ChannelHead } from './onboarding'
import { track } from '../app/log'
import { apiBaseUrl } from '../app/runtimeConfig'
import { telegramStartUrl } from '../app/telegramLinks'
import { register } from '.'

const LINK = telegramStartUrl('liza7')

// 02-1: пост, желания дня, «Подскажи» дописывает черновик
function Compose() {
  const nav = useNav(); const p = usePlayer()
  const [text, setText] = useState(() => typeof nav.params.text === 'string' ? nav.params.text : firstPost.text.replace(' 🫠', ''))
  const done = p.wants.filter(w => w.done).length
  return (
    <div className="screen no-tab">
      <TopBar onBack={nav.back} title="Новый пост" right={`✍️ осталось ${p.postsLeft} на сегодня`} />
      <div className="pad">
        <div className="card" style={{ minHeight: 104, marginBottom: 6 }}>
          <textarea value={text} onChange={e => setText(e.target.value)} rows={4} placeholder="Как прошёл день?" maxLength={500} />
        </div>
        <Whisper>Не знаешь, что писать? Возьми идею и допиши по-своему. Своими словами подписчиков приходит на треть больше</Whisper>
        <div className="card" style={{ padding: '6px 14px', margin: '6px 0 10px' }}>
          <div className="small muted" style={{ paddingTop: 6 }}>ЖЕЛАНИЯ ДНЯ · {done} из {p.wants.length}</div>
          {p.wants.map((w, i) => (
            <div key={w.id} className="row" style={{ padding: '7px 0', borderBottom: i < p.wants.length - 1 ? '1px solid var(--line2)' : 'none' }}>
              <span>{w.done ? '✅' : '⬜'}</span><span style={{ flex: 1, ...(w.done ? { color: 'var(--muted2)', textDecoration: 'line-through' } : {}) }}>{w.text}</span><span className="small" style={{ color: 'var(--lilac)' }}>{w.reward}</span>
            </div>
          ))}
        </div>
        <div className="small muted" style={{ margin: '4px 0 8px' }}>💡 ПОДСКАЖИ</div>
        {ideas.map(i => <button key={i} className="opt" onClick={() => setText(i.replace(/[«»]/g, '').replace('…', ' '))}>{i}</button>)}
      </div>
      <BottomCTA main="Опубликовать" disabled={!text.trim()} onMain={() => { if (p.postsLeft > 0) { track('post_publish', { len: text.length, own_words: !ideas.some(i => text.startsWith(i.replace(/[«»…]/g, '').trim())) }); nav.replace('02-2_live', { text, publicationId: crypto.randomUUID() }) } else { track('limit_reached'); nav.replace('04-1_limit') } }} />
    </div>
  )
}

// 02-2: живая реакция 2–3 с, потом итог одной карточкой
function Live() {
  const nav = useNav(); const { patch } = useStore(); const p = usePlayer()
  const text = (nav.params.text as string) ?? firstPost.text
  const [publicationId] = useState(() => (nav.params.publicationId as string) ?? crypto.randomUUID())
  const [views, setViews] = useState(0)
  const [floats, setFloats] = useState<{ id: number; emoji: string; x: number }[]>([])
  const [comments, setComments] = useState<CommentT[]>([])
  const [typing, setTyping] = useState<string | null>(null)
  const [toast, setToast] = useState(false)
  const [published, setPublished] = useState(false)
  useEffect(() => api.publish(text, publicationId, e => {
    setPublished(true)
    if (e.type === 'views') setViews(e.views)
    if (e.type === 'react') setFloats(f => [...f, { id: Date.now() + Math.random(), emoji: e.emoji, x: 10 + Math.random() * 70 }])
    if (e.type === 'comment') { setComments(c => [...c, e.comment]); setToast(true) }
    if (e.type === 'typing') setTyping(e.who)
    if (e.type === 'result') { track('post_result', { delta: e.result.delta, viral: e.result.viral, flop: e.result.flop }); patch({ postsLeft: Math.max(0, p.postsLeft - 1), subs: p.subs + e.result.delta }); nav.replace(e.result.viral ? '02-5_viral' : e.result.flop ? '02-4_flop' : '02-3_result', { result: e.result, text }) }
  }, state => nav.replace('02-2b_publish_error', { text, publicationId, uncertain: state === 'unknown' })), []) // eslint-disable-line react-hooks/exhaustive-deps
  return (
    <div className="screen">
      <ChannelHead />
      {toast && <div className="toast"><Avatar id="mira" size={32} /><div className="small"><b>Мира прокомментировала</b><div style={{ color: 'var(--soft)' }}>«„уверенно“ это комплимент, поверь»</div></div></div>}
      <div className="pad" style={{ position: 'relative' }}>
        <div className="card" style={{ boxShadow: '0 0 0 2px rgba(255,107,139,.6),0 0 44px rgba(255,107,139,.35)', marginBottom: 12 }}>
          <div className="small" style={{ color: 'var(--accent)', marginBottom: 6 }}>{published ? 'только что опубликовано' : 'готовим публикацию'}</div>{text}
          <div className="row" style={{ marginTop: 12 }}><span style={{ fontSize: 26, fontWeight: 900 }}>👁 {fmt(views)}</span><span className="up small">растёт</span></div>
        </div>
        <div style={{ position: 'absolute', right: 14, top: 120, width: 120, height: 200, pointerEvents: 'none' }}>
          {floats.map(f => <span key={f.id} className="float-react" style={{ left: `${f.x}%`, bottom: 0, fontSize: 24 }}>{f.emoji}</span>)}
        </div>
        {comments.map(c => <Comment key={c.id} avatar={c.avatar} name={c.author} handle={c.handle} text={c.text} star={c.star} onMore={() => nav.push('06-1_report', { id: c.id })} />)}
        {typing && <div className="muted small row" style={{ gap: 8 }}><Avatar id="sonya" size={22} /> {typing} печатает…</div>}
      </div>
      <div className="small muted" style={{ position: 'fixed', bottom: 96, left: 0, right: 0, textAlign: 'center' }}>{published ? 'реакции ещё идут · итог через пару секунд' : 'звезда готовит ответ'}</div>
    </div>
  )
}

function PublishError() {
  const nav = useNav()
  const text = typeof nav.params.text === 'string' ? nav.params.text : firstPost.text.replace(' 🫠', '')
  const uncertain = nav.params.uncertain === true
  const publicationId = typeof nav.params.publicationId === 'string' ? nav.params.publicationId : undefined
  return (
    <div className="screen no-tab">
      <ChannelHead />
      <div className="pad" style={{ opacity: .45 }}><div className="card"><div className="small muted">не опубликовано</div>{text}</div></div>
      <div className="sheet-bg" style={{ background: 'rgba(8,8,12,.65)' }}>
        <div className="sheet" role="alert" aria-live="assertive">
          <div style={{ fontSize: 24, fontWeight: 800, marginBottom: 8 }}>{uncertain ? 'Проверим публикацию' : 'Пост не вышел'}</div>
          <div className="muted" style={{ marginBottom: 20 }}>{uncertain ? 'Связь прервалась. Текст сохранён; проверь статус, когда будешь готов.' : 'Текст сохранён. Публикация не списала доступный пост.'}</div>
          {uncertain && publicationId && <Button onClick={() => nav.replace('02-2_live', { text, publicationId })}>Проверить статус</Button>}
          <Button onClick={() => nav.replace('02-1_compose', { text })}>Вернуться в редактор</Button>
        </div>
      </div>
    </div>
  )
}

/** Итог поста одной карточкой поверх канала: обычный, провал, залёт (праздничная тема). */
/** Отсчёт числа 0 → n за 0,8 с (UX-аудит: «число считается, чипы по одному, haptic»). */
function useCount(n: number, ms = 800) {
  const [v, setV] = useState(0)
  useEffect(() => {
    if (new URLSearchParams(location.search).has('shot')) { setV(n); return }
    const t0 = performance.now(); let raf = 0
    const tick = (t: number) => { const k = Math.min(1, (t - t0) / ms); setV(Math.round(n * (1 - Math.pow(1 - k, 3)))); if (k < 1) raf = requestAnimationFrame(tick) }
    raf = requestAnimationFrame(tick); return () => cancelAnimationFrame(raf)
  }, [n, ms])
  return v
}
const haptic = (kind: 'success' | 'warning' | 'light') => { if (!inTelegram) return; try { if (kind === 'light') hapticFeedback.impactOccurred.ifAvailable('light'); else hapticFeedback.notificationOccurred.ifAvailable(kind) } catch { /* вне Telegram */ } }

function ResultCard({ r, text, theme }: { r: PostResult; text: string; theme: 'normal' | 'flop' | 'viral' }) {
  const nav = useNav()
  const viral = theme === 'viral'
  const shown = useCount(Math.abs(r.delta))
  useEffect(() => { haptic(r.delta >= 0 ? 'success' : 'warning'); const ts = r.whys.map((_, i) => setTimeout(() => haptic('light'), 850 + i * 150)); return () => ts.forEach(clearTimeout) }, [r])
  return (
    <div className="screen no-tab">
      <ChannelHead />
      <div className="pad" style={{ opacity: .45 }}><div className="card"><div className="small muted">только что</div>{text}<div style={{ marginTop: 8 }}><span className="react">😂 48</span><span className="react">🫶 21</span><span className="small muted" style={{ float: 'right' }}>👁 {fmt(r.views)}</span></div></div></div>
      <div className="sheet-bg" style={{ background: 'rgba(8,8,12,.55)' }}>
        <div className="sheet" style={{ background: viral ? 'linear-gradient(170deg,#1B2A4A,#2A1236 60%,#1A1A24)' : '#1A1A24', position: 'relative' }}>
          <div className="small" style={{ letterSpacing: '.06em', color: viral ? 'var(--gift)' : 'var(--muted2)' }}>{viral ? '🌊 ПОСТ ЗАЛЕТЕЛ В РЕКИ' : 'ИТОГ ПОСТА'}</div>
          <div style={{ fontSize: 64, fontWeight: 900, lineHeight: 1.1 }} className={r.delta >= 0 ? 'up' : 'down'}>{r.delta >= 0 ? '+' : '−'}{fmt(shown)}</div>
          <div className="muted2" style={{ marginBottom: 12 }}>{plural(r.delta, 'подписчик', 'подписчика', 'подписчиков')}</div>
          <div style={{ fontSize: 18, fontWeight: 800, marginBottom: 12 }}>{r.headline}</div>
          {viral && <div className="confetti" aria-hidden>{Array.from({ length: 18 }, (_, i) => <i key={i} style={{ left: `${(i * 53) % 100}%`, animationDelay: `${(i % 6) * 0.12}s`, background: ['#FF6B8B', '#9B6BFF', '#FFD66B', '#6BFFB8'][i % 4] }} />)}</div>}
          <div>{r.whys.map((w, i) => <span key={w.label} className="chip pop" style={{ background: '#22222D', animationDelay: `${0.85 + i * 0.15}s` }}>{w.icon} {w.label} <b className={/^[+×]/.test(w.value) ? 'up' : 'down'}>{w.value}</b></span>)}</div>
          {r.ribbons.map(x => <Ribbon key={x.title} icon={x.icon} title={x.title} sub={x.sub} />)}
          {r.bestComment && <div className="row" style={{ margin: '8px 0 12px' }}><Avatar id={r.bestComment.avatar} size={28} /><span className="small">«{r.bestComment.text}»</span></div>}
          {theme === 'normal' && <Whisper>Подписчики приходят за смелость, юмор и попадание в тему мира. Смотри, за что дали в этот раз</Whisper>}
          {viral ? <>
            <Button onClick={() => nav.push('02-7_share')}>Поделиться залётом</Button>
            <div className="small" style={{ textAlign: 'center', margin: '8px 0', color: 'var(--gift)' }}>🎁 друг зайдёт: +5 постов вам обоим</div>
            <Button secondary onClick={() => nav.replace('03-5_stage')}>Дальше</Button>
          </> : <>
            <Button onClick={() => nav.replace('02-6_thread')}>Дальше</Button>
            {theme === 'normal' && <><button onClick={() => nav.push('02-7_share')} style={{ display: 'block', margin: '10px auto 4px', background: 'none', border: 'none', color: 'var(--lilac)' }}>Поделиться в чат или сторис ›</button>
              <div className="small" style={{ textAlign: 'center', color: 'var(--gift)' }}>🎁 друг зайдёт по карточке: +5 постов вам обоим</div></>}
          </>}
        </div>
      </div>
    </div>
  )
}
const Result = () => { const n = useNav(); return <ResultCard r={(n.params.result as PostResult) ?? results.normal} text={(n.params.text as string) ?? firstPost.text} theme="normal" /> }
const Flop = () => { const n = useNav(); return <ResultCard r={(n.params.result as PostResult) ?? results.flop} text={(n.params.text as string) ?? 'сегодня сдавала пробник по ЕГЭ, всё сложно'} theme="flop" /> }
const Viral = () => { const n = useNav(); return <ResultCard r={(n.params.result as PostResult) ?? results.viral} text={(n.params.text as string) ?? firstPost.text} theme="viral" /> }

// 02-6: тред, первые 3 ответа дают подписчиков, ветка до 2 шагов
function Thread() {
  const nav = useNav()
  const [reply, setReply] = useState('')
  const [mine, setMine] = useState<CommentT[]>([])
  const replied = 1 + mine.length
  const send = async () => { if (!reply.trim()) return; await api.reply(firstPost.id, reply); setMine(m => [...m, { id: `m${m.length}`, author: 'Лиза · трейни', avatar: 'liza', handle: '', text: reply, me: true }]); setReply('') }
  return (
    <div className="screen no-tab">
      <TopBar onBack={() => nav.replace('03-5_stage')} title="Реакции" right={`👁 ${firstPost.views} ↑`} />
      <div className="pad">
        <div className="card" style={{ marginBottom: 12 }}>{firstPost.text}<div style={{ marginTop: 8 }}>{firstPost.reactions.map(r => <span key={r.emoji} className="react">{r.emoji} {r.count}</span>)}</div></div>
        <div className="small" style={{ color: 'var(--gift)', marginBottom: 12 }}>💬 Ответь на комментарии: первые 3 ответа +подписчики · {Math.min(3, replied)}/3</div>
        {firstPost.comments.slice(0, 3).map(c => <Comment key={c.id} avatar={c.avatar} name={c.author} handle={c.handle} text={c.text} star={c.star} me={c.me} onMore={c.me ? undefined : () => nav.push('06-1_report', { id: c.id })} />)}
        <div className="small muted" style={{ margin: '-6px 0 12px 44px' }}>ответить ещё раз · ветка до 2 ответов · <span className="up">+9 подписчиков</span></div>
        {[firstPost.comments[3], liveComment as CommentT, ...mine].map(c => <Comment key={c.id} avatar={c.avatar} name={c.author} handle={c.handle} text={c.text} star={c.star} me={c.me} onMore={() => nav.push('06-1_report')} />)}
        <div className="muted small row" style={{ gap: 8 }}><Avatar id="eva" size={22} /> Ева печатает…</div>
      </div>
      <div className="bottom-cta" style={{ flexDirection: 'row' }}>
        <div className="card" style={{ flex: 1, padding: '12px 14px' }}><input value={reply} onChange={e => setReply(e.target.value)} placeholder="Ответить · бесплатно" maxLength={300} /></div>
        <button className="card" onClick={send} style={{ padding: '14px 18px', background: 'var(--grad)', border: 'none' }} aria-label="Отправить">➤</button>
      </div>
    </div>
  )
}

// 02-7: «Отправить в чат» через нативный шеринг Telegram или в сторис. Чаты игрока мы не видим.
function Share() {
  const nav = useNav()
  const text = 'Мой пост залетел в Реки 🌊 А ты кем будешь в Сцене?'
  const toChat = () => { track('share', { to: 'chat' }); if (inTelegram) shareURL.ifAvailable(LINK, text); else window.open(`https://t.me/share/url?url=${encodeURIComponent(LINK)}&text=${encodeURIComponent(text)}`) }
  // Картинку 9:16 рисует бэкенд (#10): GET /cards/story/<postId>.png
  const storyCardUrl = new URL(`${apiBaseUrl ?? ''}/cards/story/${firstPost.id}.png`, location.origin).toString()
  const toStory = () => { track('share', { to: 'story' }); if (inTelegram) shareStory.ifAvailable(storyCardUrl, { text, widgetLink: { url: LINK, name: 'Играть' } }) }
  return (
    <div className="screen no-tab">
      <TopBar onBack={nav.back} title="Поделиться" />
      <div className="pad">
        <div style={{ background: 'linear-gradient(160deg,#2A1236,#141420)', borderRadius: 14, padding: 14, marginBottom: 12 }}>
          <div className="small" style={{ color: 'var(--gift)' }}>РЕКИ · ПОСТ ЗАЛЕТЕЛ 🌊</div>
          <div style={{ margin: '8px 0' }}>{firstPost.text}</div>
          <div className="small muted">👁 48k · Мира сделала репост</div>
          <div className="small" style={{ marginTop: 8 }}>а ты кем будешь в Сцене? <b>▶ Играть</b></div>
        </div>
        <Gift style={{ marginBottom: 12 }}>Кто зайдёт по карточке: +5 постов вам обоим</Gift>
        <div className="small muted">Telegram откроет список твоих чатов. Мы его не видим и никому не пишем сами.</div>
      </div>
      <BottomCTA second="＋ В сторис" onSecond={toStory} main="Отправить в чат" onMain={toChat} />
    </div>
  )
}

register({
  '02-1_compose': { component: Compose, title: 'Новый пост' },
  '02-2_live': { component: Live, title: 'Живая реакция' },
  '02-2b_publish_error': { component: PublishError, title: 'Ошибка публикации' },
  '02-3_result': { component: Result, title: 'Итог поста' },
  '02-4_flop': { component: Flop, title: 'Провал' },
  '02-5_viral': { component: Viral, title: 'Залёт' },
  '02-6_thread': { component: Thread, title: 'Тред' },
  '02-7_share': { component: Share, title: 'Поделиться' },
}, { '02-2_live': 0 })
