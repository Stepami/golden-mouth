import { useEffect, useRef, useState } from 'react'
import { hapticFeedback, shareStory, shareURL } from '@telegram-apps/sdk-react'
import { api } from '../api/client'
import { firstPost, results } from '../api/mock'
import type { CommentT, Post, PostResult } from '../api/types'
import { useNav } from '../app/nav'
import { usePlayer, useStore } from '../app/store'
import { inTelegram } from '../telegram'
import { Avatar, BottomCTA, Button, Comment, Gift, Ribbon, TopBar, Whisper, fmt, plural } from '../ui'
import { ChannelHead } from './onboarding'
import { track } from '../app/log'
import { contentOf } from '../api/worldContent'
import { apiBaseUrl, runtimeConfig } from '../app/runtimeConfig'
import { useReferralLink } from '../app/useReferralLink'
import { register } from '.'


// 02-1: пост, желания дня, «Подскажи» дописывает черновик
function Compose() {
  const nav = useNav(); const p = usePlayer()
  const ideas = contentOf(p.world).ideas
  const [text, setText] = useState(() => typeof nav.params.text === 'string' ? nav.params.text : '')
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

// 02-2: живая реакция 3–4 с, потом итог одной карточкой
function Live() {
  const nav = useNav(); const { patch, refresh } = useStore(); const p = usePlayer()
  const text = (nav.params.text as string) ?? firstPost.text
  const [publicationId] = useState(() => (nav.params.publicationId as string) ?? crypto.randomUUID())
  const [views, setViews] = useState(0)
  const [floats, setFloats] = useState<{ id: number; emoji: string; x: number }[]>([])
  const [comments, setComments] = useState<CommentT[]>([])
  const [typing, setTyping] = useState<string | null>(null)
  const [toastComment, setToastComment] = useState<CommentT | null>(null)
  const [phase, setPhase] = useState<'generating' | 'moderating' | 'reacting'>('generating')
  const commentsRef = useRef<CommentT[]>([])
  const reactionsRef = useRef<string[]>([])
  useEffect(() => api.publish(text, publicationId, e => {
    setPhase('reacting')
    if (e.type === 'views') setViews(e.views)
    if (e.type === 'react') { reactionsRef.current.push(e.emoji); setFloats(f => [...f, { id: Date.now() + Math.random(), emoji: e.emoji, x: 10 + Math.random() * 70 }]) }
    if (e.type === 'comment') { commentsRef.current.push(e.comment); setComments(c => [...c, e.comment]); setToastComment(e.comment) }
    if (e.type === 'typing') setTyping(e.who)
    if (e.type === 'result') {
      const post: Post = { id: e.result.postId, text, views: e.result.views,
        reactions: [...new Set(reactionsRef.current)].map(emoji => ({ emoji, count: reactionsRef.current.filter(value => value === emoji).length })),
        comments: commentsRef.current, at: 'только что' }
      track('post_result', { delta: e.result.delta, viral: e.result.viral, flop: e.result.flop })
      void (async () => {
        if (apiBaseUrl) await refresh()
        else patch({ postsLeft: Math.max(0, p.postsLeft - 1), subs: Math.max(0, p.subs + e.result.delta) })
        nav.replace(e.result.viral ? '02-5_viral' : e.result.flop ? '02-4_flop' : '02-3_result', { result: e.result, post })
      })()
    }
  }, state => nav.replace('02-2b_publish_error', { text, publicationId, uncertain: state === 'unknown' }), status => {
    if (status === 'ready_for_moderation') setPhase('moderating')
  }), []) // eslint-disable-line react-hooks/exhaustive-deps
  return (
    <div className="screen">
      <ChannelHead />
      {toastComment && <div className="toast"><Avatar seed={toastComment.avatar} size={32} /><div className="small"><b>Новый комментарий от {toastComment.author}</b><div style={{ color: 'var(--soft)' }}>«{toastComment.text}»</div></div></div>}
      <div className="pad" style={{ position: 'relative' }}>
        <div className="card" style={{ boxShadow: '0 0 0 2px rgba(255,107,139,.6),0 0 44px rgba(255,107,139,.35)', marginBottom: 12 }}>
          <div className="small" style={{ color: 'var(--accent)', marginBottom: 6 }} aria-live="polite">{phase === 'generating' ? 'готовим публикацию' : phase === 'moderating' ? 'проверяем публикацию' : 'только что опубликовано'}</div>{text}
          {phase === 'reacting'
            ? <div className="row" style={{ marginTop: 12 }}><span style={{ fontSize: 26, fontWeight: 900 }}>👁 {fmt(views)}</span><span className="up small">растёт</span></div>
            : <div className="live-current" aria-hidden="true"><span /></div>}
        </div>
        <div style={{ position: 'absolute', right: 14, top: 120, width: 120, height: 200, pointerEvents: 'none' }}>
          {floats.map(f => <span key={f.id} className="float-react" style={{ left: `${f.x}%`, bottom: 0, fontSize: 24 }}>{f.emoji}</span>)}
        </div>
        {comments.map(c => <Comment key={c.id} seed={c.avatar} name={c.author} handle={c.handle} text={c.text} star={c.star} onMore={() => nav.push('06-1_report', { id: c.id })} />)}
        {typing && <div className="muted small row" style={{ gap: 8 }}>{typing} печатает…</div>}
      </div>
      <div className="small muted" style={{ position: 'fixed', bottom: 96, left: 0, right: 0, textAlign: 'center' }} aria-live="polite">{phase === 'generating' ? 'звезда готовит ответ' : phase === 'moderating' ? 'скоро появятся реакции' : 'реакции ещё идут · итог через пару секунд'}</div>
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
    if (new URLSearchParams(location.search).has('shot') || window.matchMedia('(prefers-reduced-motion: reduce)').matches) { setV(n); return }
    const t0 = performance.now(); let raf = 0
    const tick = (t: number) => { const k = Math.min(1, (t - t0) / ms); setV(Math.round(n * (1 - Math.pow(1 - k, 3)))); if (k < 1) raf = requestAnimationFrame(tick) }
    raf = requestAnimationFrame(tick); return () => cancelAnimationFrame(raf)
  }, [n, ms])
  return v
}
const haptic = (kind: 'success' | 'warning' | 'light') => { if (!inTelegram) return; try { if (kind === 'light') hapticFeedback.impactOccurred.ifAvailable('light'); else hapticFeedback.notificationOccurred.ifAvailable(kind) } catch { /* вне Telegram */ } }

