import { rememberResult, resultHistory } from '../app/resultHistory'
import { PublicationResult } from '../ui/PublicationResult'
import { SKILLS } from '../ui/GameHeader'
import { EnergyHelp } from '../ui/GameSheets'
import '../ui/results-parity.css'
import { usePresetGoal } from '../ui/StoryGoal'
import { Icon } from '../ui/Icon'
import { useEffect, useRef, useState } from 'react'
import { hapticFeedback, shareStory, shareURL } from '@tma.js/sdk'
import { api } from '../api/client'
import { firstPost, results } from '../api/mock'
import type { CommentT, Post, PostResult, PublicationFailure } from '../api/types'
import { useNav } from '../app/nav'
import { usePlayer, useStore } from '../app/store'
import { inTelegram } from '../telegram'
import { Avatar, BottomCTA, Button, Comment, Gift, TopBar, EnergyBadge, Sheet, fmt } from '../ui'
import { ChannelHead } from './onboarding'
import { track } from '../app/log'
import { characterGuidanceFor } from '../app/characterGuidance'
import { contentOf, worldOf, worldDocumentOf } from '../api/worldContent'
import { apiBaseUrl, runtimeConfig } from '../app/runtimeConfig'
import { useReferralLink } from '../app/useReferralLink'
import { register } from '.'
import { TextField } from '../ui/TextField'
import { completePostDraft, draftKey, readPostDraft, savePostDraft, type PostDraft } from '../app/postDraft'
import { blankWorld, selectDraftOwner, setWorldDraft } from '../app/worldDraft'


