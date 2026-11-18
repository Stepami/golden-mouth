import { useGuestSession } from '../app/landing/guestSession'
import { tap } from '../app/analytics'
import { gameOnboardingState, onboardingPostSaved, onboardingReplyStarted, onboardingReplySaved, gameOnboardingActive, updateGameOnboarding, useGameOnboarding } from '../app/GameOnboardingState'
import { recordEntryStep } from '../app/landing/openobserve'
import { RepostAction } from '../ui/RepostAction'
import { PostLikeAction } from '../ui/PostLikeAction'
import { QuotedPost } from '../ui/QuotedPost'
import { StoryGoal, StoryGoalContent } from '../ui/StoryGoal'
import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import { hapticFeedback, shareStory, shareURL } from '@tma.js/sdk'
import { api, ApiError } from '../api/client'
import { firstPost, results } from '../api/mock'
import type { CommentT, Post, PostResult } from '../api/types'
import { useNav } from '../app/nav'
import { usePlayer, useStore } from '../app/store'
import { inTelegram } from '../telegram'
import { Avatar, BottomCTA, Button, Comment, Gift, Ribbon, Sheet, TopBar, EnergyBadge, Whisper, fmt, plural } from '../ui'
import { ChannelHead } from './onboarding'
import { track } from '../app/log'
import { contentOf, worldOf } from '../api/worldContent'
import { apiBaseUrl, runtimeConfig } from '../app/runtimeConfig'
import { useReferralLink } from '../app/useReferralLink'
import { register } from '.'
import { TextField } from '../ui/TextField'
import { completePostDraft, draftKey, hasPostDraft, readPostDraft, savePostDraft, type PostDraft } from '../app/postDraft'
import { blankWorld, selectDraftOwner, setWorldDraft } from '../app/worldDraft'


// 02-1: пост, «Подскажи» дописывает черновик
export function Compose() {
  const guest = useGuestSession(); const profile = useStore()?.player
  const nav = useNav(); const p = guest?.player ?? profile!
  const ideas = guest ? [guest.postIdea] : contentOf(p.world).ideas
  const repostId = typeof nav.params.repostId === 'string' ? nav.params.repostId : undefined
  const key = draftKey(p) + (repostId ? `:quote:${repostId}` : '')
  const mayPrefill = !repostId && typeof nav.params.text !== 'string' && (guest ? guest.text === undefined : !hasPostDraft(key))
  const firstPostConfirmed = nav.params.firstPostConfirmed === true
  const [visibleLength, setVisibleLength] = useState<number | undefined>(() => mayPrefill && firstPostConfirmed ? 0 : undefined)
  const [draft, setDraft] = useState<PostDraft>(() => {
    const saved = guest ? { text: guest.text ?? '', ownWords: guest.text !== undefined } : readPostDraft(key)
    const idea = ideas.find(value => value.trim())
    if (mayPrefill && firstPostConfirmed && idea) return { text: idea, ownWords: false }
    return saved.text.length || guest?.text !== undefined ? saved : { text: typeof nav.params.text === 'string' ? nav.params.text : '', ownWords: true }
  })
  const canPrefill = useRef(!guest && mayPrefill && !firstPostConfirmed)
  const [checkingFirstPost, setCheckingFirstPost] = useState(canPrefill.current)
  const text = draft.text
  const [durable, setDurable] = useState(true)
  const edit = (value: string, fromIdea = false) => {
    canPrefill.current = false
    setVisibleLength(undefined)
    const next = { ...draft, text: value, ownWords: fromIdea ? false : value.length === 0 || draft.ownWords }
    setDraft(next)
    if (guest) guest.editPost(value)
    else setDurable(savePostDraft(key, next))
  }
  useEffect(() => {
    if (!guest && (draft.text.length || !canPrefill.current)) setDurable(savePostDraft(key, draft))
  }, [key, draft])
  useEffect(() => {
    if (!canPrefill.current) return
    let active = true
    void api.channelPosts(p.worldInstanceId).then(page => {
      const idea = ideas.find(value => value.trim())
      if (active && canPrefill.current && !page.items.length && !page.nextCursor && idea) {
        canPrefill.current = false
        setDurable(savePostDraft(key, { text: idea, ownWords: false }))
        setVisibleLength(0)
        setDraft({ text: idea, ownWords: false })
      }
    }).catch(() => { /* Keep the editor available; an untouched empty draft can retry on reopening. */ })
      .finally(() => { if (active) setCheckingFirstPost(false) })
    return () => { active = false }
  }, [key])
  const revealing = visibleLength !== undefined
  useEffect(() => {
    if (!revealing) return
    const motion = window.matchMedia('(prefers-reduced-motion: reduce)')
    const finish = () => setVisibleLength(undefined)
    if (motion.matches) { finish(); return }
    motion.addEventListener('change', finish)
    const started = performance.now()
    const timer = window.setInterval(() => {
      const count = Math.floor((performance.now() - started) / 18)
      setVisibleLength(count >= text.length ? undefined : count)
    }, 30)
    return () => { window.clearInterval(timer); motion.removeEventListener('change', finish) }
  }, [text, revealing])
  return (
    <div className="screen no-tab">
      <TopBar onBack={nav.back} title={repostId ? "Репост со своим текстом" : "Новый пост"} rightPlain right={<EnergyBadge value={p.postsLeft} />} />
      <div className="pad">
        {!repostId && (guest ? <StoryGoalContent goal={guest.goal} firstStep /> : <StoryGoal world={p.world} firstStep />)}
        {repostId && <QuotedPost repostId={repostId} />}
        <TextField label="Твой пост" value={text} visibleLength={visibleLength} onInteract={() => { canPrefill.current = false; setVisibleLength(undefined) }} fromIdea={!draft.ownWords} onChange={edit} maxLength={500} rows={6} placeholder="Расскажи, что произошло…" />{!repostId && <div className="compose-ideas"><div className="small muted">О чём написать</div>{ideas.slice(0, 3).map(idea => <button className="opt" key={idea} onClick={() => { const next = idea.replace(/[«»]/g, ''); const isIdea = ideas.some(value => value.replace(/[«»]/g, '') === text); if (!text.trim() || isIdea || window.confirm('Заменить написанный текст выбранной идеей?')) edit(next, true) }}>{idea}</button>)}{text.length > 0 && <p className="small muted">Идею можно дописать своими словами.</p>}</div>}
        {checkingFirstPost && <p className="small muted" role="status">Проверяем историю публикаций…</p>}
        {!durable && <p className="small muted" role="status">Черновик доступен в этой сессии. Хранилище устройства недоступно.</p>}
      </div>
      <BottomCTA main="Опубликовать" disabled={!text.trim()} onMain={() => { if (guest) { nav.replace('02-2_live', { text }); return } if (p.postsLeft > 0) { track('post_publish', { len: text.length, own_words: draft.ownWords, publication_kind: repostId ? 'quote' : 'post' }); nav.replace('02-2_live', { text, ownWords: draft.ownWords, repostId }) } else { track('limit_reached'); nav.replace('04-1_limit') } }} />
    </div>
  )
}

