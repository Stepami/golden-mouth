import { useEffect, useRef, useState } from 'react'

const REFRESH_DISTANCE = 60

/** Native scrolling loads older posts; pulling down from the top refreshes the list. */
export function usePostListGestures(key: string, list: {
  nextCursor: string | null; hasNext: boolean; pending: string | null; error?: string
  loadMore: () => Promise<unknown>
}, refresh: () => Promise<unknown>) {
  const screenRef = useRef<HTMLDivElement>(null)
  const tailRef = useRef<HTMLDivElement>(null)
  const latest = useRef({ pending: list.pending, refresh })
  latest.current = { pending: list.pending, refresh }
  const [distance, setDistance] = useState(0)

  useEffect(() => {
    const tail = tailRef.current
    if (!tail || !list.hasNext || list.pending || list.error) return
    const observer = new IntersectionObserver(entries => {
      if (entries.some(entry => entry.isIntersecting)) void list.loadMore()
    }, { rootMargin: '0px 0px 200px 0px' })
    observer.observe(tail)
    return () => observer.disconnect()
  }, [key, list.nextCursor, list.hasNext, list.pending, list.error, list.loadMore])

  useEffect(() => {
    const screen = screenRef.current
    if (!screen) return
    let start: { x: number; y: number } | undefined
    let pulled = 0
    let suppressClickUntil = 0
    setDistance(0)
    const startPull = (event: TouchEvent) => {
      start = event.touches.length === 1 && window.scrollY <= 0 && !latest.current.pending
        ? { x: event.touches[0].clientX, y: event.touches[0].clientY } : undefined
      pulled = 0
    }
    const movePull = (event: TouchEvent) => {
      if (!start || event.touches.length !== 1) return
      const dy = event.touches[0].clientY - start.y
      const dx = Math.abs(event.touches[0].clientX - start.x)
      if (dy <= 0 || window.scrollY > 0 || dx > dy) {
        start = undefined
        pulled = 0
      } else {
        if (event.cancelable) event.preventDefault()
        pulled = Math.min(90, dy / 2)
      }
      setDistance(pulled)
    }
    const finishPull = () => {
      if (pulled > 8) suppressClickUntil = performance.now() + 400
      if (pulled >= REFRESH_DISTANCE && !latest.current.pending) void latest.current.refresh()
      start = undefined
      pulled = 0
      setDistance(0)
    }
    const cancelPull = () => { start = undefined; pulled = 0; setDistance(0) }
    const suppressClick = (event: MouseEvent) => {
      if (performance.now() >= suppressClickUntil) return
      event.preventDefault()
      event.stopImmediatePropagation()
    }
    screen.addEventListener('touchstart', startPull, { passive: true })
    screen.addEventListener('touchmove', movePull, { passive: false })
    screen.addEventListener('touchend', finishPull)
    screen.addEventListener('touchcancel', cancelPull)
    screen.addEventListener('click', suppressClick, true)
    return () => {
      screen.removeEventListener('touchstart', startPull)
      screen.removeEventListener('touchmove', movePull)
      screen.removeEventListener('touchend', finishPull)
      screen.removeEventListener('touchcancel', cancelPull)
      screen.removeEventListener('click', suppressClick, true)
    }
  }, [key])

  return { screenRef, tailRef, distance, releaseToRefresh: distance >= REFRESH_DISTANCE }
}