// 02-1: пост, желания дня, «Подскажи» дописывает черновик
function Compose() {
  const nav = useNav(); const p = usePlayer()
  const [energyHelp, setEnergyHelp] = useState(false)
  const goal = usePresetGoal(p.world)
  const guidance = characterGuidanceFor(p, worldDocumentOf(p.world))
  const firstStory = nav.params.firstStory === true
  const key = draftKey(p)
  const [draft, setDraft] = useState<PostDraft>(() => {
    const saved = readPostDraft(key)
    return saved.text.length || saved.publicationId || saved.wantId ? saved : { text: typeof nav.params.text === 'string' ? nav.params.text : '', ownWords: true }
  })
  const text = draft.text
  const [durable, setDurable] = useState(true)
  const edit = (value: string, fromIdea = false) => {
    const next = { ...draft, text: value, ownWords: fromIdea ? false : draft.ownWords || value !== draft.text }
    setDraft(next); setDurable(savePostDraft(key, next))
  }
  useEffect(() => { setDurable(savePostDraft(key, draft)) }, [key, draft])
  const publish = () => {
    if (!text.trim() || draft.publicationId) return
    if (p.postsLeft < 1) { track('limit_reached'); nav.replace('04-1_limit'); return }
    track('post_publish', { len: text.length, own_words: draft.ownWords })
    const publicationId = crypto.randomUUID()
    savePostDraft(key, { ...draft, publicationId, submittedText: text, submittedOwnWords: draft.ownWords, submittedWantId: undefined })
    nav.replace('02-2_live', { text, publicationId, ownWords: draft.ownWords, wantId: undefined })
  }
  if (firstStory) return <div className="screen no-tab story-writing-page">
    <header><button className="icon-button" aria-label="К завязке" onClick={nav.canBack ? nav.back : () => nav.replace('01-6_home')}><Icon name="back" /></button><span>{worldOf(p.world).title}<small>Первый ход</small></span></header>
    <div className="character-intro-identity"><Avatar seed={p.handle} size={40} /><span>{p.name}</span></div>
    <h1>Твой первый ход</h1><p>{guidance?.firstStep ?? goal?.firstStep ?? contentOf(p.world).firstContext}</p>
    <label className="sr-only" htmlFor="story-writing-text">Твоя запись</label><textarea id="story-writing-text" maxLength={500} placeholder="Напиши своими словами…" value={text} onChange={event => edit(event.target.value)} disabled={!!draft.publicationId} />
    {draft.publicationId && <div role="status"><p>У записи ещё нет подтверждённого результата.</p><button className="text-button" onClick={() => nav.replace('02-2b_publish_error', { text: draft.submittedText, publicationId: draft.publicationId, ownWords: draft.submittedOwnWords, wantId: draft.submittedWantId, uncertain: true })}>Проверить статус →</button></div>}
    {!durable && <p className="small muted" role="status">Черновик доступен в этой сессии. Хранилище устройства недоступно.</p>}
    <span className="small muted">{text.length} / 500 · ⚡ −1</span>{p.postsLeft < 1 && <p role="status">Энергия закончилась. Черновик сохранён.</p>}<button className="btn" disabled={!text.trim() || !!draft.publicationId || p.postsLeft < 1} onClick={publish}>Опубликовать</button>
  </div>
  return (
    <div className={`screen no-tab compose-screen results-compose${firstStory ? ' story-compose' : ''}`}>
      <header className="results-compose-heading"><button className="icon-button" aria-label="Закрыть редактор" onClick={nav.canBack ? nav.back : () => nav.replace('03-3_feed')}><Icon name="close" /></button><h1>Новая запись</h1></header>
      <section className="results-compose-resources" aria-label="Ресурсы мира">
        <div className="results-resource-row"><button className="results-energy" onClick={() => setEnergyHelp(true)} aria-label="Как работает энергия"><span>⚡ <b>{p.postsLeft}</b><small> / {p.postsCap}</small></span><small>Энергия</small></button><div className="results-audience"><span>👥 <b>{fmt(p.subs)}</b></span><small>{worldOf(p.world).title}</small></div></div>
        <button className="results-compose-skills" onClick={() => nav.push('05-13_progress')} aria-label="История и навыки">{SKILLS.map(({ id, icon, name }) => <span key={id}><span>{icon} {name} <b>ур. {p.skills[id].level}</b></span><i><em style={{ width: `${Math.max(0, Math.min(100, p.skills[id].progress * 100))}%` }} /></i></span>)}<Icon name="arrow" size={18} /></button>
      </section>
      <div className="results-compose-body">
        <div className="compose-world small muted">Публикация в мире · <b>{worldOf(p.world).title}</b></div>
        <div className="row compose-author"><Avatar seed={p.handle} size={36} /><div><b>{p.name}</b><div className="small muted">{worldOf(p.world).title} · {p.role}</div></div></div>
        {firstStory && <section className="compose-story-context"><h2>Твой первый ход</h2><p className="muted">{guidance?.firstStep ?? goal?.firstStep ?? contentOf(p.world).firstContext}</p></section>}
        <label className="sr-only" htmlFor="compose-post-text">Твой пост</label>
        <textarea id="compose-post-text" className="compose-input" value={text} onChange={event => edit(event.target.value)} maxLength={500} placeholder="Что происходит?" disabled={!!draft.publicationId} />
        {!durable && <p className="small muted" role="status">Черновик доступен в этой сессии. Хранилище устройства недоступно.</p>}
        {draft.publicationId && <div className="results-pending" role="status"><b>Проверим, что с записью.</b><p>Запись могла сохраниться — сначала проверим её статус. Черновик сохранён.</p><button className="text-button" onClick={() => nav.replace('02-2b_publish_error', { text: draft.submittedText, publicationId: draft.publicationId, ownWords: draft.submittedOwnWords, wantId: draft.submittedWantId, uncertain: true })}>Проверить статус →</button></div>}
        <div className="results-compose-bottom"><div className="results-compose-actions"><span className="small muted">Один пост · 1 энергия</span><button className="btn" disabled={!text.trim() || !!draft.publicationId || p.postsLeft < 1} onClick={publish}>Опубликовать</button></div><div className="results-compose-count small muted">{text.length} / 500 · ⚡ −1</div>{p.postsLeft < 1 && <p className="small muted" role="status">Энергия закончилась. Черновик сохранён.</p>}</div>
      </div>
      {energyHelp && <EnergyHelp onClose={() => setEnergyHelp(false)} />}
    </div>
  )
}

