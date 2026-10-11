// Клиент API. Runtime config выбирает backend или тестовые данные (mock.ts).
// Во фронте НЕТ секретов: runtime config виден в браузере. Ключи LLM и токен бота живут только на бэкенде.
import * as mock from './mock'
import { error, warn } from '../app/log'
import { setAnalyticsWorld, setTelemetryPlayerAvailable } from '../app/analytics'
import { apiBaseUrl as BASE, runtimeConfig } from '../app/runtimeConfig'
import { addDemoComment, demoChannelPosts, demoFeed, demoOwnPosts, demoPlayer, demoPost, resetDemoPlayer, saveDemoPlayer, saveDemoPost, shuffle, withDemoComments } from './demo'
import { contentOf, installWorldContent, installWorldTemplates, worldDocumentOf, wantsFor } from './worldContent'
import { telegramAuthHeaders } from './auth'
import type { AgeBand } from '../app/ageBand'
import type { PlayerDocument, Challenge, ChannelPost, Dm, FeedItem, Friend, GameEvent, LiveEvent, Player, Post, PostDetailResponse, PostResult, PublicationState, PublicationStatus, ReplyResult, WorldDocument, WorldDefinition, WorldId, WorldUpdate } from './types'

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
function rememberPlayer(player: Player): Player { setTelemetryPlayerAvailable(true); sessionIdentity = { ...player, experiments: player.experiments ?? {} }; sessionPlayer = player; setAnalyticsWorld(player.worldInstanceId, player.world); return player }

export class ApiError extends Error {
  constructor(public readonly status: number, method: string, path: string) {
    super(`${method} ${path.replace(/\/[\w-]{16,}/g, '/:id')} ${status}`)
    this.name = 'ApiError'
  }
}