function ResultCard({ r, post, theme }: { r: PostResult; post: Post; theme: 'normal' | 'flop' | 'viral' }) {
  const nav = useNav()
  const viral = theme === 'viral'
  const shown = useCount(Math.abs(r.delta))
  const additive = r.whys.filter(why => /^[+−-]\d+(?:[.,]\d+)?$/.test(why.value))
  const explainsDelta = additive.length > 0 && additive.reduce((sum, why) => sum + Number(why.value.replace('−', '-').replace(',', '.')), 0) === r.delta
  useEffect(() => { haptic(r.delta >= 0 ? 'success' : 'warning'); const ts = r.whys.map((_, i) => setTimeout(() => haptic('light'), 850 + i * 150)); return () => ts.forEach(clearTimeout) }, [r])
  return (
    <div className="screen no-tab">
      <ChannelHead />
      <div className="pad" style={{ opacity: .45 }}><div className="card"><div className="small muted">только что</div>{post.text}<div style={{ marginTop: 8 }}>{post.reactions.map(reaction => <span key={reaction.emoji} className="react">{reaction.emoji} {reaction.count}</span>)}<span className="small muted" style={{ float: 'right' }}>👁 {fmt(r.views)}</span></div></div></div>
      <div className="sheet-bg" style={{ background: 'rgba(8,8,12,.55)' }}>
        <div className="sheet" style={{ background: viral ? 'linear-gradient(170deg,#1B2A4A,#2A1236 60%,#1A1A24)' : '#1A1A24', position: 'relative' }}>
          <div className="small" style={{ letterSpacing: '.06em', color: viral ? 'var(--gift)' : 'var(--muted2)' }}>{viral ? '🌊 ПОСТ ЗАЛЕТЕЛ В РЕКИ' : 'ИТОГ ПОСТА'}</div>
          <div style={{ fontSize: 64, fontWeight: 900, lineHeight: 1.1 }} className={r.delta >= 0 ? 'up' : 'down'}>{r.delta >= 0 ? '+' : '−'}{fmt(shown)}</div>
          <div className="muted2" style={{ marginBottom: 12 }}>{plural(shown, 'подписчик', 'подписчика', 'подписчиков')}</div>
          <div style={{ fontSize: 18, fontWeight: 800, marginBottom: 12 }}>{r.headline}</div>
          {viral && <div className="confetti" aria-hidden>{Array.from({ length: 18 }, (_, i) => <i key={i} style={{ left: `${(i * 53) % 100}%`, animationDelay: `${(i % 6) * 0.12}s`, background: ['#FF6B8B', '#9B6BFF', '#FFD66B', '#6BFFB8'][i % 4] }} />)}</div>}
          <div>{r.whys.map((w, i) => <span key={w.label} className="chip pop" style={{ background: '#22222D', animationDelay: `${0.85 + i * 0.15}s` }}>{({ star: '⭐', brave: '🔥', humor: '😂', topic: '🎯', reach: '👁' } as Record<string, string>)[w.icon] ?? w.icon} {w.label} {(!/^[+−-]/.test(w.value) || explainsDelta) && <b className={/^[+×]/.test(w.value) ? 'up' : 'down'}>{w.value}</b>}</span>)}</div>
          {(runtimeConfig.features.collection ? r.ribbons : []).map(x => <Ribbon key={x.title} icon={x.icon} title={x.title} sub={x.sub} />)}
          {r.bestComment && <div className="row" style={{ margin: '8px 0 12px' }}><Avatar seed={r.bestComment.avatar} size={28} /><span className="small">«{r.bestComment.text}»</span></div>}
          {theme === 'normal' && <Whisper>Подписчики приходят за смелость, юмор и попадание в тему мира. Смотри, за что дали в этот раз</Whisper>}
          {viral ? <>
            <Button onClick={() => nav.push('02-7_share', { post })}>Поделиться залётом</Button>
            <div className="small" style={{ textAlign: 'center', margin: '8px 0', color: 'var(--gift)' }}>Позови друга в свой мир</div>
            <Button secondary onClick={() => nav.replace('03-5_stage')}>Дальше</Button>
          </> : <>
            <Button onClick={() => nav.replace('02-6_thread', { post })}>Дальше</Button>
            {theme === 'normal' && <><button onClick={() => nav.push('02-7_share', { post })} style={{ display: 'block', margin: '10px auto 4px', background: 'none', border: 'none', color: 'var(--lilac)' }}>Поделиться в чат или сторис ›</button>
              </>}
          </>}
        </div>
      </div>
    </div>
  )
}
const Result = () => { const n = useNav(); const r = (n.params.result as PostResult) ?? results.normal; return <ResultCard r={r} post={(n.params.post as Post) ?? { ...firstPost, id: r.postId, text: (n.params.text as string) ?? firstPost.text }} theme="normal" /> }
const Flop = () => { const n = useNav(); const r = (n.params.result as PostResult) ?? results.flop; return <ResultCard r={r} post={(n.params.post as Post) ?? { ...firstPost, id: r.postId, text: (n.params.text as string) ?? 'сегодня сдавала пробник по ЕГЭ, всё сложно' }} theme="flop" /> }
const Viral = () => { const n = useNav(); const r = (n.params.result as PostResult) ?? results.viral; return <ResultCard r={r} post={(n.params.post as Post) ?? { ...firstPost, id: r.postId, text: (n.params.text as string) ?? firstPost.text }} theme="viral" /> }

