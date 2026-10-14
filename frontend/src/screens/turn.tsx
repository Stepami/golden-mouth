import { useEffect, useRef, useState } from 'react'
import { hapticFeedback, shareStory, shareURL } from '@telegram-apps/sdk-react'
import { api } from '../api/client'
import { firstPost, results } from '../api/mock'
import type { CommentT, Post, PostResult, PublicationFailure } from '../api/types'
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
import { TextField } from '../ui/TextField'
import { completePostDraft, draftKey, readPostDraft, savePostDraft } from '../app/postDraft'
import { blankWorld, selectDraftOwner, setWorldDraft } from '../app/worldDraft'


// 02-1: пост, желания дня, «Подскажи» дописывает черновик
function Compose() {
  const nav = useNav(); const p = usePlayer()
  const ideas = contentOf(p.world).ideas
  const key = draftKey(p)
  const [draft, setDraft] = useState(() => {
    const saved = readPostDraft(key)
    return saved.text.length || saved.publicationId ? saved : { text: typeof nav.params.text === 'string' ? nav.params.text : '', ownWords: true }
  })
  const text = draft.text
  const [durable, setDurable] = useState(true)
  const edit = (value: string, fromIdea = false) => {
    const next = { ...draft, text: value, ownWords: fromIdea ? false : draft.ownWords || value !== draft.text }
    setDraft(next); setDurable(savePostDraft(key, next))
  }
  useEffect(() => { setDurable(savePostDraft(key, draft)) }, [key, draft])
  const done = p.wants.filter(w => w.done).length
  return (
    <div className="screen no-tab">
      <TopBar onBack={nav.back} title="Новый пост" right={`⚡ ${p.postsLeft}`} />
      <div className="pad">
        <div className="compose-topic"><span className="small eyebrow">✨ О чём написать</span><p>{p.wants.find(w => !w.done)?.text ?? "Что произошло в твоём мире сегодня?"}</p><span className="small muted">{p.wants.find(w => !w.done)?.reward ?? "Своя история тоже подойдёт."}</span></div><TextField label="Твой пост" value={text} fromIdea={!draft.ownWords} onChange={edit} maxLength={500} rows={6} placeholder="Расскажи, что произошло…" /><div className="compose-ideas"><div className="small muted">Начни с одной из идей</div>{ideas.slice(0, 3).map(idea => <button className="opt" key={idea} disabled={text.length > 0} onClick={() => edit(idea.replace(/[«»]/g, ''), true)}>{idea}</button>)}{text.length > 0 && <p className="small muted">Идею можно дописать своими словами.</p>}</div>
        {!durable && <p className="small muted" role="status">Черновик доступен в этой сессии. Хранилище устройства недоступно.</p>}
        {draft.publicationId && <div className="card"><p>У предыдущей публикации ещё нет подтверждённого результата.</p><button className="btn" onClick={() => nav.replace('02-2_live', { text: draft.submittedText, publicationId: draft.publicationId, ownWords: draft.submittedOwnWords })}>Проверить статус</button></div>}
        <button className="chip" onClick={() => nav.push('06-4_rules')}>Правила публикаций</button>

        <details className="editor-extra"><summary>Все задания дня · {done} из {p.wants.length}</summary><p className="small muted">Темы для поста с дополнительным опытом.</p>
          {p.wants.map((w, i) => (
            <div key={w.id} className="row" style={{ padding: '7px 0', borderBottom: i < p.wants.length - 1 ? '1px solid var(--line2)' : 'none' }}>
              <span>{w.done ? '✅' : '⬜'}</span><span style={{ flex: 1, ...(w.done ? { color: 'var(--muted2)', textDecoration: 'line-through' } : {}) }}>{w.text}</span><span className="small" style={{ color: 'var(--lilac)' }}>{w.reward}</span>
            </div>
          ))}
        </details>

      </div>
      <BottomCTA main="Опубликовать" disabled={!text.trim() || !!draft.publicationId} onMain={() => { if (p.postsLeft > 0) { track('post_publish', { len: text.length, own_words: draft.ownWords }); const publicationId = crypto.randomUUID(); savePostDraft(key, { ...draft, publicationId, submittedText: text, submittedOwnWords: draft.ownWords }); nav.replace('02-2_live', { text, publicationId, ownWords: draft.ownWords }) } else { track('limit_reached'); nav.replace('04-1_limit') } }} />
    </div>
  )
}