// 02-2: живая реакция 3–4 с, потом итог одной карточкой
export function Live() {
  const guest = useGuestSession(); const store = useStore()
  const nav = useNav(); const p = guest?.player ?? store.player!
  const text = guest?.text ?? (nav.params.text as string) ?? firstPost.text
  const [floats, setFloats] = useState<{ id: number; emoji: string; x: number }[]>([])
  const [views, setViews] = useState(0)
  const [typing, setTyping] = useState<string | null>(null)
  const [phase, setPhase] = useState<'generating' | 'reacting'>('generating')
  useEffect(() => {
    if (guest) {
      const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches
      const reaction = setTimeout(() => {
        setViews(guest.post.views); setPhase('reacting'); setTyping(guest.speaker)
        if (!reduced && guest.post.likes > 0) setFloats([{ id: 0, emoji: '♡', x: 45 }])
      }, reduced ? 0 : 230)
      const finish = setTimeout(() => nav.replace('02-3_result'), reduced ? 0 : 1530)
      return () => { clearTimeout(reaction); clearTimeout(finish) }
    }
    const { patch, refresh } = store
    let active = true
    const start = setTimeout(() => { void (async () => {
      const firstPost = gameOnboardingState()?.phase === 'post' && !nav.params.repostId
      let created: Awaited<ReturnType<typeof api.createPost>>
      try {
        created = await api.createPost(text, nav.params.ownWords !== false, typeof nav.params.repostId === 'string' ? nav.params.repostId : undefined)
      } catch (cause) {
        if (!active) return
        if (firstPost) track('onboarding_step', { step: 'post_save', ok: false, kind: cause instanceof ApiError ? String(cause.status) : 'unconfirmed' })
        if (cause instanceof ApiError && cause.problemType === 'urn:reki:problem:crisis') { nav.replace(p.ageBand === '14-17' || !p.ageBand ? '06-2_crisis' : '06-3_crisis_adult'); return }
        nav.replace('02-2b_publish_error', { text, repostId: nav.params.repostId, status: cause instanceof ApiError ? cause.status : undefined, uncertain: !(cause instanceof ApiError) || (cause.status >= 500 && cause.status !== 503) })
        return
      }
      if (!nav.params.repostId) onboardingPostSaved(p.worldInstanceId ?? p.world, created.post.id)
      if (!active) return
      if (!nav.params.repostId || text.length > 0) completePostDraft(draftKey(p) + (typeof nav.params.repostId === 'string' ? `:quote:${nav.params.repostId}` : ''), text)
      const latest = await refresh()
      if (!active) return
      if (!latest) patch({ postsLeft: Math.max(0, p.postsLeft - 1), subs: Math.max(0, p.subs + created.result.delta) })
      setViews(created.post.views)
      setPhase('reacting')
      setFloats(created.post.likes > 0 ? [{ id: 0, emoji: '♡', x: 45 }] : [])
      const star = contentOf(p.world).stars.find(item => `Star_${item.id}` === created.predictedAuthorId)
      setTyping(star?.name ?? 'Звезда')
      const commentPromise = api.initialComment(created.post.id, created.predictedAuthorId).catch(() => null).then(saved => {
        if (firstPost) track('onboarding_step', { step: 'initial_reaction_result', ok: !!saved?.comments.length })
        return saved
      })
      await new Promise(resolve => setTimeout(resolve, 1300))
      if (!active) return
      setTyping(null)
      track('post_result', { delta: created.result.delta, viral: created.result.viral, flop: created.result.flop, publication_kind: nav.params.repostId ? text.length ? 'quote' : 'repost' : 'post' })
      const result = created.result
      nav.replace(result.viral ? '02-5_viral' : result.flop ? '02-4_flop' : '02-3_result', { result, post: created.post, commentPromise, predictedAuthorId: created.predictedAuthorId })
    })() }, 0)
    return () => { active = false; clearTimeout(start) }
  }, []) // eslint-disable-line react-hooks/exhaustive-deps
  return (
    <div className="screen">
      <ChannelHead />

      <div className="pad" style={{ position: 'relative' }}>
        <div className="card" style={{ boxShadow: '0 0 0 2px rgba(255,107,139,.6),0 0 44px rgba(255,107,139,.35)', marginBottom: 12 }}>
          <div className="small" style={{ color: 'var(--accent)', marginBottom: 6 }} aria-live="polite">{phase === 'generating' ? 'готовим публикацию' : 'только что опубликовано'}</div>{text}{!guest && <QuotedPost repostId={typeof nav.params.repostId === 'string' ? nav.params.repostId : undefined} />}
          {phase === 'reacting'
            ? <div className="small muted" role="status" style={{ marginTop: 16 }}>👁 {fmt(views)} · Появляются реакции…</div>
            : <div className="live-current" aria-hidden="true"><span /></div>}
        </div>
        <div style={{ position: 'absolute', right: 14, top: 120, width: 120, height: 200, pointerEvents: 'none' }}>
          {floats.map(f => <span key={f.id} className="float-react" style={{ left: `${f.x}%`, bottom: 0, fontSize: 24 }}>{f.emoji}</span>)}
        </div>
        {typing && <div className="muted small row" style={{ gap: 8 }}>{typing} печатает...</div>}
      </div>
      <div className="small muted" style={{ position: 'fixed', bottom: 96, left: 0, right: 0, textAlign: 'center' }} aria-live="polite">{phase === 'generating' ? 'готовим пост' : 'Собираем итог…'}</div>
    </div>
  )
}

