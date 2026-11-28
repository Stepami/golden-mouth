import { hapticFeedback } from '@tma.js/sdk'
import { useCallback, useEffect, useRef } from 'react'
import { inTelegram } from '../telegram'

type Impact = 'medium' | 'heavy' | 'rigid' | 'success'
const cues = {
  'crowd-open': { priority: 0, pattern: [55], impacts: ['medium'] },
  'crowd-wave': { priority: 0, pattern: [85, 60, 65], impacts: ['heavy', 'medium'] },
  'crowd-settle': { priority: 0, pattern: [65], impacts: ['rigid'] },
  task: { priority: 1, pattern: [95, 65, 85], impacts: ['heavy', 'medium'] },
  day: { priority: 2, pattern: [125, 95, 140, 85, 145], impacts: ['success', 'heavy', 'rigid'] },
} satisfies Record<string, { priority: number; pattern: number[]; impacts: Impact[] }>

export type ResultCue = keyof typeof cues
export type CrowdCue = Extract<ResultCue, `crowd-${string}`>

export function useResultHaptics() {
  const active = useRef({ priority: -1, until: 0, timers: [] as number[], vibrating: false })
  const stop = useCallback(() => {
    const state = active.current
    state.timers.forEach(timer => window.clearTimeout(timer))
    state.timers.length = 0
    if (state.vibrating) {
      try { navigator.vibrate?.(0) } catch { /* The platform may reject a haptic request. */ }
    }
    state.vibrating = false
    state.until = 0
  }, [])
  useEffect(() => {
    const motion = window.matchMedia('(prefers-reduced-motion: reduce)')
    const pause = () => { if (document.hidden || motion.matches) stop() }
    document.addEventListener('visibilitychange', pause)
    window.addEventListener('pagehide', stop)
    motion.addEventListener('change', pause)
    return () => {
      stop()
      document.removeEventListener('visibilitychange', pause)
      window.removeEventListener('pagehide', stop)
      motion.removeEventListener('change', pause)
    }
  }, [stop])
  return useCallback((cue: ResultCue) => {
    if (document.hidden || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return
    const state = active.current
    const { priority, pattern, impacts } = cues[cue]
    const now = performance.now()
    if (now < state.until && priority <= state.priority) return
    stop()
    state.priority = priority
    state.until = now + pattern.reduce((total, duration) => total + duration, 0)
    try {
      if (inTelegram && hapticFeedback.impactOccurred.isAvailable()) {
        let delay = 0
        impacts.forEach((style, index) => {
          const play = () => {
            if (document.hidden || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return
            try {
              if (style === 'success') hapticFeedback.notificationOccurred.ifAvailable('success')
              else hapticFeedback.impactOccurred.ifAvailable(style)
            } catch { /* The platform may reject a haptic request. */ }
          }
          if (!delay) play()
          else state.timers.push(window.setTimeout(play, delay))
          delay += pattern[index * 2] + (pattern[index * 2 + 1] ?? 0)
        })
      } else {
        state.vibrating = navigator.vibrate?.(pattern) ?? false
      }
    } catch { /* Feedback is optional on the platform. */ }
  }, [stop])
}