// 02-2: живая реакция 3–4 с, потом итог одной карточкой
function Live() {
  const nav = useNav(); const { patch, refresh } = useStore(); const p = usePlayer()
  const text = (nav.params.text as string) ?? firstPost.text
  const [publicationId] = useState(() => (nav.params.publicationId as string) ?? crypto.randomUUID())
  const [floats, setFloats] = useState<{ id: number; emoji: string; x: number }[]>([])
  const [comments, setComments] = useState<CommentT[]>([])
  const [typing, setTyping] = useState<string | null>(null)
  const [phase, setPhase] = useState<'generating' | 'moderating' | 'reacting'>('generating')
  const commentsRef = useRef<CommentT[]>([])
  const reactionsRef = useRef<string[]>([])
  useEffect(() => api.publish(text, publicationId, e => {
    setPhase('reacting')
    if (e.type === 'react') { reactionsRef.current.push(e.emoji); setFloats(f => [...f, { id: Date.now() + Math.random(), emoji: e.emoji, x: 10 + Math.random() * 70 }]) }
    if (e.type === 'comment') { commentsRef.current.push(e.comment); setComments(c => [...c, e.comment]) }
    if (e.type === 'typing') setTyping(e.who)
    if (e.type === 'result') {
      const post: Post = { id: e.result.postId, text, views: e.result.views,
        reactions: [...new Set(reactionsRef.current)].map(emoji => ({ emoji, count: reactionsRef.current.filter(value => value === emoji).length })),
        comments: commentsRef.current, at: 'только что' }
      completePostDraft(draftKey(p), publicationId, text)
      track('post_result', { delta: e.result.delta, viral: e.result.viral, flop: e.result.flop })
      void (async () => {
        let confirmedResult = e.result
        if (apiBaseUrl) {
          const latest = await refresh()
          if (latest) confirmedResult = { ...e.result, completedWants: latest.wants.filter(want => want.done && p.wants.some(before => before.id === want.id && !before.done)).map(({ id, text, reward }) => ({ id, text, reward })) }

        }
        else patch({ postsLeft: Math.max(0, p.postsLeft - 1), subs: Math.max(0, p.subs + e.result.delta) })
        nav.replace(e.result.viral ? '02-5_viral' : e.result.flop ? '02-4_flop' : '02-3_result', { result: confirmedResult, post })
      })()
    }
  }, (state, reason) => { if (state === 'failed') { const saved = readPostDraft(draftKey(p)); if (saved.publicationId === publicationId) savePostDraft(draftKey(p), { text: saved.text, ownWords: saved.ownWords }) }; if (reason?.code === 'crisis') { nav.replace(p.ageBand === '14-17' || !p.ageBand ? '06-2_crisis' : '06-3_crisis_adult'); return }; nav.replace('02-2b_publish_error', { text, publicationId, ownWords: nav.params.ownWords, reason, uncertain: state === 'unknown' }) }, status => {
    if (status === 'ready_for_moderation') setPhase('moderating')
  }, nav.params.ownWords !== false), []) // eslint-disable-line react-hooks/exhaustive-deps
  return (
    <div className="screen">
      <ChannelHead />

      <div className="pad" style={{ position: 'relative' }}>
        <div className="card" style={{ boxShadow: '0 0 0 2px rgba(255,107,139,.6),0 0 44px rgba(255,107,139,.35)', marginBottom: 12 }}>
          <div className="small" style={{ color: 'var(--accent)', marginBottom: 6 }} aria-live="polite">{phase === 'generating' ? 'готовим публикацию' : phase === 'moderating' ? 'проверяем публикацию' : 'только что опубликовано'}</div>{text}
          {phase === 'reacting'
            ? <div className="small muted" role="status" style={{ marginTop: 16 }}>Появляются реакции…</div>
            : <div className="live-current" aria-hidden="true"><span /></div>}
        </div>
        <div style={{ position: 'absolute', right: 14, top: 120, width: 120, height: 200, pointerEvents: 'none' }}>
          {floats.map(f => <span key={f.id} className="float-react" style={{ left: `${f.x}%`, bottom: 0, fontSize: 24 }}>{f.emoji}</span>)}
        </div>
        {comments.map(c => <Comment key={c.id} seed={c.avatar} name={c.author} handle={c.handle} text={c.text} star={c.star} me={c.me} />)}
        {typing && <div className="muted small row" style={{ gap: 8 }}>{typing} печатает…</div>}
      </div>
      <div className="small muted" style={{ position: 'fixed', bottom: 96, left: 0, right: 0, textAlign: 'center' }} aria-live="polite">{phase === 'generating' ? 'звезда готовит ответ' : phase === 'moderating' ? 'скоро появятся реакции' : 'Собираем итог…'}</div>
    </div>
  )
}

