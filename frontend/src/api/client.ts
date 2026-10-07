// Клиент API. Runtime config выбирает backend или тестовые данные (mock.ts).
// Во фронте НЕТ секретов: runtime config виден в браузере. Ключи LLM и токен бота живут только на бэкенде.
import * as mock from './mock'
import { error, warn } from '../app/log'
import { apiBaseUrl as BASE } from '../app/runtimeConfig'
import { telegramAuthHeaders } from './auth'
import type { Challenge, Dm, FeedItem, Friend, GameEvent, LiveEvent, Player, Post, PostResult, PublicationState, PublicationStatus, ReplyResult, WorldRules } from './types'

export class ApiError extends Error {
  constructor(public readonly status: number, method: string, path: string) {
    super(`${method} ${path.replace(/\/[\w-]{16,}/g, '/:id')} ${status}`)
    this.name = 'ApiError'
  }
}

async function call<T>(path: string, init?: RequestInit): Promise<T> {
  const r = await fetch(`${BASE}${path}`, { ...init, headers: { 'Content-Type': 'application/json', ...telegramAuthHeaders(), ...init?.headers } })
  if (!r.ok) { const e = new ApiError(r.status, init?.method ?? 'GET', path); if (r.status >= 500) error('api_error', e, { status: r.status }); else warn('api_error', { status: r.status, msg: e.message }); throw e }
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

export const api = {
  me: (): Promise<Player> => BASE ? call('/me') : later(mock.player),
  onboarding: (body: { birthGroup: '14-17' | '18+'; world: string; role: string; name: string; handle: string; address: string; star: string }): Promise<Player> =>
    BASE ? call('/onboarding', { method: 'POST', body: JSON.stringify(body) }) : later({ ...mock.player, subs: 120, postsLeft: 10 }),
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
        const r = await fetch(`${BASE}/posts/publications/${encodeURIComponent(publicationId)}/moderation`, {
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
          const r = await fetch(`${BASE}/posts/publications`, {
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
    const steps: [number, LiveEvent][] = [[250, { type: 'views', views: 48 }], [650, { type: 'react', emoji: '😂' }], [1100, { type: 'comment', comment: mock.liveComment }],
      [1450, { type: 'views', views: 190 }], [1800, { type: 'react', emoji: '🫶' }], [2250, { type: 'typing', who: 'Соня' }], [2650, { type: 'react', emoji: '🔥' }],
      [3000, { type: 'views', views: 312 }], [3900, { type: 'result', result: mock.results.normal as PostResult }]]
    const timers = steps.map(([ms, e]) => setTimeout(() => onEvent(e), ms))
    return () => timers.forEach(clearTimeout)
  },
  reply: (postId: string, commentId: string, replyId: string, text: string): Promise<ReplyResult> =>
    BASE
      ? call(`/posts/${postId}/replies`, { method: 'POST', body: JSON.stringify({ commentId, replyId, text }) })
      : later(mock.threadReply(replyId)),
  report: (commentId: string, reason: string) => BASE ? call('/reports', { method: 'POST', body: JSON.stringify({ commentId, reason }) }) : later({ ok: true }),
  plusInterest: () => BASE ? call('/plus-interest', { method: 'POST' }) : later({ bonus: 5 }),
  settings: (patch: Partial<Player['settings']>) => BASE ? call('/settings', { method: 'PATCH', body: JSON.stringify(patch) }) : later({ ok: true }),
  feed: (): Promise<FeedItem[]> => BASE ? call('/feed') : later(mock.feed),
  post: (postId: string): Promise<Post> => BASE ? call(`/posts/${encodeURIComponent(postId)}`) : laterLazy(() => mock.post(postId)),
  friends: (): Promise<Friend[]> => BASE ? call('/friends') : later(mock.friends),
  dm: (starId: string): Promise<Dm> => BASE ? call(`/dm/${starId}`) : later(mock.dm),
  dmSend: (starId: string, text: string): Promise<{ reply?: string; repliesLeft: number }> => BASE ? call(`/dm/${starId}`, { method: 'POST', body: JSON.stringify({ text }) }) : later({ repliesLeft: 2 }),
  forget: (starId: string, fact: string) => BASE ? call(`/stars/${starId}/memory`, { method: 'DELETE', body: JSON.stringify({ fact }) }) : later({ ok: true }),
  eventToday: (): Promise<GameEvent> => BASE ? call('/events/today') : later(mock.events.collab),
  answerEvent: (id: string, text: string) => BASE ? call(`/events/${id}/answer`, { method: 'POST', body: JSON.stringify({ text }) }) : later({ ok: true }),
  challenge: (): Promise<Challenge | null> => BASE ? call('/challenges/active') : later(mock.challenge),
  startChallenge: (friendId: string) => BASE ? call('/challenges', { method: 'POST', body: JSON.stringify({ friendId }) }) : later({ ok: true }),
  worldRules: (worldId: string): Promise<WorldRules> => BASE ? call(`/worlds/${worldId}/rules`) : later(worldId === 'own' ? mock.ownWorld : mock.presetRules),
  saveWorld: (rules: WorldRules) => BASE ? call('/worlds/own', { method: 'PUT', body: JSON.stringify(rules) }) : later({ ok: true }),
  deleteAccount: () => BASE ? call('/me', { method: 'DELETE' }) : later({ ok: true }),
}
