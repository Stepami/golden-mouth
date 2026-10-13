// Клиент API. Runtime config выбирает backend или тестовые данные (mock.ts).
// Во фронте НЕТ секретов: runtime config виден в браузере. Ключи LLM и токен бота живут только на бэкенде.
import { clearPlayerDrafts } from '../app/postDraft'
import * as mock from './mock'
import { error, warn } from '../app/log'
import { setAnalyticsWorld, setTelemetryPlayerAvailable } from '../app/analytics'
import { apiBaseUrl as BASE, runtimeConfig } from '../app/runtimeConfig'
import { demoChannelPosts, demoOwnPosts, demoPlayer, demoPost, resetDemoPlayer, saveDemoPlayer, saveDemoPost, withDemoComments } from './demo'
import { contentOf, installWorldContent, installWorldTemplates, worldDocumentOf, wantsFor } from './worldContent'
import { telegramAuthHeaders } from './auth'
import type { AgeBand } from '../app/ageBand'
import type { PlayerDocument, Challenge, ChannelPost, Dm, FeedItem, Friend, GameEvent, LiveEvent, Player, Post, PostDetailResponse, PostResult, PublicationState, PublicationStatus, PublicationFailure, ReplyLiveEvent, WorldDocument, WorldDefinition, WorldId, WorldUpdate } from './types'

let sessionPlayer: Player | undefined
let sessionIdentity: PlayerDocument | undefined
let templates: WorldDocument[] = []
let templatesRequest: Promise<void> | undefined
const inWorld = (path: string): string => {
  if (!sessionPlayer?.worldInstanceId) throw new Error('World must be loaded before this request')
  return `${path}?worldId=${encodeURIComponent(sessionPlayer.worldInstanceId)}`
}
async function fetchTemplates(): Promise<void> {
  try {
    const loaded = await call<WorldDocument[]>('/worlds/templates', undefined, false)
    if (!loaded.length) throw new Error('No world templates returned')
    installWorldTemplates(loaded)
    templates = loaded
  } catch (cause) {
    // A template 404 is a configuration failure, not a missing player.
    throw new Error('Unable to load world templates', { cause })
  }
}
function loadTemplates(): Promise<void> {
  if (templates.length) return Promise.resolve()
  return templatesRequest ??= fetchTemplates().finally(() => { templatesRequest = undefined })
}
function rememberPlayer(player: Player): Player {
  try { if (player.worldInstanceId) localStorage.setItem(`reki:selected-world:${player.id}`, player.worldInstanceId) } catch { /* memory fallback */ } setTelemetryPlayerAvailable(true); sessionIdentity = { ...player, energy: player.postsLeft, experiments: player.experiments ?? {} }; sessionPlayer = player; setAnalyticsWorld(player.worldInstanceId, player.world); return player }

export class ApiError extends Error {
  constructor(public readonly status: number, method: string, path: string, public readonly failure?: PublicationFailure) {
    super(`${method} ${path.replace(/\/[\w-]{16,}/g, '/:id')} ${status}`)
    this.name = 'ApiError'
  }
}

async function call<T>(path: string, init?: RequestInit, authenticated = true): Promise<T> {
  const r = await fetch(`${BASE ?? runtimeConfig.apiBaseUrl}${path}`, { ...init, headers: { 'Content-Type': 'application/json', ...(authenticated ? telegramAuthHeaders() : {}), ...init?.headers } })
  if (!r.ok) { let failure: PublicationFailure | undefined; try { failure = (await r.json() as { failure?: PublicationFailure }).failure } catch { /* untyped transport error */ }; const e = new ApiError(r.status, init?.method ?? 'GET', path, failure); if (r.status >= 500) error('api_error', e, { status: r.status }); else if (r.status !== 404 || path !== '/me') warn('api_error', { status: r.status, msg: e.message }); throw e }
  const body = await r.text()
  return body ? JSON.parse(body) as T : undefined as T
}
const later = <T,>(v: T, ms = 150) => new Promise<T>(res => setTimeout(() => res(structuredClone(v)), ms))
const laterLazy = <T,>(load: () => T, ms = 150) => new Promise<T>((resolve, reject) => setTimeout(() => { try { resolve(structuredClone(load())) } catch (e) { reject(e) } }, ms))