// 02-6: тред, первые ответы дают подписчиков, ветка до 2 шагов, потом реакция комментатора.
// Реальная лента приходит с postId: грузим именно этот пост. Fallback на firstPost — только для
// экранов-макетов без id (например, карточка в канале 03-5), не для ленты.
function Thread() {
  const nav = useNav()
  const player = usePlayer()
  const { patch } = useStore()
  const publishedPost = nav.params.post as Post | undefined
  const postId = typeof nav.params.postId === 'string' ? nav.params.postId : undefined
  // Ключ открытого треда: реальный пост из ленты, только что опубликованный или мок-карточка.
  const threadKey = postId ?? (publishedPost ? `own:${publishedPost.id}` : 'mock')
  const [post, setPost] = useState<Post | null>(() => publishedPost ?? (postId ? null : firstPost))
  const [load, setLoad] = useState<'ready' | 'loading' | 'error' | 'unavailable'>(() => (publishedPost || !postId ? 'ready' : 'loading'))
  const [loadedKey, setLoadedKey] = useState(threadKey)
  const [reloadToken, setReloadToken] = useState(0)
  const [reply, setReply] = useState('')
  const [targetId, setTargetId] = useState<string | null>(null)
  const [mine, setMine] = useState<CommentT[]>([])
  const [reactions, setReactions] = useState<Record<string, string>>({})
  const [awarded, setAwarded] = useState(0)
  const [limit, setLimit] = useState<number | null>(null)
  const [bonus, setBonus] = useState<number | null>(null)
  const [sending, setSending] = useState(false)
  const [replyError, setReplyError] = useState('')
  // replyId стабилен для одного черновика: при ошибке повтор идёт с тем же ключом, после успеха сбрасывается
  const [replyId, setReplyId] = useState(() => crypto.randomUUID())
  const draftKey = `${targetId ?? ''}|${reply}`
  const draftKeyRef = useRef(draftKey)
  // Пост сменился (навигация/повторное открытие): стираем прошлый тред и его черновик до отрисовки нового,
  // чтобы не показать и не отправить чужой пост.
  if (loadedKey !== threadKey) {
    setLoadedKey(threadKey)
    setPost(publishedPost ?? (postId ? null : firstPost))
    setLoad(publishedPost || !postId ? 'ready' : 'loading')
    setReply('')
    setTargetId(null)
    setMine([])
    setReactions({})
    setAwarded(0)
    setLimit(null)
    setBonus(null)
    setSending(false)
    setReplyError('')
    setReplyId(crypto.randomUUID())
  }
  useEffect(() => {
    if (draftKeyRef.current !== draftKey) { draftKeyRef.current = draftKey; setReplyId(crypto.randomUUID()) }
  }, [draftKey])
  useEffect(() => {
    if (publishedPost || !postId) return
    let alive = true
    setLoad('loading')
    api.post(postId)
      .then(found => { if (alive) { setPost(found); setLoad('ready') } })
      .catch((error: unknown) => {
        if (!alive) return
        const status = (error as { status?: number } | null)?.status
        setLoad(status === 404 ? 'unavailable' : 'error')
      })
    return () => { alive = false }
  }, [postId, publishedPost, reloadToken])
  const rewardedReplies = !!apiBaseUrl && (!!publishedPost || post?.canReply === true)
  const demoComments = runtimeConfig.features.mockFeed && (post?.demoComments === true || !apiBaseUrl || post?.comments.length === 0)
  const canReply = rewardedReplies || demoComments
  const target = post ? [...post.comments, ...mine].find(comment => comment.id === targetId) : undefined
  const send = async () => {
    if (!post || !reply.trim() || (!targetId && !demoComments) || sending) return
    if (apiBaseUrl && !publishedPost && !postId) { setReplyError('Открой опубликованный пост, чтобы ответить на него.'); return }
    setSending(true)
    setReplyError('')
    try {
      if (demoComments && (!rewardedReplies || !targetId || !apiBaseUrl)) {
        const comment = await api.demoComment(post.id, player, reply, replyId)
        setMine(comments => [...comments, comment])
        setReply(''); setTargetId(null)
        return
      }
      if (!targetId) return
      const result = await api.reply(post.id, targetId, replyId, reply)
      setMine(comments => [...comments, { id: result.commentId, author: player.name, avatar: player.handle, handle: player.handle, text: reply, me: true }])
      setAwarded(result.awardedReplies)
      setLimit(result.replyLimit)
      setBonus(result.subscriberBonus)
      const reaction = result.reaction
      if (reaction) setReactions(prev => ({ ...prev, [result.commentId]: reaction }))
      patch({ subs: result.subscriberTotal })
      setReply('')
      setTargetId(null)
    } catch {
      setReplyError('Ответ не отправился. Попробуй ещё раз.')
    } finally {
      setSending(false)
    }
  }
  const goBack = () => { if (postId && !publishedPost) nav.back(); else nav.replace('03-5_stage') }
  if (load !== 'ready') {
    return (
      <div className="screen no-tab">
        <TopBar onBack={goBack} title="Реакции" />
        <div className="pad">
          <div className="card" role={load === 'error' ? 'alert' : 'status'} aria-live="polite">
            {load === 'loading' && <div className="small muted">загружаем пост…</div>}
            {load === 'error' && <><div className="small">Не получилось открыть пост.</div><Button style={{ marginTop: 12 }} onClick={() => setReloadToken(token => token + 1)}>Попробовать снова</Button></>}
            {load === 'unavailable' && <><div className="small">Этот пост больше не доступен.</div><div className="small muted" style={{ marginTop: 6 }}>Возможно, его удалили или он из другого мира.</div></>}
          </div>
        </div>
      </div>
    )
  }
  if (!post) return null
  return (
    <div className="screen no-tab">
      <TopBar onBack={goBack} title="Реакции" right={`👁 ${fmt(post.views)} ↑`} />
      <div className="pad">
        <div className="card" style={{ marginBottom: 12 }}>{post.text}<div style={{ marginTop: 8 }}>{post.reactions.map(r => <span key={r.emoji} className="react">{r.emoji} {r.count}</span>)}</div></div>
        {demoComments && !rewardedReplies
          ? <div className="small muted" style={{ marginBottom: 12 }}>Демо-комментарии сохраняются на этом устройстве. Подписчики за них не начисляются.</div>
          : rewardedReplies
          ? <div className="small" style={{ color: 'var(--gift)', marginBottom: 12 }}>💬 Ответь на комментарии: первые ответы +подписчики{limit != null ? ` · ${awarded}/${limit}` : ''}</div>
          : <div className="small muted" style={{ marginBottom: 12 }}>Чужой пост · отвечать можно только под своими</div>}
        {bonus != null && bonus > 0 && <Gift style={{ marginBottom: 12 }}>+{bonus} подписчиков за ответ</Gift>}
        {post.comments.map(c => (
          <div key={c.id}>
            <Comment seed={c.avatar} name={c.author} handle={c.handle} text={c.text} star={c.star} me={c.me} onMore={c.me ? undefined : () => nav.push('06-1_report', { id: c.id })} />
            {canReply && !c.me && <button className="small" onClick={() => setTargetId(c.id)} style={{ display: 'block', margin: '-8px 0 12px 44px', background: 'none', border: 'none', color: targetId === c.id ? 'var(--accent)' : 'var(--lilac)' }}>{targetId === c.id ? 'отвечаешь этому комментарию' : 'Ответить'}</button>}
          </div>
        ))}
        {mine.map(c => (
          <div key={c.id}>
            <Comment seed={c.avatar} name={c.author} handle={c.handle} text={c.text} star={c.star} me={c.me} />
            {reactions[c.id] && <div className="small" style={{ margin: '-6px 0 12px 44px', color: 'var(--accent)' }}>реакция комментатора: {reactions[c.id]}</div>}
          </div>
        ))}
        {target && <div className="small muted" style={{ marginBottom: 8 }}>Отвечаешь: {target.author}</div>}
        {replyError && <div className="small" role="alert" style={{ color: 'var(--accent)', marginBottom: 12 }}>{replyError}</div>}
      </div>
      {canReply && (
        <div className="bottom-cta" style={{ flexDirection: 'row' }}>
          <div className="card" style={{ flex: 1, padding: '12px 14px' }}><input className="thread-reply-input" value={reply} onChange={e => setReply(e.target.value)} placeholder={targetId ? 'Ответ · бесплатно' : demoComments ? 'Твой комментарий' : 'Выбери комментарий'} maxLength={300} disabled={(!targetId && !demoComments) || sending} /></div>
          <button className="card thread-reply-send" onClick={send} disabled={sending || !reply.trim() || (!targetId && !demoComments)} style={{ padding: '14px 18px', background: 'var(--grad)', border: 'none' }} aria-label="Отправить">➤</button>
        </div>
      )}
    </div>
  )
}