async function call<T>(path: string, init?: RequestInit, authenticated = true): Promise<T> {
  const r = await fetch(`${BASE ?? runtimeConfig.apiBaseUrl}${path}`, { ...init, headers: { 'Content-Type': 'application/json', ...(authenticated ? telegramAuthHeaders() : {}), ...init?.headers } })
  if (!r.ok) { const e = new ApiError(r.status, init?.method ?? 'GET', path); if (r.status >= 500) error('api_error', e, { status: r.status }); else if (r.status !== 404 || path !== '/me') warn('api_error', { status: r.status, msg: e.message }); throw e }
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
  const world = ownedWorlds.find(item => item.id === (worldId ?? sessionPlayer?.worldInstanceId)) ?? ownedWorlds[0]
  if (!world) throw new ApiError(404, 'GET', '/worlds')
  installWorldContent([world])
  return rememberPlayer({ ...player, worldInstanceId: world.id, avatarStyle: world.avatarStyle,
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
  hasPlayer: (): boolean => sessionIdentity !== undefined,
  onboarding: async (body: { birthGroup: '14-17' | '18+'; ageBand?: AgeBand; address: string; referralCode?: string }): Promise<PlayerDocument> => {
    if (sessionIdentity) throw new Error('Onboarding has already been completed')
    sessionIdentity = BASE
      ? await call<PlayerDocument>('/onboarding', { method: 'POST', body: JSON.stringify(body) })
      : await later({
        id: crypto.randomUUID().replaceAll('-', ''), address: body.address as Player['address'],
        postsLeft: 10, postsCap: 10, firstPostInviteBonus: 0, streak: { days: 1, week: [true, false, false, false, false, false, false], nextReward: 'ещё день — и бонус' },
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
  /** Генерация, затем модерация и поток реакции; при обрыве сверяем сохранённый статус. */
  publish(text: string, publicationId: string, onEvent: (e: LiveEvent) => void, onError: (state: 'failed' | 'unknown') => void, onStatus?: (status: PublicationStatus) => void): () => void {
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
          method: 'POST', signal: ctrl.signal, headers: { Accept: 'text/event-stream', ...telegramAuthHeaders() },
        })
        if (!r.ok) throw new ApiError(r.status, 'POST', '/posts/publications/:id/moderation')
        let lastShownAt = 0
        const minimumGap: Record<LiveEvent['type'], number> = {
          views: 650, react: 700, comment: 850, typing: 850, result: 1200,
        }
        await readSse<LiveEvent>(r, async event => {
          const remaining = minimumGap[event.type] - (performance.now() - lastShownAt)
          if (lastShownAt && remaining > 0) await pause(remaining)
          lastShownAt = performance.now()
          if (event.type === 'result') deliveredResult = true
          onEvent(event)
        })
        if (!deliveredResult) throw new Error('Post event stream ended before result')
      }
      const recover = async (): Promise<void> => {
        let state: PublicationState
        try { state = await publicationState(publicationId) } catch { onError('unknown'); return }
        if (state.status === 'completed' && state.result) {
          if (!deliveredResult) { deliveredResult = true; onEvent({ type: 'result', result: state.result }) }
        } else if (state.status === 'ready_for_moderation') {
          onError('unknown')
        } else if (state.status === 'failed') {
          onError('failed')
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
            body: JSON.stringify({ publicationId, text }),
          })
          if (!r.ok) throw new ApiError(r.status, 'POST', '/posts/publications')
          let lastStatus: PublicationStatus | null = null
          await readSse<{ type: PublicationStatus; publicationId: string }>(r, event => { lastStatus = event.type; onStatus?.(event.type) })
          if (lastStatus === 'failed') { onError('failed'); return }
          if (lastStatus === 'completed') { await recover(); return }
          if (lastStatus !== 'ready_for_moderation') { await recover(); return }
          await moderation()
        } catch (e) {
          if ((e as Error).name === 'AbortError') return
          error('sse_error', e)
          if (e instanceof ApiError && e.status >= 400 && e.status < 500 && e.status !== 409) onError('failed')
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
    }, ms))
    return () => timers.forEach(clearTimeout)
  },
  reply: (postId: string, commentId: string, replyId: string, text: string): Promise<ReplyResult> =>
    BASE
      ? call(inWorld(`/posts/${postId}/replies`), { method: 'POST', body: JSON.stringify({ commentId, replyId, text }) })
      : later(mock.threadReply(replyId)),
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
    const remote: FeedItem[] = BASE ? await call<FeedItem[]>(inWorld('/feed')) : demoOwnPosts(sessionPlayer?.id ?? '').slice(0, 20).map(post => ({
      kind: 'post', postId: post.id, canReply: true, author: sessionPlayer?.name, avatar: sessionPlayer?.handle, role: sessionPlayer?.role,
      ago: post.at, text: post.text, reactions: post.reactions, views: post.views,
      commentCount: withDemoComments(post, sessionPlayer?.id ?? '').comments.length,
    }))
    const demos = runtimeConfig.features.mockFeed ? demoFeed(world).map(item => ({ ...item,
      commentCount: withDemoComments(demoPost(item.postId!)!, sessionPlayer?.id ?? '').comments.length })) : []
    return runtimeConfig.features.mockFeed ? shuffle([...remote, ...demos]) : remote
  },
  channelPosts: (): Promise<ChannelPost[]> => BASE ? call(inWorld('/posts/mine')) : laterLazy(() => demoChannelPosts(sessionPlayer?.id ?? '')),
  post: async (postId: string): Promise<Post> => {
    const demo = runtimeConfig.features.mockFeed ? demoPost(postId) : undefined
    const post = demo ?? (BASE
      ? await call<PostDetailResponse>(inWorld(`/posts/${encodeURIComponent(postId)}`)).then(({ postId: id, ...detail }) => ({ ...detail, id }))
      : await laterLazy(() => demoOwnPosts(sessionPlayer?.id ?? '').find(item => item.id === postId) ?? mock.post(postId)))
    if (!post) throw { status: 404 }
    return runtimeConfig.features.mockFeed
      ? withDemoComments({ ...post, demoComments: post.demoComments || !post.canReply }, sessionPlayer?.id ?? '') : post
  },
  demoComment: (postId: string, player: Player, text: string, replyId: string) => laterLazy(() => addDemoComment(postId, player, text, replyId)),
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
  updateWorld: async (patch: WorldUpdate) => {
    if (BASE) {
      if (!sessionPlayer?.worldInstanceId) throw new Error('World must be loaded before this request')
      await call(`/worlds/${encodeURIComponent(sessionPlayer.worldInstanceId)}`, { method: 'PATCH', body: JSON.stringify(patch) })
    }
    if (sessionPlayer) {
      installWorldContent([{ ...worldDocumentOf(sessionPlayer.world), ...patch }])
      sessionPlayer = { ...sessionPlayer, drama: patch.drama ?? sessionPlayer.drama, voice: patch.voice ?? sessionPlayer.voice }
      if (!BASE) saveDemoPlayer(sessionPlayer)
    }
  },
  deleteAccount: async () => { if (BASE) await call('/me', { method: 'DELETE' }); resetDemoPlayer(sessionPlayer?.id); sessionPlayer = undefined; sessionIdentity = undefined; setTelemetryPlayerAvailable(false); setAnalyticsWorld(undefined) },
}