function PublishError() {
  const nav = useNav()
  const text = typeof nav.params.text === 'string' ? nav.params.text : firstPost.text.replace(' 🫠', '')
  const uncertain = nav.params.uncertain === true
  const reason = nav.params.reason as PublicationFailure | undefined
  const explanations: Record<string, string> = {
    harassment: 'В тексте есть оскорбления или угрозы. Обсуждай поступки и идеи без нападок на человека.',
    age_restricted_topic: 'В тексте есть тема, недоступная для выбранной возрастной группы. Убери взрослые подробности и попробуй снова.',
    explicit_content: 'Убери откровенные или жестокие подробности и попробуй снова.',
    hate: 'Обсуждай идеи и поступки без разжигания ненависти.',
    dangerous_instructions: 'Убери опасные инструкции и призывы причинить вред.',
  }
  const explanation = uncertain ? 'Связь прервалась. Проверь статус: новый пост создавать не нужно.' : reason?.code === 'unsafe_generated_content' ? 'Ответ игры не прошёл проверку безопасности. Твой текст сохранён, доступный пост не списан.' : reason?.code === 'limit_reached' ? 'Энергия закончилась. Новая появится в 9:00. Черновик сохранён.' : reason?.code === 'technical_failure' ? 'Не получилось подготовить публикацию. Твой текст сохранён, попробуй позже.' : reason?.code === 'content_rejected' ? explanations[reason.category] ?? 'Публикация не прошла проверку. Измени текст и попробуй снова.' : 'Не получилось подтвердить публикацию. Текст сохранён.'
  const publicationId = typeof nav.params.publicationId === 'string' ? nav.params.publicationId : undefined
  return (
    <div className="screen no-tab">
      <ChannelHead />
      <div className="pad" style={{ opacity: .45 }}><div className="card"><div className="small muted">не опубликовано</div>{text}</div></div>
      <div className="sheet-bg" style={{ background: 'rgba(8,8,12,.65)' }}>
        <div className="sheet" role="alert" aria-live="assertive">
          <div style={{ fontSize: 24, fontWeight: 800, marginBottom: 8 }}>{uncertain ? 'Проверим публикацию' : 'Пост не опубликован'}</div>
          <div className="muted" style={{ marginBottom: 20 }}>{explanation}</div>
          {uncertain && publicationId && <Button onClick={() => nav.replace('02-2_live', { text, publicationId, ownWords: nav.params.ownWords })}>Проверить статус</Button>}
          <Button onClick={() => nav.replace('02-1_compose', { text })}>Изменить текст</Button>
          <Button secondary onClick={() => nav.push('06-4_rules')}>Правила публикаций</Button>
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
          <div style={{ fontSize: 18, fontWeight: 800, marginBottom: 12 }}>{viral ? r.headline : r.delta > 0 ? "Твой пост заметили ✨" : r.headline}</div>
          {viral && <div className="confetti" aria-hidden>{Array.from({ length: 18 }, (_, i) => <i key={i} style={{ left: `${(i * 53) % 100}%`, animationDelay: `${(i % 6) * 0.12}s`, background: ['#FF6B8B', '#9B6BFF', '#FFD66B', '#6BFFB8'][i % 4] }} />)}</div>}

          <div className="result-awards" aria-live="polite">{r.completedWants?.map(want => <div className="result-task" key={want.id}><span>✅</span><div><b>Задание выполнено</b><p>{want.text}</p><span className="small">{want.reward}</span></div></div>)}{r.ribbons.map(award => <Ribbon key={award.title} icon={award.icon} title={award.title} sub={award.sub} />)}</div>
          <details className="result-details"><summary>Разбор поста</summary>          <div>{r.whys.map((w, i) => <span key={w.label} className="chip pop" style={{ background: '#22222D', animationDelay: `${0.85 + i * 0.15}s` }}>{({ star: '⭐', brave: '🔥', humor: '😂', topic: '🎯', reach: '👁' } as Record<string, string>)[w.icon] ?? w.icon} {w.label} {(!/^[+−-]/.test(w.value) || explainsDelta) && <b className={/^[+×]/.test(w.value) ? 'up' : 'down'}>{w.value}</b>}</span>)}</div>
          {r.bestComment && <div className="row" style={{ margin: '8px 0 12px' }}><Avatar seed={r.bestComment.avatar} size={28} mode="default" /><span className="small">«{r.bestComment.text}»</span></div>}
          </details>
          {viral ? <>
            <Button onClick={() => nav.push('02-7_share', { post })}>Поделиться залётом</Button>
            <div className="small" style={{ textAlign: 'center', margin: '8px 0', color: 'var(--gift)' }}>Позови друга в свой мир</div>
            <Button secondary onClick={() => nav.replace('03-5_stage')}>Дальше</Button>
          </> : <>
            <Button onClick={() => nav.replace('02-6_thread', { post })}>Посмотреть реакции</Button>
            {theme === 'normal' && <><button onClick={() => nav.push('02-7_share', { post })} aria-label="Поделиться постом" className="result-share">↗ Поделиться</button>
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

// 02-6: один SSE-запрос; ответ и AI-реплики публикуются вместе за 1 энергию.
function Thread() {
  const nav = useNav()
  const player = usePlayer()
  const { patch } = useStore()
  const publishedPost = nav.params.post as Post | undefined
  const postId = typeof nav.params.postId === 'string' ? nav.params.postId : publishedPost?.id
  const stars = contentOf(player.world).stars
  const starIds = stars.map(star => star.id).join('\0')
  const threadKey = `${player.id}:${player.worldInstanceId ?? player.world}:${postId ?? 'mock'}`
  const storageKey = `reki:reply-draft:${threadKey}`
  type Draft = { requestId: string; commentId: string | null; text: string; submitted: boolean }
  const fresh = (): Draft => ({ requestId: crypto.randomUUID(), commentId: null, text: '', submitted: false })
  const readDraft = (): Draft => {
    try {
      const saved = JSON.parse(localStorage.getItem(storageKey) ?? 'null') as Draft | null
      if (saved && typeof saved.requestId === 'string' && saved.requestId.length > 0 && saved.requestId.length <= 64 && typeof saved.text === 'string' && saved.text.length <= 1000 && (saved.commentId === null || typeof saved.commentId === 'string')) return { ...saved, submitted: saved.submitted === true }
    } catch { /* Хранилище может быть недоступно. */ }
    return fresh()
  }
  const [draft, setDraft] = useState<Draft>(readDraft)
  const [post, setPost] = useState<Post | null>(() => publishedPost ?? (postId ? null : firstPost))
  const [load, setLoad] = useState<'ready' | 'loading' | 'error' | 'unavailable'>(() => post ? 'ready' : 'loading')
  const [loadedKey, setLoadedKey] = useState(threadKey)
  const [reloadToken, setReloadToken] = useState(0)
  const [sending, setSending] = useState(false)
  const [preview, setPreview] = useState('')
  const [replyError, setReplyError] = useState('')
  const controller = useRef<AbortController | null>(null)
  const activeKey = useRef(threadKey)
  activeKey.current = threadKey
  if (loadedKey !== threadKey) {
    setLoadedKey(threadKey); setPost(publishedPost ?? (postId ? null : firstPost))
    setLoad(postId ? 'loading' : 'ready'); setDraft(readDraft()); setSending(false); setPreview(''); setReplyError('')
  }
  useEffect(() => {
    if (loadedKey !== threadKey) return
    try { localStorage.setItem(storageKey, JSON.stringify(draft)) } catch { /* optional storage */ }
  }, [draft, loadedKey, threadKey, storageKey])
  useEffect(() => () => { controller.current?.abort() }, [threadKey])
  useEffect(() => {
    if (!postId) return
    let alive = true
    setLoad('loading')
    api.post(postId).then(found => {
      if (!alive) return
      setPost(found); setLoad('ready')
      const saved = found.comments.find(comment => comment.requestId === draft.requestId && (comment.parentCommentId ?? null) === draft.commentId && comment.text === draft.text)
      if (saved) setDraft(fresh())
    }).catch((cause: unknown) => {
      if (alive) setLoad((cause as { status?: number })?.status === 404 ? 'unavailable' : 'error')
    })
    return () => { alive = false }
    // The current draft is compared once when opening/reloading this post.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [postId, threadKey, reloadToken, starIds])
  const postAuthor = stars.find(star => star.id === post?.authorId)
  const isStarComment = (comment?: CommentT) => !!comment?.star && !comment.hidden && stars.some(star => star.id === comment.authorId)
  const target = post?.comments.find(comment => comment.id === draft.commentId)
  const targetAvailable = draft.commentId === null ? !!postAuthor : isStarComment(target)
  const recipient = draft.commentId === null ? postAuthor?.name : target?.author
  const edit = (change: Partial<Pick<Draft, 'text' | 'commentId'>>) => {
    setDraft(current => {
      const next = { ...current, ...change }
      return next.text === current.text && next.commentId === current.commentId
        ? current : { ...next, requestId: crypto.randomUUID(), submitted: false }
    })
    setReplyError('')
  }
  const send = async () => {
    if (!post || !targetAvailable || !draft.text.trim() || sending) return
    if (player.postsLeft < 1 && !draft.submitted) { setReplyError('Энергия закончилась. Новая появится в 9:00.'); return }
    const ctrl = new AbortController()
    controller.current = ctrl
    const key = threadKey
    setSending(true); setReplyError(''); setPreview(''); setDraft(current => ({ ...current, submitted: true }))
    try {
      const event = await api.reply(post.id, draft.commentId, draft.requestId, draft.text, { signal: ctrl.signal,
        onEvent: update => { if (!ctrl.signal.aborted && activeKey.current === key && update.status === 'generating' && update.text) setPreview(update.text) } })
      if (ctrl.signal.aborted || activeKey.current !== key) return
      if (!event.post || event.energyLeft == null || event.subscribers == null) throw new Error('No saved reply returned')
      const { postId: id, ...detail } = event.post
      setPost({ ...detail, id }); patch({ subs: event.subscribers, postsLeft: event.energyLeft })
      setDraft(fresh()); setPreview('')
      try { localStorage.removeItem(storageKey) } catch { /* optional storage */ }
    } catch (cause) {
      if (ctrl.signal.aborted || activeKey.current !== key) return
      const status = (cause as { status?: number })?.status
      setReplyError(status === 409 ? 'Ответ пока не сохранён. Проверь энергию и попробуй снова.' : status === 422 ? 'Так не получится, попробуй по-другому.' : 'Не удалось подтвердить ответ. Попробуй снова: сохранённый ответ не расходует энергию повторно.')
      setPreview('')
    } finally {
      if (!ctrl.signal.aborted && activeKey.current === key) setSending(false)
      if (controller.current === ctrl) controller.current = null
    }
  }
  const tutorialKey = `reki:thread-help:${player.id}`
  const [showHelp, setShowHelp] = useState(() => { try { return localStorage.getItem(tutorialKey) !== 'seen' } catch { return true } })
  const dismissHelp = () => { setShowHelp(false); try { localStorage.setItem(tutorialKey, 'seen') } catch { /* memory fallback */ } }
  const focusReply = (id: string) => { edit({ commentId: id }); requestAnimationFrame(() => document.querySelector<HTMLInputElement>('.thread-reply-input')?.focus()) }
  const goBack = () => { if (postId && !publishedPost) nav.back(); else nav.replace('03-5_stage') }
  if (load !== 'ready' || loadedKey !== threadKey) return (
    <div className="screen no-tab"><TopBar onBack={goBack} title="Реакции" /><div className="pad"><div className="card" role="status">
      {load === 'loading' && <span className="small muted">Загружаем пост…</span>}
      {load === 'error' && <><div>Не получилось открыть пост.</div><Button onClick={() => setReloadToken(value => value + 1)}>Попробовать снова</Button></>}
      {load === 'unavailable' && <div>Этот пост больше не доступен.</div>}
    </div></div></div>
  )
  if (!post) return null
  const ordered: CommentT[] = []
  const visited = new Set<string>()
  const append = (comment: CommentT) => {
    if (visited.has(comment.id)) return
    visited.add(comment.id); ordered.push(comment)
    post.comments.filter(child => child.parentCommentId === comment.id).forEach(append)
  }
  post.comments.filter(comment => !comment.parentCommentId).forEach(append)
  post.comments.forEach(append)
  return (
    <div className="screen no-tab">
      <TopBar title="Реакции" rightPlain right={<button className="thread-exit" onClick={() => nav.replace('03-5_stage')}>В канал →</button>} />
      <div className="pad">
        <div className="card" style={{ marginBottom: 12 }}>{post.text}<div style={{ marginTop: 8 }}>{post.reactions.map(reaction => <span key={reaction.emoji} className="react">{reaction.emoji} {reaction.count}</span>)}</div></div>
        {showHelp && <div className="thread-help"><span className="thread-help-icon">💬</span><div><b>{postAuthor ? 'Обсуди пост с автором' : 'Герой ждёт ответа'}</b><p>{postAuthor ? 'Напиши комментарий к посту или нажми «Ответить» под репликой.' : 'Нажми «Ответить» под репликой и продолжи историю.'}</p><span className="small muted">{postAuthor ? 'Комментарий' : 'Ответ'} · 1 энергия</span></div><button aria-label="Скрыть подсказку" onClick={dismissHelp}>×</button></div>}
        {!showHelp && <div className="small muted" style={{ marginBottom: 16 }}>{postAuthor ? 'Комментарий' : 'Ответ'} · 1 энергия</div>}
        {ordered.map(comment => <div key={comment.id} style={{ marginLeft: Math.min(comment.depth ?? 0, 2) * 14 }}>
          <Comment seed={comment.avatar} name={comment.author} handle={comment.star ? comment.handle : comment.role ?? comment.handle} text={comment.hidden ? 'Комментарий скрыт' : comment.text} star={comment.star} me={comment.me} />
          {!!comment.reactions?.length && <div className="small" style={{ margin: '-6px 0 12px 44px' }}>{comment.reactions.map(reaction => <span key={`${reaction.authorId}:${reaction.emoji}`} title={reaction.author} className="react">{reaction.emoji} {reaction.author}</span>)}</div>}
          {isStarComment(comment) && <button className="small" disabled={sending} onClick={() => focusReply(comment.id)} style={{ display: 'block', margin: '-8px 0 12px 44px', background: 'none', border: 'none', color: draft.commentId === comment.id ? 'var(--accent)' : 'var(--lilac)' }}>{draft.commentId === comment.id ? 'Отвечаешь этому комментарию' : `Ответить герою ${comment.author}`}</button>}
        </div>)}
        {sending && <div className="card" role="status" aria-live="polite"><div className="small muted">{recipient ?? 'Звезда'} готовит ответ…</div><div className="live-current" aria-hidden="true"><span /></div>{preview && <><p>{preview}</p><div className="small muted">Ответ ещё не опубликован</div></>}</div>}
        {targetAvailable && <div className="small muted" style={{ marginBottom: 8 }}>{draft.commentId === null ? 'Комментарий к посту' : 'Отвечаешь'}: {recipient}</div>}
        {postAuthor && draft.commentId !== null && <button disabled={sending} onClick={() => { edit({ commentId: null }); requestAnimationFrame(() => document.querySelector<HTMLInputElement>('.thread-reply-input')?.focus()) }} style={{ background: 'none', border: 'none', color: 'var(--lilac)', marginBottom: 8 }}>К посту</button>}
        {draft.commentId && !targetAvailable && <div className="small muted" role="status" style={{ marginBottom: 8 }}>Этому аккаунту больше нельзя ответить. Выбери другой комментарий — текст ответа сохранён.</div>}
        {replyError && <div className="small" role="alert" style={{ color: 'var(--accent)', marginBottom: 12 }}>{replyError}</div>}
        <Button secondary disabled={sending} onClick={() => nav.replace('02-1_compose')}>Написать следующий пост</Button>
      </div>
      <div className="bottom-cta" style={{ flexDirection: 'row' }}>
        <div className="card" style={{ flex: 1, padding: '12px 14px' }}><input className="thread-reply-input" value={draft.text} onChange={event => edit({ text: event.target.value })} placeholder={targetAvailable ? (draft.commentId === null ? 'Комментарий · 1 энергия' : 'Ответ · 1 энергия') : 'Выбери комментарий звезды'} maxLength={1000} disabled={!targetAvailable || sending} /></div>
        <button className="card thread-reply-send" onClick={send} disabled={sending || !targetAvailable || !draft.text.trim() || (player.postsLeft < 1 && !draft.submitted)} style={{ padding: '14px 18px', background: 'var(--grad)', border: 'none' }} aria-label={draft.submitted ? 'Повторить ответ' : 'Отправить за 1 энергию'}>➤</button>
      </div>
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