function PublishError() {
  const nav = useNav()
  const text = typeof nav.params.text === 'string' ? nav.params.text : firstPost.text.replace(' 🫠', '')
  const uncertain = nav.params.uncertain === true
  const status = nav.params.status as number | undefined
  const explanation = uncertain ? 'Не удалось подтвердить публикацию. Проверь канал, прежде чем отправлять пост снова.' : status === 409 ? 'Энергия закончилась. Новая появится в 9:00. Черновик сохранён.' : status === 422 ? 'Так не получится, попробуй по-другому. Черновик сохранён.' : status === 503 ? 'Не получилось подготовить публикацию. Твой текст сохранён, попробуй позже.' : 'Не получилось опубликовать пост. Текст сохранён.'

  return (
    <div className="screen no-tab">
      <ChannelHead />
      <div className="pad" style={{ opacity: .45 }}><div className="card"><div className="small muted">{uncertain ? 'статус неизвестен' : 'не опубликовано'}</div>{text}</div></div>
      <div className="sheet-bg" style={{ background: 'rgba(8,8,12,.65)' }}>
        <div className="sheet" role="alert" aria-live="assertive">
          <div style={{ fontSize: 24, fontWeight: 800, marginBottom: 8 }}>{uncertain ? 'Не удалось подтвердить публикацию' : 'Пост не опубликован'}</div>
          <div className="muted" style={{ marginBottom: 20 }}>{explanation}</div>
          <Button onClick={() => nav.replace('02-1_compose', { text, repostId: nav.params.repostId })}>Изменить текст</Button>
          <Button secondary onClick={() => nav.replace('03-5_stage')}>В канал</Button>

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

function ResultCard({ r, post: initialPost, theme }: { r: PostResult; post: Post; theme: 'normal' | 'flop' | 'viral' }) {
  const guest = useGuestSession(); const profile = useStore()?.player
  const nav = useNav(); const player = guest?.player ?? profile!
  const onboarding = useGameOnboarding()
  const [post, setPost] = useState(initialPost)
  const [commentFailed, setCommentFailed] = useState(false)
  const commentPromise = nav.params.commentPromise as Promise<Post | null> | undefined
  useEffect(() => {
    let active = true
    void commentPromise?.then(saved => { if (active) { if (saved) setPost(current => ({ ...current, comments: saved.comments })); else setCommentFailed(true) } }, () => { if (active) setCommentFailed(true) })
    return () => { active = false }
  }, [commentPromise])
  const closeButton = useRef<HTMLButtonElement>(null), dialog = useRef<HTMLDivElement>(null)
  useEffect(() => {
    closeButton.current?.focus()
    const keyboard = (event: KeyboardEvent) => {
      if (event.key === 'Escape') nav.replace('03-5_stage')
      if (event.key !== 'Tab') return
      const items = dialog.current?.querySelectorAll<HTMLElement>('button:not(:disabled),summary,a[href]')
      if (!items?.length) return
      const first = items[0], last = items[items.length - 1]
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus() }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus() }
    }
    document.addEventListener('keydown', keyboard)
    return () => document.removeEventListener('keydown', keyboard)
  }, [nav])
  const viral = theme === 'viral'
  const stars = guest?.world.stars ?? contentOf(player.world).stars
  const comment = post.comments.find(item => item.star && !item.hidden)
  const fallback = r.bestComment
  const actor = stars.find(item => `Star_${item.id}` === comment?.authorId || item.avatarSeed === (comment?.avatar ?? fallback?.avatar) || `Star_${item.id}` === nav.params.predictedAuthorId)
  const commentText = comment?.text ?? fallback?.text
  const entryReaction = useRef('')
  useEffect(() => {
    if (guest) return
    const seen = () => {
      if (document.visibilityState !== 'visible' || !commentText?.trim() || entryReaction.current === post.id) return
      entryReaction.current = post.id
      recordEntryStep('landing_reaction_viewed', post.id)
    }
    seen()
    document.addEventListener('visibilitychange', seen)
    return () => document.removeEventListener('visibilitychange', seen)
  }, [commentText, post.id])
  const shown = useCount(Math.abs(r.delta))
  const additive = r.whys.filter(why => /^[+−-]\d+(?:[.,]\d+)?$/.test(why.value))
  const explainsDelta = additive.length > 0 && additive.reduce((sum, why) => sum + Number(why.value.replace('−', '-').replace(',', '.')), 0) === r.delta
  const explanation = r.delta === 0 ? 'Число подписчиков не изменилось.' : r.delta < 0 ? r.whys.find(why => !why.value.startsWith('×') && why.label !== 'В тему')?.label ?? r.headline : r.headline
  useEffect(() => { if (!guest) haptic(r.delta >= 0 ? 'success' : 'warning') }, [r])
  return (
    <div className="screen no-tab">
      <ChannelHead />
      <div className="pad" style={{ opacity: .45 }}><div className="card"><div className="small muted">только что</div>{post.text}{!guest && <QuotedPost repostId={post.repostId} />}<div style={{ marginTop: 8 }}><span className="small muted">♡ {fmt(post.likes)}</span><span className="small muted" style={{ float: 'right' }}>👁 {fmt(r.views)}</span></div></div></div>
      <div className="sheet-bg" style={{ background: 'rgba(8,8,12,.55)' }}>
        <div ref={dialog} role="dialog" aria-modal="true" aria-label="Пост опубликован" className={`sheet post-result${viral ? ' post-result--viral' : ''}`}>
          <button ref={closeButton} className="result-close" aria-label="Закрыть итог и вернуться в канал" onClick={() => nav.replace('03-5_stage')}>×</button>
          <div className="post-result-title">Пост опубликован</div>
          <section className="post-result-reaction" aria-label="Ответ персонажа">
            <div className="row"><Avatar seed={comment?.avatar ?? actor?.avatarSeed ?? fallback?.avatar} label={comment?.author ?? actor?.name ?? 'Персонаж'} size={40} avatarStyle={guest?.world.avatarStyle} mode="default" /><div><strong>{comment?.author ?? actor?.name ?? 'Персонаж'}</strong><div className="small muted">{guest?.world.title ?? worldOf(player.world).title} · AI-персонаж</div></div></div>
            {commentText ? <p>{commentText}</p> : <p className="muted" role="status">{commentFailed || !commentPromise ? 'Ответ персонажа пока не загрузился.' : 'Персонаж готовит ответ…'}</p>}
          </section>
          <div className={`post-result-delta ${r.delta > 0 ? 'up' : r.delta < 0 ? 'down' : ''}`}>{r.delta > 0 ? '+' : r.delta < 0 ? '−' : ''}{fmt(shown)} <span>{plural(Math.abs(r.delta), 'подписчик', 'подписчика', 'подписчиков')}</span></div>
          <p className="small muted">{explanation}</p>
          {!guest && <div className="post-action-group"><PostLikeAction key={`${player.id}:${player.worldInstanceId}:${post.id}`} postId={post.id} likes={post.likes} likedBy={post.likedBy} onSaved={saved => setPost(current => ({ ...current, likes: saved.likes, likedBy: saved.likedBy }))} />{!post.repostId && <RepostAction postId={post.id} />}</div>}
          <details className="result-details"><summary>Разбор поста</summary><div>{r.whys.map((why, i) => <span key={`${why.label}:${i}`} className="chip">{({ star: '⭐', brave: '🔥', humor: '😂', topic: '🎯', reach: '👁' } as Record<string, string>)[why.icon] ?? why.icon} {why.label} {(!/^[+−-]/.test(why.value) || explainsDelta) && <b className={/^[+×]/.test(why.value) ? 'up' : 'down'}>{why.value}</b>}</span>)}</div>
            <div className="result-awards">{r.ribbons.map(award => <Ribbon key={award.title} icon={award.icon} title={award.title} sub={award.sub} />)}</div>
          </details>
          <div className="post-result-actions">{guest?.hint}{!guest && !gameOnboardingActive(onboarding) && <Button onClick={() => { tap('result_open_next_post'); nav.replace('02-1_compose') }}>Новый пост</Button>}<span data-game-onboarding-post={comment ? post.id : undefined}><Button secondary onClick={() => { if (!guest) tap('post_open_conversation', { ok: !!comment }); nav.replace('02-6_thread', { post, commentPromise, focusReply: true }) }}>Комментировать</Button></span></div>
          {!guest && <button onClick={() => nav.push('02-7_share', { post })} className="result-share">Поделиться</button>}
        </div>
      </div>
    </div>
  )
}