// 02-2: живая реакция 3–4 с, потом итог одной карточкой
function Live() {
  const nav = useNav(); const { patch, refresh } = useStore(); const p = usePlayer()
  const text = typeof nav.params.text === 'string' ? nav.params.text : readPostDraft(draftKey(p)).submittedText ?? ''
  const [publicationId] = useState(() => (nav.params.publicationId as string) ?? readPostDraft(draftKey(p)).publicationId ?? '')
  const [floats, setFloats] = useState<{ id: number; emoji: string; x: number }[]>([])
  const [comments, setComments] = useState<CommentT[]>([])
  const [typing, setTyping] = useState<string | null>(null)
  const [phase, setPhase] = useState<'generating' | 'moderating' | 'reacting'>('generating')
  const commentsRef = useRef<CommentT[]>([])
  const reactionsRef = useRef<string[]>([])
  useEffect(() => {
    if (!text.trim() || !publicationId) { nav.replace('02-1_compose'); return }
    let active = true
    const cancel = api.publish(text, publicationId, e => {
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
        rememberResult(p, post, confirmedResult)
        if (active) nav.replace(e.result.viral ? '02-5_viral' : e.result.flop ? '02-4_flop' : '02-3_result', { result: confirmedResult, post })
      })()
    }
  }, (state, reason) => { if (state === 'failed') { const saved = readPostDraft(draftKey(p)); if (saved.publicationId === publicationId) savePostDraft(draftKey(p), { text: saved.text, ownWords: saved.ownWords, wantId: saved.wantId }) }; if (reason?.code === 'crisis') { nav.replace(p.ageBand === '14-17' || !p.ageBand ? '06-2_crisis' : '06-3_crisis_adult'); return }; nav.replace('02-2b_publish_error', { text, publicationId, ownWords: nav.params.ownWords, wantId: nav.params.wantId, reason, uncertain: state === 'unknown' }) }, status => {
    if (status === 'ready_for_moderation') setPhase('moderating')
  }, nav.params.ownWords !== false, typeof nav.params.wantId === 'string' ? nav.params.wantId : undefined, nav.params.recoveryOnly === true || typeof nav.params.publicationId !== 'string')
    return () => { active = false; cancel() }
  }, []) // eslint-disable-line react-hooks/exhaustive-deps
  return (
    <div className="screen">
      <ChannelHead />

      <div className="pad" style={{ position: 'relative' }}>
        <div className="card" style={{ border: '1px solid var(--line)', marginBottom: 12 }}>
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
  const p = usePlayer()
  const text = typeof nav.params.text === 'string' ? nav.params.text : readPostDraft(draftKey(p)).text
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
      <div className="pad" style={{ opacity: .45 }}><div className="card"><div className="small muted">{uncertain ? 'Результат уточняется' : 'Не опубликовано'}</div>{text}</div></div>
      <Sheet title={uncertain ? 'Проверим публикацию' : 'Пост не опубликован'} className="publication-error-sheet" onClose={() => nav.replace('03-5_stage')}>
          <div className="muted" style={{ marginBottom: 20 }}>{explanation}</div>
          {uncertain && publicationId && <Button onClick={() => nav.replace('02-2_live', { text, publicationId, ownWords: nav.params.ownWords, wantId: nav.params.wantId, recoveryOnly: true })}>Проверить статус</Button>}
          <Button onClick={() => nav.replace('02-1_compose', { text })}>Изменить текст</Button>
          <Button secondary onClick={() => nav.replace('03-5_stage')}>В канал</Button>

      </Sheet>
    </div>
  )
}

