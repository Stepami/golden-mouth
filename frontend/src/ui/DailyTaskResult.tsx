import { useEffect, useRef } from 'react'
import type { Player } from '../api/types'
import { usePlayer } from '../app/store'
import { dailyTasksFor } from './DailyTasks'

export function dailyTaskBaseline(player: Player) {
  return { playerId: player.id, worldId: player.worldInstanceId, day: player.dailyChallenge?.day, tasks: dailyTasksFor(player) }
}
export type DailyTaskBaseline = ReturnType<typeof dailyTaskBaseline>
const animated = new Set<string>()

export function DailyTaskResult({ baseline, postId }: { baseline?: DailyTaskBaseline; postId: string }) {
  const player = usePlayer()
  const root = useRef<HTMLElement>(null)
  const valid = baseline && baseline.playerId === player.id && baseline.worldId === player.worldInstanceId
    && baseline.day === player.dailyChallenge?.day && baseline.day === new Date().toISOString().slice(0, 10)
  const changes = valid ? dailyTasksFor(player).flatMap(task => {
    const before = baseline.tasks.find(previous => previous.type === task.type && previous.target === task.target)
    return before && task.progress > before.progress ? [{ ...task, before: before.progress, delta: task.progress - before.progress, completedNow: task.done && !before.done }] : []
  }) : []
  const changeKey = changes.map(task => task.type).join(',')
  useEffect(() => {
    const animations: Animation[] = []
    const flyers: HTMLElement[] = []
    const frame = requestAnimationFrame(() => {
      const counter = document.querySelector<HTMLElement>('[data-daily-count]')
      changes.forEach(task => {
        const key = `${player.id}:${player.worldInstanceId}:${baseline?.day}:${postId}:${task.type}`
        if (animated.has(key)) return
        animated.add(key)
        if (!counter || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return
        const origin = root.current?.querySelector<HTMLElement>(`[data-daily-result="${task.type}"]`)
        if (!origin) return
        const from = origin.getBoundingClientRect(), to = counter.getBoundingClientRect()
        const flyer = document.createElement('span')
        flyer.className = 'daily-progress-flyer'
        flyer.textContent = `+${task.delta}`
        Object.assign(flyer.style, { left: `${from.right - 28}px`, top: `${from.top}px` })
        document.body.append(flyer)
        flyers.push(flyer)
        const flight = flyer.animate([
          { transform: 'translate(0, 0) scale(1)', opacity: 1 },
          { transform: `translate(${to.left + to.width / 2 - from.right + 28}px, ${to.top + to.height / 2 - from.top}px) scale(.7)`, opacity: 0 },
        ], { duration: 850, easing: 'cubic-bezier(.2,.6,.3,1)', fill: 'forwards' })
        animations.push(flight)
        flight.onfinish = () => {
          flyer.remove()
          if (counter.isConnected) animations.push(counter.animate([
            { color: task.completedNow ? 'var(--up)' : 'var(--lilac)', transform: 'scale(1)' },
            { color: task.completedNow ? 'var(--up)' : 'var(--lilac)', transform: 'scale(1.15)' }, { transform: 'scale(1)' },
          ], { duration: 450 }))
        }
      })
    })
    return () => { cancelAnimationFrame(frame); animations.forEach(animation => animation.cancel()); flyers.forEach(flyer => flyer.remove()) }
  }, [postId, player.id, player.worldInstanceId, baseline?.day, changeKey])
  if (!changes.length) return null
  return <section ref={root} className="daily-result" aria-label="Прогресс заданий дня" role="status" aria-live="polite">
    <b className="small">Задания дня</b>
    {changes.map(task => <div key={task.type} data-daily-result={task.type}>
      <p>{task.title}</p><b>{task.before}/{task.target} → {task.progress}/{task.target}</b>
      {task.completedNow && <span className="small up"> ✓ Выполнено в одном из миров</span>}
    </div>)}
  </section>
}