// 02-7: «Отправить в чат» через нативный шеринг Telegram или в сторис. Чаты игрока мы не видим.
function Share() {
  const nav = useNav()
  const player = usePlayer()
  const { link, error: linkError, retry } = useReferralLink()
  const post = (nav.params.post as Post | undefined) ?? firstPost
  const text = `Мой пост в Реках 🌊 А ты кем будешь в ${contentOf(player.world).where}?`
  const toChat = () => { if (!link) return; track('share', { to: 'chat' }); if (inTelegram) shareURL.ifAvailable(link, text); else window.open(`https://t.me/share/url?url=${encodeURIComponent(link)}&text=${encodeURIComponent(text)}`) }
  // Публичную картинку 9:16 Telegram загружает без заголовков авторизации.
  const storyCardUrl = new URL(`${apiBaseUrl ?? ''}/cards/story/${encodeURIComponent(player.id)}/${encodeURIComponent(post.id)}.png`, location.origin).toString()
  const toStory = () => { if (!link) return; track('share', { to: 'story' }); if (inTelegram) shareStory.ifAvailable(storyCardUrl, { text, widgetLink: { url: link, name: 'Играть' } }) }
  return (
    <div className="screen no-tab">
      <TopBar onBack={nav.back} title="Поделиться" />
      <div className="pad">
        <div style={{ background: 'linear-gradient(160deg,#2A1236,#141420)', borderRadius: 14, padding: 14, marginBottom: 12 }}>
          <div className="small" style={{ color: 'var(--gift)' }}>РЕКИ · ПОСТ ЗАЛЕТЕЛ 🌊</div>
          <div style={{ margin: '8px 0' }}>{post.text}</div>
          <div className="small muted">👁 {fmt(post.views)}</div>
          <div className="small" style={{ marginTop: 8 }}>а ты кем будешь в {contentOf(player.world).where}? <b>▶ Играть</b></div>
        </div>
        <Gift style={{ marginBottom: 12 }}>Позови друга в Реки</Gift>
        <div className="small muted">Telegram откроет список твоих чатов. Мы его не видим и никому не пишем сами.</div>
      </div>
      {linkError && <div className="pad" role="alert">Не удалось получить твою ссылку.<Button secondary onClick={retry}>Попробовать снова</Button></div>}
      <BottomCTA disabled={!link} secondDisabled={!link} second="＋ В сторис" onSecond={toStory} main="Отправить в чат" onMain={toChat} />
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
