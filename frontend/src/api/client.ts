// Клиент API. Без VITE_API_URL работает на тестовых данных (mock.ts).
// Во фронте НЕТ секретов: всё из import.meta.env попадает в браузер. Ключи LLM и токен бота живут только на бэкенде.
import * as mock from './mock'
import type { Challenge, Dm, FeedItem, Friend, GameEvent, LiveEvent, Player, PostResult, WorldRules } from './types'

const BASE = import.meta.env.VITE_API_URL as string | undefined
let initDataRaw = ''
export const setInitData = (raw: string) => { initDataRaw = raw }

async function call<T>(path: string, init?: RequestInit): Promise<T> {
  const r = await fetch(`${BASE}${path}`, { ...init, headers: { 'Content-Type': 'application/json', Authorization: `tma ${initDataRaw}`, ...init?.headers } })
  if (!r.ok) throw new Error(`${path}: ${r.status}`)
  return r.json() as Promise<T>
}
const later = <T,>(v: T, ms = 150) => new Promise<T>(res => setTimeout(() => res(structuredClone(v)), ms))

export const api = {
  me: (): Promise<Player> => BASE ? call('/me') : later(mock.player),
  onboarding: (body: { birthGroup: '14-17' | '18+'; world: string; role: string; avatar: string; name: string; handle: string; address: string; star: string }): Promise<Player> =>
    BASE ? call('/onboarding', { method: 'POST', body: JSON.stringify(body) }) : later({ ...mock.player, subs: 120, postsLeft: 10 }),
  /** Публикация: поток событий реакции, последним приходит итог. */
  publish(text: string, onEvent: (e: LiveEvent) => void): () => void {
    if (BASE) {
      const ctrl = new AbortController()
      fetch(`${BASE}/posts`, { method: 'POST', signal: ctrl.signal, headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream', Authorization: `tma ${initDataRaw}` }, body: JSON.stringify({ text }) })
        .then(async r => {
          const reader = r.body!.getReader(); const dec = new TextDecoder(); let buf = ''
          for (;;) { const { done, value } = await reader.read(); if (done) break; buf += dec.decode(value, { stream: true })
            let i; while ((i = buf.indexOf('\n\n')) >= 0) { const chunk = buf.slice(0, i); buf = buf.slice(i + 2); const data = chunk.replace(/^data: /gm, ''); if (data) onEvent(JSON.parse(data)) } }
        }).catch(() => {})
      return () => ctrl.abort()
    }
    const steps: [number, LiveEvent][] = [[200, { type: 'views', views: 48 }], [500, { type: 'react', emoji: '😂' }], [800, { type: 'comment', comment: mock.liveComment }],
      [1100, { type: 'views', views: 190 }], [1300, { type: 'react', emoji: '🫶' }], [1600, { type: 'typing', who: 'Соня' }], [1900, { type: 'react', emoji: '🔥' }],
      [2200, { type: 'views', views: 312 }], [2900, { type: 'result', result: mock.results.normal as PostResult }]]
    const timers = steps.map(([ms, e]) => setTimeout(() => onEvent(e), ms))
    return () => timers.forEach(clearTimeout)
  },
  reply: (postId: string, text: string) => BASE ? call(`/posts/${postId}/replies`, { method: 'POST', body: JSON.stringify({ text }) }) : later({ ok: true }),
  report: (commentId: string, reason: string) => BASE ? call('/reports', { method: 'POST', body: JSON.stringify({ commentId, reason }) }) : later({ ok: true }),
  plusInterest: () => BASE ? call('/plus-interest', { method: 'POST' }) : later({ bonus: 5 }),
  settings: (patch: Partial<Player['settings']>) => BASE ? call('/settings', { method: 'PATCH', body: JSON.stringify(patch) }) : later({ ok: true }),
  feed: (): Promise<FeedItem[]> => BASE ? call('/feed') : later(mock.feed),
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
