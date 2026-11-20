import { useEffect, useRef } from 'react'
import type { Player } from '../api/types'
import { usePlayer } from '../app/store'
import { dailyTasksFor, formatDailyNumber } from './DailyTasks'

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
    return before && task.progress > before.progress ? [{ ...task, previousProgress: before.progress, completedNow: task.done && !before.done }] : []
  }) : []
  const changeKey = changes.map(task => task.type).join(',')
  useEffect(() => {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return
    const animations: Animation[] = []
    const frame = requestAnimationFrame(() => {
      const counter = document.querySelector<HTMLElement>('[data-daily-count]')
      changes.forEach(task => {
        const key = `${player.id}:${player.worldInstanceId}:${baseline?.day}:${postId}:${task.type}`
        if (animated.has(key)) return
        const row = root.current?.querySelector<HTMLElement>(`[data-daily-result="${task.type}"]`)
        if (!row) return
        animated.add(key)
        if (task.completedNow) {
          const mark = row.querySelector<HTMLElement>('.daily-result-mark')
          if (mark) animations.push(mark.animate([
            { transform: 'scale(.75)', opacity: .45 },
            { transform: 'scale(1.15)', opacity: 1, offset: .57 },
            { transform: 'scale(1)', opacity: 1 },
          ], { duration: 1050, easing: 'ease-out' }))
          animations.push(row.animate([
            { backgroundColor: '#6bffb82b' },
            { backgroundColor: '#6bffb82b', offset: .55 },
            { backgroundColor: 'transparent' },
          ], { duration: 1550, easing: 'ease-in-out' }))
          if (counter) animations.push(counter.animate([
            { color: 'var(--up)', transform: 'scale(1)' },
            { color: 'var(--up)', transform: 'scale(1.1)' }, { transform: 'scale(1)' },
          ], { delay: 850, duration: 520 }))
          return
        }
        const fraction = (progress: number) => Math.min(1, progress / Math.max(task.target, 1))
        const fill = row.querySelector<HTMLElement>('.daily-result-bar > span')
        if (fill) animations.push(fill.animate([
          { transform: `scaleX(${fraction(task.previousProgress)})` },
          { transform: `scaleX(${fraction(task.progress)})` },
        ], { duration: 1150, easing: 'cubic-bezier(.34,.05,.23,1)' }))
        const before = row.querySelector<HTMLElement>('.daily-result-count-before')
        const after = row.querySelector<HTMLElement>('.daily-result-count-after')
        if (before && after) {
          animations.push(before.animate([{ opacity: 1 }, { opacity: 1, offset: .65 }, { opacity: 0 }], 1150))
          animations.push(after.animate([{ opacity: 0 }, { opacity: 0, offset: .65 }, { opacity: 1 }], 1150))
        }
      })
    })
    return () => { cancelAnimationFrame(frame); animations.forEach(animation => animation.cancel()) }
  }, [postId, player.id, player.worldInstanceId, baseline?.day, changeKey])
  if (!changes.length) return null
  return <section ref={root} className="daily-result" aria-label="Прогресс заданий дня" role="status" aria-live="polite">
    {changes.map(task => <div className={`daily-result-row${task.completedNow ? ' is-complete' : ''}`} key={task.type} data-daily-result={task.type} data-wide-count={!task.completedNow && task.target >= 10_000 || undefined}>
      <span className="daily-result-mark" aria-hidden="true">{task.completedNow ? '✓' : '↗'}</span>
      <div>
        <b>{task.completedNow ? 'Задание дня выполнено' : 'Пост пошёл в зачёт'}</b>
        <p>{task.title}</p>
        {!task.completedNow && <div className="daily-result-bar" role="progressbar" aria-label={`Прогресс: ${task.title}`} aria-valuenow={task.progress} aria-valuemax={task.target}>
          <span style={{ transform: `scaleX(${Math.min(1, task.progress / Math.max(task.target, 1))})` }} />
        </div>}
      </div>
      {!task.completedNow && <span className="daily-result-count">
        <span className="daily-result-count-before" aria-hidden="true">{formatDailyNumber(task.previousProgress)} из {formatDailyNumber(task.target)}</span>
        <span className="daily-result-count-after">{formatDailyNumber(task.progress)} из {formatDailyNumber(task.target)}</span>
      </span>}
    </div>)}
  </section>
}