export const Result = () => { const n = useNav(); const guest = useGuestSession(); if (guest) return <ResultCard r={guest.result} post={guest.post} theme="normal" />; const r = (n.params.result as PostResult) ?? results.normal; return <ResultCard r={r} post={(n.params.post as Post) ?? { ...firstPost, id: r.postId, text: (n.params.text as string) ?? firstPost.text }} theme="normal" /> }
const Flop = () => { const n = useNav(); const r = (n.params.result as PostResult) ?? results.flop; return <ResultCard r={r} post={(n.params.post as Post) ?? { ...firstPost, id: r.postId, text: (n.params.text as string) ?? 'сегодня сдавала пробник по ЕГЭ, всё сложно' }} theme="flop" /> }
const Viral = () => { const n = useNav(); const r = (n.params.result as PostResult) ?? results.viral; return <ResultCard r={r} post={(n.params.post as Post) ?? { ...firstPost, id: r.postId, text: (n.params.text as string) ?? firstPost.text }} theme="viral" /> }

// 02-6: комментарий игрока сохраняется сразу, затем каждая звезда генерируется и сохраняется отдельно.
export function Thread() {
  const guest = useGuestSession()
  const nav = useNav()
  const onboarding = useGameOnboarding()
  const store = useStore()
  const player = guest?.player ?? store.player!
  const publishedPost = guest?.post ?? (nav.params.post as Post | undefined)
  const postId = typeof nav.params.postId === 'string' ? nav.params.postId : publishedPost?.id
  const stars = guest?.world.stars ?? contentOf(player.world).stars
  const starIds = stars.map(star => star.id).join('\0')
  const threadKey = `${player.id}:${player.worldInstanceId ?? player.world}:${postId ?? 'mock'}`
  const storageKey = `reki:reply-draft:${threadKey}`
  type Draft = { requestId: string; commentId: string | null; text: string; submitted: boolean; additionalStarId?: string }
  const fresh = (): Draft => ({ requestId: crypto.randomUUID(), commentId: null, text: '', submitted: false })
  const readDraft = (): Draft => {
    if (guest) return { requestId: 'guest-reply', commentId: publishedPost?.comments[0]?.id ?? null, text: guest.reply, submitted: guest.replySubmitted }
    try {
      const saved = JSON.parse(localStorage.getItem(storageKey) ?? 'null') as Draft | null
      if (saved && typeof saved.requestId === 'string' && saved.requestId.length > 0 && saved.requestId.length <= 64 && typeof saved.text === 'string' && saved.text.length <= 1000 && (saved.commentId === null || typeof saved.commentId === 'string')) return { ...saved, submitted: saved.submitted === true, additionalStarId: typeof saved.additionalStarId === 'string' ? saved.additionalStarId : undefined }
    } catch { /* Хранилище может быть недоступно. */ }
    return fresh()
  }
  const [draft, setDraft] = useState<Draft>(readDraft)
  const [post, setPost] = useState<Post | null>(() => publishedPost ?? (postId ? null : firstPost))
  const [load, setLoad] = useState<'ready' | 'loading' | 'error' | 'unavailable'>(() => post ? 'ready' : 'loading')
  const [loadedKey, setLoadedKey] = useState(threadKey)
  const [reloadToken, setReloadToken] = useState(0)
  const [sending, setSending] = useState(false)
  const [typingName, setTypingName] = useState<string | null>(null)
  const [replyError, setReplyError] = useState('')
  const [deleting, setDeleting] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [deleteError, setDeleteError] = useState('')
  const replyInput = useRef<HTMLTextAreaElement>(null)
  useLayoutEffect(() => {
    const input = replyInput.current
    if (!input) return
    const scrollTop = input.scrollTop
    input.style.height = 'auto'
    input.style.height = `${input.scrollHeight}px`
    input.scrollTop = document.activeElement === input && input.selectionStart === input.value.length
      ? input.scrollHeight : scrollTop
  }, [draft.text, load, threadKey])
  const controller = useRef<AbortController | null>(null)
  const activeKey = useRef(threadKey)
  activeKey.current = threadKey
  if (loadedKey !== threadKey) {
    setLoadedKey(threadKey); setPost(publishedPost ?? (postId ? null : firstPost))
    setLoad(postId ? 'loading' : 'ready'); setDraft(readDraft()); setSending(false); setTypingName(null); setReplyError('')
  }
  useEffect(() => {
    if (guest || loadedKey !== threadKey) return
    try { localStorage.setItem(storageKey, JSON.stringify(draft)) } catch { /* optional storage */ }
  }, [draft, loadedKey, threadKey, storageKey])
  useEffect(() => () => { controller.current?.abort() }, [threadKey])
  useEffect(() => {
    if (guest || !postId) return
    let alive = true
    setLoad('loading')
    api.post(postId).then(found => {
      if (!alive) return
      if (!found) { setPost(null); setLoad('unavailable'); return }
      setPost(found); setLoad('ready')
      const saved = found.comments.find(comment => comment.requestId === draft.requestId && (comment.parentCommentId ?? null) === draft.commentId && comment.text === draft.text)
      if (saved && onboarding?.postId && ['reaction', 'reply'].includes(onboarding.phase)) {
        const recipient = saved.parentCommentId ? found.comments.find(comment => comment.id === saved.parentCommentId)?.authorId : found.authorId
        if (stars.some(star => `Star_${star.id}` === recipient) && found.id === (onboarding.conversationPostId ?? onboarding.postId)) {
          updateGameOnboarding({ phase: 'reply', conversationPostId: found.id })
          onboardingReplyStarted(found.id, draft.requestId); onboardingReplySaved(found.id, draft.requestId)
        }
      }
      if (saved && found.comments.some(comment => comment.requestId === `${draft.requestId}-star`) &&
        (!draft.additionalStarId || found.comments.some(comment => comment.requestId === `${draft.requestId}-extra`))) setDraft(fresh())
    }).catch((cause: unknown) => {
      if (alive) setLoad((cause as { status?: number })?.status === 404 ? 'unavailable' : 'error')
    })
    return () => { alive = false }
    // The current draft is compared once when opening/reloading this post.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [postId, threadKey, reloadToken, starIds])
  const commentPromise = nav.params.commentPromise as Promise<Post | null> | undefined
  useEffect(() => {
    if (guest || load !== 'ready' || !commentPromise) return
    let active = true
    void commentPromise.then(saved => {
      if (active && saved) setPost(current => current ? { ...current, comments: [...current.comments, ...saved.comments.filter(comment => !current.comments.some(existing => existing.id === comment.id))] } : saved)
    })
    return () => { active = false }
  }, [commentPromise, load, threadKey])
  const initialReplyFocused = useRef('')
  useEffect(() => {
    if (guest || !nav.params.focusReply || load !== 'ready' || !post || initialReplyFocused.current === threadKey) return
    const target = post.comments.find(comment => comment.star && !comment.hidden && stars.some(star => `Star_${star.id}` === comment.authorId))
    if (!target) return
    initialReplyFocused.current = threadKey
    setDraft(current => current.commentId || current.submitted ? current : { ...current, commentId: target.id, requestId: crypto.randomUUID() })
    requestAnimationFrame(() => replyInput.current?.focus())
  }, [post, load, threadKey, nav.params.focusReply])
  const postAuthor = stars.find(star => `Star_${star.id}` === post?.authorId)
  const isStarComment = (comment?: CommentT) => !!comment?.star && !comment.hidden && stars.some(star => `Star_${star.id}` === comment.authorId)
  const target = post?.comments.find(comment => comment.id === draft.commentId)
  useEffect(() => {
    if (guest || load !== 'ready' || !post || !onboarding?.postId || !['reaction', 'reply'].includes(onboarding.phase)) return
    if ((post.id === (onboarding.conversationPostId ?? onboarding.postId) && (post.comments.some(isStarComment) || postAuthor)) && (onboarding.phase !== 'reply' || (onboarding.conversationPostId ?? onboarding.postId) !== post.id)) updateGameOnboarding({ phase: 'reply', conversationPostId: post.id, replyRequestId: undefined })
  }, [load, post, onboarding?.phase, onboarding?.postId, onboarding?.conversationPostId])
  const targetAvailable = draft.commentId === null ? !!postAuthor : isStarComment(target)
  const recipient = draft.commentId === null ? postAuthor?.name : target?.author
  const edit = (change: Partial<Pick<Draft, 'text' | 'commentId'>>) => {
    setDraft(current => {
      const next = { ...current, ...change }
      return next.text === current.text && next.commentId === current.commentId
        ? current : { ...next, requestId: guest ? current.requestId : crypto.randomUUID(), submitted: false, additionalStarId: undefined }
    })
    if (guest && change.text !== undefined) guest.editReply(change.text)
    setReplyError('')
  }
  const send = async () => {
    if (!post || !targetAvailable || !draft.text.trim() || sending) return
    if (guest) { guest.sendReply(); return }
    const { patch } = store
    if (player.postsLeft < 1 && !draft.submitted) { setReplyError('Энергия закончилась. Новая появится в 9:00.'); return }
    const ctrl = new AbortController()
    controller.current = ctrl
    const key = threadKey
    let playerSaved = false
    tap('reply_send')
    onboardingReplyStarted(post.id, draft.requestId)
    setSending(true); setReplyError(''); setTypingName(null); setDraft(current => ({ ...current, submitted: true }))
    try {
      const starAuthorId = draft.commentId === null ? postAuthor?.id : target?.authorId
      if (!starAuthorId) throw new Error('Star is unavailable')
      const event = await api.reply(post.id, draft.commentId, draft.requestId, draft.text, `Star_${starAuthorId.replace(/^Star_/, '')}`, { signal: ctrl.signal,
        additionalStarId: draft.additionalStarId,
        onAdditionalStar: starId => {
          if (!ctrl.signal.aborted && activeKey.current === key)
            setDraft(current => current.requestId === draft.requestId ? { ...current, additionalStarId: starId ?? undefined } : current)
        },
        onSaved: saved => {
          if (ctrl.signal.aborted || activeKey.current !== key) return
          if (saved.post.comments.some(comment => comment.requestId === draft.requestId)) { playerSaved = true; onboardingReplySaved(post.id, draft.requestId) }
          const { postId: id, ...detail } = saved.post
          setPost(current => ({ ...detail, id, likes: current?.likes ?? detail.likes, likedBy: current?.likedBy ?? detail.likedBy })); patch({ subs: saved.subscribers, postsLeft: saved.energyLeft })
        },
        onTyping: authorId => {
          if (ctrl.signal.aborted || activeKey.current !== key) return
          setTypingName(authorId ? stars.find(star => `Star_${star.id}` === authorId)?.name ?? recipient ?? 'Звезда' : null)
        } })
      if (ctrl.signal.aborted || activeKey.current !== key) return
      if (event.post.comments.some(comment => comment.requestId === draft.requestId)) onboardingReplySaved(post.id, draft.requestId)
      const { postId: id, ...detail } = event.post
      setPost(current => ({ ...detail, id, likes: current?.likes ?? detail.likes, likedBy: current?.likedBy ?? detail.likedBy })); patch({ subs: event.subscribers, postsLeft: event.energyLeft })
      setDraft(fresh()); setTypingName(null)
      try { localStorage.removeItem(storageKey) } catch { /* optional storage */ }
    } catch (cause) {
      if (ctrl.signal.aborted || activeKey.current !== key) return
      const status = (cause as { status?: number })?.status
      track('onboarding_step', { step: playerSaved ? 'reply_reaction_result' : 'reply_save', ok: false, kind: status ? String(status) : 'unconfirmed' })
      setReplyError(playerSaved && cause instanceof Error && cause.name === 'unsafe_generated_content'
        ? 'Комментарий сохранён. Ответ звезды не прошёл проверку 14+.'
        : playerSaved ? 'Комментарий сохранён. Ответ звезды пока не появился. Попробуй снова.'
          : status === 409 ? 'Комментарий пока не сохранён. Проверь энергию и попробуй снова.'
            : status === 422 ? 'Так не получится, попробуй по-другому.'
              : 'Не удалось подтвердить комментарий. Попробуй снова: сохранённый комментарий не расходует энергию повторно.')
      setTypingName(null)
    } finally {
      if (!ctrl.signal.aborted && activeKey.current === key) setSending(false)
      if (controller.current === ctrl) controller.current = null
    }
  }
  const tutorialKey = `reki:thread-help:${player.id}`
  const [showHelp, setShowHelp] = useState(() => { if (guest) return false; try { return localStorage.getItem(tutorialKey) !== 'seen' } catch { return true } })
  const dismissHelp = () => { setShowHelp(false); try { localStorage.setItem(tutorialKey, 'seen') } catch { /* memory fallback */ } }
  const focusReply = (id: string) => { tap('reply_select_hero'); edit({ commentId: id }); requestAnimationFrame(() => replyInput.current?.focus()) }
  const fromFeed = nav.params.returnTo === 'feed' || (!nav.params.returnTo && !!postAuthor)
  const fromMarked = nav.params.returnTo === 'marked'
  const goBack = () => { if (guest) nav.replace('03-5_stage'); else if (nav.params.returnTo && nav.canBack) nav.back(); else if (fromMarked) nav.replace('05-5_collection', { profileTab: 'marked' }); else nav.replace(fromFeed ? '03-3_feed' : '03-5_stage') }
  const removePost = async () => {
    if (!post || deleting) return
    setDeleting(true); setDeleteError('')
    try { await api.deletePost(post.id); nav.reset(fromMarked ? '05-5_collection' : fromFeed ? '03-3_feed' : '03-5_stage', fromMarked ? { profileTab: 'marked' } : {}) }
    catch { setDeleteError('Не получилось удалить пост. Попробуй ещё раз.'); setDeleting(false) }
  }
  if (!guest && (load !== 'ready' || loadedKey !== threadKey)) return (
    <div className="screen no-tab"><TopBar onBack={goBack} title="Реакции" /><div className="pad"><div className="card" role="status">
      {load === 'loading' && <span className="small muted">Загружаем пост…</span>}
      {load === 'error' && <><div>Не получилось открыть пост.</div><Button onClick={() => setReloadToken(value => value + 1)}>Попробовать снова</Button></>}
      {load === 'unavailable' && <div>Этот пост больше не доступен.</div>}
    </div></div></div>
  )
  const shownPost = guest?.post ?? post
  if (!shownPost) return null
  const replySending = guest ? guest.replySubmitted && !guest.replyDone : sending
  const activeTypingName = guest ? replySending ? guest.speaker : null : typingName
  const ordered: CommentT[] = []
  const visited = new Set<string>()
  const append = (comment: CommentT) => {
    if (visited.has(comment.id)) return
    visited.add(comment.id); ordered.push(comment)
    shownPost.comments.filter(child => child.parentCommentId === comment.id).forEach(append)
  }
  shownPost.comments.filter(comment => !comment.parentCommentId).forEach(append)
  shownPost.comments.forEach(append)
  return (
    <div className="screen no-tab thread-screen">
      <TopBar title="Реакции" rightPlain right={<div className="thread-header-actions"><EnergyBadge value={player.postsLeft} />{guest && <span className="crystal-button thread-crystal-count" role="status" aria-label={`Кристаллы в демо: ${guest.crystals}`}>💎 {guest.crystals}</span>}{!guest && onboarding && player.energyWallet && <span className="crystal-button thread-crystal-count" data-onboarding-crystal-counter role="status" aria-label={`Кристаллы: ${player.energyWallet.crystals}`}>💎 {player.energyWallet.crystals}</span>}{!guest?.replySubmitted && <button className="thread-exit" onClick={goBack}>{fromMarked ? 'К отметкам →' : fromFeed ? 'В ленту →' : 'В канал →'}</button>}</div>} />
      <div className="pad">
        {typeof nav.params.guestImportNotice === 'string' && <div className="card" role="status">{nav.params.guestImportNotice}</div>}
        <div className="card" style={{ marginBottom: 12 }}>{shownPost.text}{!guest && <QuotedPost repostId={shownPost.repostId} />}{!guest && <div className="row" style={{ marginTop: 8, justifyContent: 'space-between' }}><div className="post-action-group"><PostLikeAction key={threadKey} postId={shownPost.id} likes={shownPost.likes} likedBy={shownPost.likedBy} onSaved={saved => setPost(current => current ? { ...current, likes: saved.likes, likedBy: saved.likedBy } : current)} />{!shownPost.repostId && <RepostAction postId={shownPost.id} />}</div>{shownPost.authorId === `Player_${player.id.replaceAll('-', '')}` && <button type="button" className="delete-trigger" aria-label="Удалить пост" disabled={deleting} onClick={() => { setDeleteError(''); setConfirmDelete(true) }} style={{ background: 'none', fontSize: 20 }}>🧺</button>}</div>}</div>
        {showHelp && !onboarding && <div className="thread-help"><span className="thread-help-icon">💬</span><div><b>{postAuthor ? 'Обсуди пост с автором' : 'Герой ждёт ответа'}</b><p>{postAuthor ? 'Напиши комментарий к посту или нажми «Ответить» под репликой.' : 'Нажми «Ответить» под репликой и продолжи историю.'}</p><span className="small muted">{postAuthor ? 'Комментарий' : 'Ответ'} · 1 энергия</span></div><button aria-label="Скрыть подсказку" onClick={dismissHelp}>×</button></div>}
        {(!showHelp || !!onboarding) && <div className="small muted" style={{ marginBottom: 16 }}>{postAuthor ? 'Комментарий' : 'Ответ'} · 1 энергия</div>}
        {ordered.map(comment => <div key={comment.id} style={{ marginLeft: Math.min(comment.depth ?? 0, 2) * 14 }}>
          <Comment seed={comment.avatar} avatarStyle={guest?.world.avatarStyle} name={comment.author} handle={comment.star ? comment.handle : comment.role ?? comment.handle} text={comment.hidden ? 'Комментарий скрыт' : comment.text} star={comment.star} me={comment.me} />
          {!!comment.reactions?.length && <div className="small" style={{ margin: '-6px 0 12px 44px' }}>{comment.reactions.map(reaction => <span key={`${reaction.authorId}:${reaction.emoji}`} title={reaction.author} className="react">{reaction.emoji} {reaction.author}</span>)}</div>}
          {!guest?.replySubmitted && isStarComment(comment) && <button data-game-onboarding-post={!targetAvailable ? shownPost.id : undefined} className="small" disabled={sending} onClick={() => focusReply(comment.id)} style={{ display: 'block', margin: '-8px 0 12px 44px', background: 'none', border: 'none', color: draft.commentId === comment.id ? 'var(--accent)' : 'var(--lilac)' }}>{draft.commentId === comment.id ? 'Отвечаешь этому комментарию' : `Ответить герою ${comment.author}`}</button>}
        </div>)}
        {activeTypingName && <div className="card" role="status" aria-live="polite"><div className="small muted">{activeTypingName} печатает...</div><div className="live-current" aria-hidden="true"><span /></div></div>}
        {targetAvailable && !guest?.replySubmitted && <div className="small muted" style={{ marginBottom: 8 }}>{draft.commentId === null ? 'Комментарий к посту' : 'Отвечаешь'}: {recipient}</div>}
        {postAuthor && draft.commentId !== null && <button disabled={sending} onClick={() => { edit({ commentId: null }); requestAnimationFrame(() => replyInput.current?.focus()) }} style={{ background: 'none', border: 'none', color: 'var(--lilac)', marginBottom: 8 }}>К посту</button>}
        {draft.commentId && !targetAvailable && <div className="small muted" role="status" style={{ marginBottom: 8 }}>Этому аккаунту больше нельзя ответить. Выбери другой комментарий — текст ответа сохранён.</div>}
        {replyError && <div className="small" role="alert" style={{ color: 'var(--accent)', marginBottom: 12 }}>{replyError}</div>}
        {!guest && !gameOnboardingActive(onboarding) && <Button secondary disabled={sending} onClick={() => { tap('thread_open_next_post'); nav.replace('02-1_compose') }}>Написать следующий пост</Button>}
      </div>
      {confirmDelete && <Sheet onClose={() => { if (!deleting) setConfirmDelete(false) }}>
        <h2>Удалить пост?</h2>
        <p style={{ margin: '8px 0 16px' }}>Пост и все комментарии будут удалены. Отменить это действие нельзя.</p>
        {deleteError && <p role="alert" style={{ color: 'var(--warn)', marginBottom: 12 }}>{deleteError}</p>}
        <Button disabled={deleting} onClick={removePost} style={{ marginBottom: 8, background: 'transparent', border: '2px solid #F06473', color: '#F06473' }}>{deleting ? 'Удаляем…' : 'Удалить пост'}</Button>
        <Button secondary disabled={deleting} onClick={() => setConfirmDelete(false)}>Отмена</Button>
      </Sheet>}
      {!guest?.replySubmitted && <div className="bottom-cta thread-reply" data-game-onboarding-post={targetAvailable ? shownPost.id : undefined}>
        {guest?.hint}
        <div className="card thread-reply-field"><textarea ref={replyInput} className="thread-reply-input" rows={1} aria-label="Текст ответа" value={draft.text} onChange={event => edit({ text: event.target.value })} placeholder={targetAvailable ? (draft.commentId === null ? 'Комментарий · 1 энергия' : 'Ответ · 1 энергия') : 'Выбери комментарий звезды'} maxLength={1000} disabled={!targetAvailable || sending} /></div>
        <button className="card thread-reply-send" onClick={send} disabled={sending || !targetAvailable || !draft.text.trim() || (player.postsLeft < 1 && !draft.submitted)} style={{ padding: '14px 18px', background: 'var(--grad)', border: 'none' }} aria-label={draft.submitted ? 'Повторить ответ' : 'Отправить за 1 энергию'}>➤</button>
      </div>}
    </div>
  )
}

// 02-7: «Отправить в чат» через нативный шеринг Telegram или в сторис. Чаты игрока мы не видим.
function Share() {
  const nav = useNav()
  const player = usePlayer()
  const { link, error: linkError, retry } = useReferralLink()
  const post = (nav.params.post as Post | undefined) ?? firstPost
  const text = `${post.text}\n\nМой пост в Реках 🌊`
  const [shareError, setShareError] = useState('')
  const toChat = async () => {
    if (!link) return
    track('share', { to: 'chat' }); setShareError('')
    try { if (inTelegram) shareURL.ifAvailable(link, text); else if (navigator.share) await navigator.share({ title: 'Реки', text, url: link }); else window.open(`https://t.me/share/url?url=${encodeURIComponent(link)}&text=${encodeURIComponent(text)}`, '_blank', 'noopener,noreferrer') }
    catch (cause) { if (!(cause instanceof DOMException && cause.name === 'AbortError')) setShareError('Не получилось открыть отправку. Попробуй ещё раз.') }
  }
  // Публичную картинку 9:16 Telegram загружает без заголовков авторизации.
  const storyCardUrl = new URL(`${apiBaseUrl ?? ''}/cards/story/${encodeURIComponent(player.id)}/${encodeURIComponent(post.id)}.png`, location.origin).toString()
  const toStory = () => { if (!link) return; track('share', { to: 'story' }); if (inTelegram) shareStory.ifAvailable(storyCardUrl, { text, widgetLink: { url: link, name: 'Играть' } }) }
  return (
    <div className="screen no-tab">
      <TopBar onBack={nav.back} title="Поделиться" />
      <div className="pad">
        <div style={{ background: 'linear-gradient(160deg,#2A1236,#141420)', borderRadius: 14, padding: 14, marginBottom: 12 }}>
          <div className="small" style={{ color: 'var(--gift)' }}>РЕКИ · МОЙ ПОСТ</div>
          <div style={{ margin: '8px 0' }}>{post.text}</div>
          <div className="small muted">👁 {fmt(post.views)}</div>
          <div className="small" style={{ marginTop: 8 }}>а ты кем будешь в {contentOf(player.world).where}? <b>▶ Играть</b></div>
        </div>
        <Gift style={{ marginBottom: 12 }}>Позови друга в Реки</Gift>
        <div className="small muted">Telegram откроет список твоих чатов. Мы его не видим и никому не пишем сами.</div>
      </div>
      {linkError && <div className="pad" role="alert">Не удалось получить твою ссылку.<Button secondary onClick={retry}>Попробовать снова</Button></div>}
      {shareError && <p className="pad small" role="alert">{shareError}</p>}
      <BottomCTA disabled={!link} secondDisabled={!link} second={inTelegram && shareStory.isAvailable() ? '＋ В сторис' : undefined} onSecond={toStory} main="Отправить в чат" onMain={() => void toChat()} />
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