async function readSse<T>(response: Response, onEvent: (event: T) => void | Promise<void>): Promise<void> {
  if (!response.body) throw new Error('Response has no event stream')
  const reader = response.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''
  for (;;) {
    const { done, value } = await reader.read()
    buffer += done ? decoder.decode() : decoder.decode(value, { stream: true })
    for (;;) {
      const match = /\r?\n\r?\n/.exec(buffer)
      if (!match) break
      const frame = buffer.slice(0, match.index)
      buffer = buffer.slice(match.index + match[0].length)
      const data = frame.split(/\r?\n/).filter(line => line.startsWith('data:')).map(line => line.slice(5).trimStart()).join('\n')
      if (data) await onEvent(JSON.parse(data) as T)
    }
    if (done) break
  }
}

async function publicationState(id: string): Promise<PublicationState> {
  return call(`/posts/publications/${encodeURIComponent(id)}`)
}

const playerRequests = new Map<string, Promise<Player>>()
async function loadPlayer(worldId?: string): Promise<Player> {
  if (!BASE) {
    // Demo worlds use template content because they have no server-side instances.
    await loadTemplates()
    installWorldContent(templates)
    return rememberPlayer(await laterLazy(demoPlayer))
  }
  const player = await call<PlayerDocument>('/me')
  sessionIdentity = player
  setTelemetryPlayerAvailable(true)
  const ownedWorlds = await call<WorldDocument[]>('/worlds')
  let selected: string | undefined
  try { selected = localStorage.getItem(`reki:selected-world:${player.id}`) ?? undefined } catch { /* memory fallback */ }
  const world = ownedWorlds.find(item => item.id === (worldId ?? sessionPlayer?.worldInstanceId ?? selected)) ?? ownedWorlds[0]
  if (!world) throw new ApiError(404, 'GET', '/worlds')
  installWorldContent([world])
  const invitation = await call<{ bonus: number }>('/invitation')
  return rememberPlayer({ ...player, postsLeft: player.energy, postsCap: Math.max(player.energy, player.settings.plus ? 20 : 10), firstPostInviteBonus: invitation.bonus, worldInstanceId: world.id, avatarStyle: world.avatarStyle,
    world: world.balanceKey, name: world.name, handle: world.avatar, role: world.role, subs: world.subscribers,
    wants: world.wants, collection: world.achievements, bio: '', type: 'свой голос',
    voice: world.voice, drama: world.drama,
  })
}