/** Итог поста одной карточкой поверх канала: обычный, провал, залёт (праздничная тема). */
const haptic = (kind: 'success' | 'warning' | 'light') => { if (!inTelegram) return; try { if (kind === 'light') hapticFeedback.impactOccurred.ifAvailable('light'); else hapticFeedback.notificationOccurred.ifAvailable(kind) } catch { /* вне Telegram */ } }

function ResultCard({ r, post, theme }: { r: PostResult; post: Post; theme: 'normal' | 'flop' | 'viral' }) {
  const nav = useNav(); const p = usePlayer()
  const closeButton = useRef<HTMLButtonElement>(null)
  const saved = resultHistory(p).find(item => item.post.id === r.postId)
  const close = () => nav.replace('03-5_stage')
  useEffect(() => {
    haptic(r.delta >= 0 ? 'success' : 'warning')
    const previous = document.activeElement as HTMLElement | null
    closeButton.current?.focus()
    const keydown = (event: KeyboardEvent) => { if (event.key === 'Escape') close() }
    document.addEventListener('keydown', keydown)
    return () => { document.removeEventListener('keydown', keydown); previous?.focus() }
  }, [])
  return <div className="screen result-screen results-parity-screen"><ChannelHead /><div className="pad"><article className="timeline-post"><div className="row"><Avatar seed={p.handle} /><b>{p.name}</b><span className="small muted">только что</span></div><p>{post.text}</p></article></div>
    <div className="publication-overlay results-peek-layer"><div className={`publication-dialog results-peek${theme === 'flop' ? ' negative' : ''}`} role="dialog" aria-labelledby="publication-title" onClick={event => event.stopPropagation()}>
      <div className="publication-dialog-heading"><p className="muted" id="publication-title">Запись опубликована</p><button ref={closeButton} className="icon-button" aria-label="Закрыть результат" onClick={close}><Icon name="close" /></button></div>
      <PublicationResult result={r} skills={saved?.skills} onDiscussion={() => nav.replace('02-6_thread', { post })} />
    </div></div></div>
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
    try { localStorage.setItem(storageKey, JSON.stringify({ ...draft, at: Date.now() })) } catch { /* optional storage */ }
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
  const postAuthor = stars.find(star => `Star_${star.id}` === post?.authorId)
  const isStarComment = (comment?: CommentT) => !!comment?.star && !comment.hidden && stars.some(star => `Star_${star.id}` === comment.authorId)
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
  const fromFeed = nav.params.returnTo === 'feed' || (!nav.params.returnTo && !!postAuthor)
  const goBack = () => { if (nav.params.returnTo && nav.canBack) nav.back(); else nav.replace(fromFeed ? '03-3_feed' : '03-5_stage') }
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
    <div className="screen thread-screen">
      <TopBar onBack={goBack} title="Обсуждение" rightPlain right={<div className="thread-header-actions"><EnergyBadge value={player.postsLeft} /><button className="thread-exit" onClick={goBack}>{fromFeed ? 'В ленту →' : 'В канал →'}</button></div>} />
      <div className="pad">
        <div className="timeline-post" style={{ marginBottom: 12 }}><div className="row"><Avatar seed={postAuthor?.avatarSeed ?? player.handle} size={40} /><b>{postAuthor?.name ?? player.name}</b></div><p>{post.text}</p><div style={{ marginTop: 8 }}>{post.reactions.map(reaction => <span key={reaction.emoji} className="react">{reaction.emoji} {reaction.count}</span>)}</div></div>
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
      <div className="bottom-cta thread-composer" style={{ flexDirection: 'row' }}>
        <div className="card" style={{ flex: 1, padding: '12px 14px' }}><textarea rows={2} className="thread-reply-input" value={draft.text} onChange={event => edit({ text: event.target.value })} placeholder={targetAvailable ? (draft.commentId === null ? 'Комментарий · 1 энергия' : 'Ответ · 1 энергия') : 'Выбери комментарий звезды'} maxLength={1000} disabled={!targetAvailable || sending} /></div>
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
}, { '02-2_live': 0, '02-6_thread': 1 })
