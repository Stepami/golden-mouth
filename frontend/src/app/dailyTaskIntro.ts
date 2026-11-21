import { useSyncExternalStore } from 'react'
import { event } from './analytics'

type Intro = { postId: string; shown?: true }
const prefix = 'reki:daily-tasks-intro:v1:'
const memory = new Map<string, Intro | null>()
const listeners = new Set<() => void>()

function read(playerId: string) {
  if (!memory.has(playerId)) {
    try {
      const saved = localStorage.getItem(prefix + playerId)
      if (saved) memory.set(playerId, JSON.parse(saved) as Intro | null)
    } catch { /* Session fallback. */ }
  }
  return memory.get(playerId)
}

function write(playerId: string, intro: Intro | null) {
  memory.set(playerId, intro)
  try { localStorage.setItem(prefix + playerId, JSON.stringify(intro)) } catch { /* Session fallback. */ }
  listeners.forEach(listener => listener())
}

const subscribe = (listener: () => void) => { listeners.add(listener); return () => { listeners.delete(listener) } }

export function useDailyTaskIntro(playerId: string) {
  return useSyncExternalStore(subscribe, () => read(playerId))
}

export function rememberDailyTaskPost(playerId: string, postId: string, firstInOnboarding: boolean) {
  const intro = read(playerId)
  if (intro && intro.postId !== postId) write(playerId, null)
  else if (intro === undefined && firstInOnboarding) write(playerId, { postId })
}

export function showDailyTaskIntro(playerId: string) {
  const intro = read(playerId)
  if (!intro || intro.shown) return
  write(playerId, { ...intro, shown: true })
  event('onboarding_step', { step: 'daily_tasks_intro_shown', ok: true })
}

export function finishDailyTaskIntro(playerId: string, step: 'opened' | 'dismissed') {
  if (!read(playerId)) return
  showDailyTaskIntro(playerId)
  write(playerId, null)
  event('onboarding_step', { step: `daily_tasks_intro_${step}`, ok: true })
}