export const api = {
  loadWorldTemplates: loadTemplates,
  me: (worldId?: string): Promise<Player> => {
    const key = worldId ?? sessionPlayer?.worldInstanceId ?? ''
    const pending = playerRequests.get(key)
    if (pending) return pending
    const request = loadPlayer(worldId).finally(() => { playerRequests.delete(key) })
    playerRequests.set(key, request)
    return request
  },
  playerId: (): string | undefined => sessionIdentity?.id,
  hasPlayer: (): boolean => sessionIdentity !== undefined,
  onboarding: async (body: { birthGroup: '14-17' | '18+'; ageBand?: AgeBand; address: string; referralCode?: string }): Promise<PlayerDocument> => {
    if (sessionIdentity) throw new Error('Onboarding has already been completed')
    sessionIdentity = BASE
      ? await call<PlayerDocument>('/onboarding', { method: 'POST', body: JSON.stringify(body) })
      : await later({
        id: crypto.randomUUID().replaceAll('-', ''), address: body.address as Player['address'],
        energy: 10, streak: { days: 1, week: [true, false, false, false, false, false, false], nextReward: 'ещё день — и бонус' },
        skills: { brave: { level: 1, progress: 0 }, humor: { level: 1, progress: 0 }, topic: { level: 1, progress: 0 } },
        settings: { notifications: 'all', quiet: '22:00-09:00', plus: false }, experiments: {},
      })
    setTelemetryPlayerAvailable(true)
    return sessionIdentity
  },
  createWorld: async (definition: WorldDefinition): Promise<Player> => {
    if (BASE) {
      const world = await call<WorldDocument>('/worlds', { method: 'POST', body: JSON.stringify(definition) })
      return api.me(world.id)
    }
    if (!sessionIdentity) throw new Error('Player must be loaded before creating a world')
    return rememberPlayer(await laterLazy(() => saveDemoPlayer({
      ...structuredClone(mock.player), ...sessionIdentity!, worldInstanceId: crypto.randomUUID().replaceAll('-', ''), world: definition.balanceKey,
      name: definition.name, handle: definition.avatar, role: definition.role, avatarStyle: definition.avatarStyle,
      voice: definition.voice, drama: definition.drama,
      subs: 120, wants: definition.wants, collection: definition.achievements,
    })))
  },
  /** Генерация, затем модерация и анимация массива реакций; при обрыве сверяем сохранённый статус. */
  publish(text: string, publicationId: string, onEvent: (e: LiveEvent) => void, onError: (state: 'failed' | 'unknown', reason?: PublicationFailure) => void, onStatus?: (status: PublicationStatus) => void, ownWords = true): () => void {
    if (BASE) {
      const ctrl = new AbortController()
      let deliveredResult = false
      const pause = (ms: number): Promise<void> => {
        if (ctrl.signal.aborted) return Promise.reject(new DOMException('Aborted', 'AbortError'))
        if (document.hidden) return Promise.resolve()
        return new Promise((resolve, reject) => {
          const finish = () => { clearTimeout(timer); ctrl.signal.removeEventListener('abort', onAbort); document.removeEventListener('visibilitychange', onVisibilityChange) }
          const onAbort = () => { finish(); reject(new DOMException('Aborted', 'AbortError')) }
          const onVisibilityChange = () => { if (document.hidden) { finish(); resolve() } }
          const timer = setTimeout(() => { finish(); resolve() }, ms)
          ctrl.signal.addEventListener('abort', onAbort, { once: true })
          document.addEventListener('visibilitychange', onVisibilityChange)
          if (ctrl.signal.aborted) onAbort()
          else if (document.hidden) onVisibilityChange()
        })
      }
      const moderation = async () => {
        const r = await fetch(`${BASE}${inWorld(`/posts/publications/${encodeURIComponent(publicationId)}/moderation`)}`, {
          method: 'POST', signal: ctrl.signal, headers: { Accept: 'application/json', ...telegramAuthHeaders() },
        })
        if (!r.ok) throw new ApiError(r.status, 'POST', '/posts/publications/:id/moderation')
        const events = await r.json() as LiveEvent[]
        let lastShownAt = 0
        const minimumGap: Record<LiveEvent['type'], number> = {
          views: 750, react: 800, comment: 950, typing: 950, result: 1300,
        }
        for (const event of events) {
          if (ctrl.signal.aborted) throw new DOMException('Aborted', 'AbortError')
          const remaining = minimumGap[event.type] - (performance.now() - lastShownAt)
          if (lastShownAt && remaining > 0) await pause(remaining)
          lastShownAt = performance.now()
          if (event.type === 'result') deliveredResult = true
          onEvent(event)
        }
        if (!deliveredResult) throw new Error('Post response has no result')
      }
      const recover = async (): Promise<void> => {
        let state: PublicationState
        try { state = await publicationState(publicationId) } catch { onError('unknown'); return }
        if (state.status === 'completed' && state.result) {
          if (!deliveredResult) { deliveredResult = true; onEvent({ type: 'result', result: state.result }) }
        } else if (state.status === 'ready_for_moderation') {
          onError('unknown')
        } else if (state.status === 'failed') {
          onError('failed', state.failure)
        } else {
          onError('unknown')
        }
      }
      // StrictMode immediately unmounts the first effect in development.
      // Defer the request so that probe does not create an abandoned publication.
      const start = setTimeout(() => { void (async () => {
        try {
          const r = await fetch(`${BASE}${inWorld("/posts/publications")}`, {
            method: 'POST', signal: ctrl.signal,
            headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream', ...telegramAuthHeaders() },
            body: JSON.stringify({ publicationId, text, ownWords }),
          })
          if (!r.ok) { let failure: PublicationFailure | undefined; try { failure = (await r.json() as { failure?: PublicationFailure }).failure } catch { /* transport */ }; throw new ApiError(r.status, 'POST', '/posts/publications', failure) }
          let lastStatus: PublicationStatus | null = null
          let failure: PublicationFailure | undefined
          await readSse<{ type: PublicationStatus; publicationId: string; failure?: PublicationFailure }>(r, event => { lastStatus = event.type; failure = event.failure; onStatus?.(event.type) })
          if (lastStatus === 'failed') { onError('failed', failure); return }
          if (lastStatus === 'completed') { await recover(); return }
          if (lastStatus !== 'ready_for_moderation') { await recover(); return }
          await moderation()
        } catch (e) {
          if ((e as Error).name === 'AbortError') return
          error('sse_error', e)
          if (e instanceof ApiError && e.failure) onError('failed', e.failure)
          else if (e instanceof ApiError && e.status >= 400 && e.status < 500 && e.status !== 409) onError('failed')
          else await recover()
        }
      })() }, 0)
      return () => { clearTimeout(start); ctrl.abort() }
    }
    onStatus?.('pending')
    const profile = contentOf(sessionPlayer?.world ?? 'stage')
    const star = profile.stars[0]
    const comment = { ...mock.liveComment, id: crypto.randomUUID(), author: star.name, avatar: star.id, handle: star.role, text: 'Вот это история! Расскажешь, что было дальше?', star: true }
    const result = { ...mock.results.normal, postId: publicationId, headline: `${star.name} замечает твой пост`,
      bestComment: { avatar: star.id, text: comment.text }, hook: `${profile.stars[1].name} готовит ответ…` }
    const steps: [number, LiveEvent][] = [[250, { type: 'views', views: 48 }], [650, { type: 'react', emoji: '😂' }], [1100, { type: 'comment', comment }],
      [1450, { type: 'views', views: 190 }], [1800, { type: 'react', emoji: '🫶' }], [2250, { type: 'typing', who: profile.stars[1].name }], [2650, { type: 'react', emoji: '🔥' }],
      [3000, { type: 'views', views: 312 }], [3900, { type: 'result', result }]]
    const timers = steps.map(([ms, e]) => setTimeout(() => {
      if (e.type === 'result' && sessionPlayer) {
        saveDemoPost(sessionPlayer.id, { id: publicationId, text, views: result.views, reactions: [{ emoji: '😂', count: 1 }, { emoji: '🫶', count: 1 }, { emoji: '🔥', count: 1 }], comments: [comment], canReply: false, demoComments: true, at: new Date().toISOString() })
        sessionPlayer = saveDemoPlayer({ ...sessionPlayer, subs: sessionPlayer.subs + result.delta, postsLeft: Math.max(0, sessionPlayer.postsLeft - 1) })
      }
      onEvent(e)
    }, ms * 1.1))
    return () => timers.forEach(clearTimeout)
  },
  reply: async (postId: string, commentId: string, requestId: string, text: string,
    options: { signal?: AbortSignal; onEvent?: (event: ReplyLiveEvent) => void } = {}): Promise<ReplyLiveEvent> => {
    const signal = options.signal
    const path = BASE ? inWorld(`/posts/${encodeURIComponent(postId)}/replies`) : ''
    const player = sessionPlayer
    if (!player) throw new Error('Player must be loaded')
    const update = (event: ReplyLiveEvent) => {
      options.onEvent?.(event)
      if (event.status === 'completed' && event.energyLeft != null && event.subscribers != null && sessionPlayer?.id === player.id && sessionPlayer.worldInstanceId === player.worldInstanceId) {
        sessionPlayer = { ...sessionPlayer, postsLeft: event.energyLeft, subs: event.subscribers }
        if (!BASE) saveDemoPlayer(sessionPlayer)
      }
    }
    if (!BASE) {
      const post = await api.post(postId)
      const existing = post.comments.find(comment => comment.requestId === requestId)
      if (existing && (existing.text !== text || existing.parentCommentId !== commentId)) throw new ApiError(409, 'POST', '/posts/:id/replies')
      if (existing) {
        const event: ReplyLiveEvent = { status: 'completed', requestId, post: { ...post, postId: post.id }, energyLeft: player.postsLeft, subscribers: player.subs }
        update(event); return event
      }
      if (player.postsLeft < 1) throw new ApiError(409, 'POST', '/posts/:id/replies')
      const target = post.comments.find(comment => comment.id === commentId && comment.star && !comment.hidden)
      if (!target) throw new ApiError(409, 'POST', '/posts/:id/replies')
      update({ status: 'generating', requestId })
      await later(undefined, 650)
      if (signal?.aborted) throw new DOMException('Aborted', 'AbortError')
      const responseText = `Интересная мысль! Теперь хочется узнать продолжение.`
      update({ status: 'generating', requestId, text: responseText })
      const id = crypto.randomUUID()
      const saved: Post = { ...post, comments: [...post.comments,
        { id, parentCommentId: target.id, requestId, authorId: player.id, author: player.name, avatar: player.handle, handle: player.handle, text, me: true, depth: (target.depth ?? 0) + 1, reactions: [] },
        { id: crypto.randomUUID(), parentCommentId: id, authorId: target.authorId ?? target.avatar, author: target.author, avatar: target.avatar, handle: target.handle, text: responseText, star: true, depth: (target.depth ?? 0) + 2, reactions: [] }] }
      const bonus = post.comments.filter(comment => comment.me && comment.parentCommentId).length < 3 ? 9 : 0
      saveDemoPost(player.id, saved)
      const event: ReplyLiveEvent = { status: 'completed', requestId, post: { ...saved, postId: saved.id }, energyLeft: player.postsLeft - 1, subscribers: player.subs + bonus }
      update(event); return event
    }
    let completed: ReplyLiveEvent | undefined
    try {
      const response = await fetch(`${BASE}${path}`, { method: 'POST', signal,
        headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream', ...telegramAuthHeaders() },
        body: JSON.stringify({ requestId, commentId, text }) })
      if (!response.ok) throw new ApiError(response.status, 'POST', '/posts/:id/replies')
      await readSse<ReplyLiveEvent>(response, event => {
        update(event)
        if (event.status === 'failed') throw new Error('Reply generation failed')
        if (event.status === 'completed' && event.post && event.energyLeft != null && event.subscribers != null) completed = event
      })
      if (!completed) throw new Error('Reply stream ended before completion')
      return completed
    } catch (cause) {
      if (signal?.aborted || (cause instanceof ApiError && cause.status >= 400 && cause.status < 500)) throw cause
      // A lost final SSE event does not imply that the transaction failed.
      const query = path.slice(path.indexOf('?'))
      const post = await call<PostDetailResponse>(`/posts/${encodeURIComponent(postId)}${query}`)
      const saved = post.comments.find(comment => comment.requestId === requestId && comment.parentCommentId === commentId && comment.text === text)
      if (!saved) throw cause
      const [identity, worlds] = await Promise.all([call<PlayerDocument>('/me'), call<WorldDocument[]>('/worlds')])
      const world = worlds.find(value => value.id === player.worldInstanceId)
      if (!world) throw cause
      const event: ReplyLiveEvent = { status: 'completed', requestId, post, energyLeft: identity.energy, subscribers: world.subscribers }
      update(event); return event
    }
  },
  report: (commentId: string, reason: string) => BASE ? call('/reports', { method: 'POST', body: JSON.stringify({ commentId, reason }) }) : later({ ok: true }),
  plusInterest: async (interest = true): Promise<{ bonus: number }> => {
    if (BASE) return call('/plus-interest', { method: 'POST', body: JSON.stringify({ interest }) })
    const key = `reki:demo-plus-claimed:${sessionPlayer?.id}`
    const claimed = localStorage.getItem(key) === 'true'
    const bonus = claimed ? 0 : 5
    if (!claimed && sessionPlayer) { sessionPlayer = saveDemoPlayer({ ...sessionPlayer, postsLeft: sessionPlayer.postsLeft + bonus, postsCap: sessionPlayer.postsCap + bonus }); localStorage.setItem(key, 'true') }
    return later({ bonus })
  },
  referral: (): Promise<{ code: string }> => BASE ? call('/referral') : laterLazy(() => ({ code: `ref_${sessionPlayer!.id === 'p1' ? mock.player.id : sessionPlayer!.id.replaceAll('-', '')}` })),
  settings: async (patch: Partial<Player['settings']> & { address?: Player['address'] }) => {
    if (BASE) await call('/settings', { method: 'PATCH', body: JSON.stringify(patch) })
    if (sessionPlayer) {
      const { address, ...settings } = patch
      sessionPlayer = { ...sessionPlayer, address: address ?? sessionPlayer.address, settings: { ...sessionPlayer.settings, ...settings } }
      if (!BASE) saveDemoPlayer(sessionPlayer)
    }
  },
  feed: async (world: WorldId = sessionPlayer?.world ?? 'stage'): Promise<FeedItem[]> => {
    const remoteData = BASE ? await Promise.all([call<FeedItem[]>(inWorld('/feed')), call<ChannelPost[]>(inWorld('/posts/mine'))]) : undefined
    const ownedPostIds = new Set(remoteData?.[1].map(post => post.postId))
    const remote: FeedItem[] = remoteData ? remoteData[0].map(item => ({ ...item, canReply: !!item.postId && ownedPostIds.has(item.postId) })) : demoOwnPosts(sessionPlayer?.id ?? '').slice(0, 20).map(post => ({
      kind: 'post', postId: post.id, canReply: true, author: sessionPlayer?.name, avatar: sessionPlayer?.handle, role: sessionPlayer?.role,
      ago: post.at, text: post.text, reactions: post.reactions, views: post.views,
      commentCount: withDemoComments(post, sessionPlayer?.id ?? '').comments.length,
    }))
    return remote
  },
  channelPosts: (): Promise<ChannelPost[]> => BASE ? call(inWorld('/posts/mine')) : laterLazy(() => demoChannelPosts(sessionPlayer?.id ?? '')),
  post: async (postId: string): Promise<Post> => {
    const demo = !BASE ? demoPost(postId) : undefined
    const local = !BASE ? demoOwnPosts(sessionPlayer?.id ?? '').find(item => item.id === postId) : undefined
    const post = local ?? demo ?? (BASE
      ? await call<PostDetailResponse>(inWorld(`/posts/${encodeURIComponent(postId)}`)).then(({ postId: id, ...detail }) => ({ ...detail, id }))
      : await laterLazy(() => demoOwnPosts(sessionPlayer?.id ?? '').find(item => item.id === postId) ?? mock.post(postId)))
    if (!post) throw { status: 404 }
    return !BASE
      ? withDemoComments({ ...post, demoComments: post.demoComments || !post.canReply }, sessionPlayer?.id ?? '') : post
  },
  friends: (): Promise<Friend[]> => BASE ? call('/friends') : later(mock.friends),
  dm: (starId: string): Promise<Dm> => BASE ? call(inWorld(`/dm/${starId}`)) : laterLazy(() => {
    const star = contentOf(sessionPlayer?.world ?? 'stage').stars.find(profile => profile.id === starId)
    if (!star) throw new ApiError(404, 'GET', `/dm/${starId}`)
    return { ...mock.dm, star }
  }),
  dmSend: (starId: string, text: string): Promise<{ reply?: string; repliesLeft: number }> => BASE ? call(inWorld(`/dm/${starId}`), { method: 'POST', body: JSON.stringify({ text }) }) : later({ repliesLeft: 2 }),
  forget: (starId: string, fact: string) => BASE ? call(inWorld(`/stars/${starId}/memory`), { method: 'DELETE', body: JSON.stringify({ fact }) }) : later({ ok: true }),
  eventToday: (): Promise<GameEvent> => BASE ? call(inWorld('/events/today')) : later(mock.events.collab),
  answerEvent: (id: string, text: string) => BASE ? call(inWorld(`/events/${id}/answer`), { method: 'POST', body: JSON.stringify({ text }) }) : later({ ok: true }),
  challenge: (): Promise<Challenge | null> => BASE ? call('/challenges/active') : later(mock.challenge),
  startChallenge: (friendId: string) => BASE ? call('/challenges', { method: 'POST', body: JSON.stringify({ friendId }) }) : later({ ok: true }),
  ownedWorlds: (): Promise<WorldDocument[]> => BASE ? call('/worlds') : later(sessionPlayer ? [worldDocumentOf(sessionPlayer.world)] : []),
  saveWorld: async (worldId: string, definition: WorldDefinition): Promise<Player> => {
    if (BASE) {
      await call(`/worlds/${encodeURIComponent(worldId)}`, { method: 'PUT', body: JSON.stringify(definition) })
      return api.me(worldId)
    }
    installWorldContent([{ ...definition, id: worldId, subscribers: sessionPlayer?.subs ?? 120 }])
    return rememberPlayer({ ...sessionPlayer!, name: definition.name, handle: definition.avatar, role: definition.role, voice: definition.voice, drama: definition.drama })
  },
  updateWorld: async (patch: WorldUpdate) => {
    if (!sessionPlayer?.worldInstanceId) throw new Error('World must be loaded before this request')
    const current = (await api.ownedWorlds()).find(world => world.id === sessionPlayer!.worldInstanceId)
    if (!current) throw new Error('World not found')
    const { id, subscribers: _subs, lock: _lock, ...definition } = current
    return api.saveWorld(id, { ...definition, ...patch })
  },
  deleteAccount: async () => { if (BASE) await call('/me', { method: 'DELETE' }); if (sessionIdentity) { clearPlayerDrafts(sessionIdentity.id); try { localStorage.removeItem(`reki:selected-world:${sessionIdentity.id}`) } catch { /* optional storage */ } } resetDemoPlayer(sessionPlayer?.id); sessionPlayer = undefined; sessionIdentity = undefined; setTelemetryPlayerAvailable(false); setAnalyticsWorld(undefined) },
}
