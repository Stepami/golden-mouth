import { useEffect, useLayoutEffect, useRef, useState, type CSSProperties } from 'react'
import type { Player } from '../api/types'
import { flyCrystal, pulseCrystalCounter } from './crystalFlight'
import { dailyTasksFor, formatDailyNumber } from './DailyTasks'
import { useResultHaptics, type ResultCue } from './resultHaptics'

export function dailyTaskBaseline(player: Player) {
  const completion = Object.values(player.dailyChallenge?.completion ?? {})
  return {
    playerId: player.id, worldId: player.worldInstanceId, day: player.dailyChallenge?.day, tasks: dailyTasksFor(player),
    complete: completion.length > 0 && completion.every(Boolean),
  }
}
export type DailyTaskBaseline = ReturnType<typeof dailyTaskBaseline>

const animated = new Set<string>()
// DailyProgressBehavior grants this award in the same write as the completed card.
const dailyReward = 2

const storageKey = (key: string) => `reki:daily-result:v1:${key}`

function hasCelebrated(key: string) {
  if (animated.has(key)) return true
  try { return sessionStorage.getItem(storageKey(key)) === '1' }
  catch { return false }
}

function firstCelebration(key: string) {
  if (hasCelebrated(key)) return false
  animated.add(key)
  try { sessionStorage.setItem(storageKey(key), '1') } catch { /* Use the in-memory marker. */ }
  return true
}

function dailyChanges(baseline: DailyTaskBaseline, after?: DailyTaskBaseline) {
  if (!after || after.playerId !== baseline.playerId || after.worldId !== baseline.worldId ||
    after.day !== baseline.day || baseline.day !== new Date().toISOString().slice(0, 10)) return []
  return baseline.tasks.flatMap(before => {
    if (before.done) return []
    const task = after.tasks.find(task => task.type === before.type && task.target === before.target && task.starId === before.starId)
    return task && task.progress > before.progress ? [{ ...task, previousProgress: before.progress }] : []
  })
}

type DailyChange = ReturnType<typeof dailyChanges>[number]
const cardKeyFor = (baseline: DailyTaskBaseline) => `${baseline.playerId}:${baseline.day}`

export function DailyTaskResult({ baseline, afterPost, afterStar, postId, onPulse }: {
  baseline: DailyTaskBaseline; afterPost?: DailyTaskBaseline; afterStar?: Promise<DailyTaskBaseline | undefined>;
  postId: string; onPulse: (cue: ResultCue) => void
}) {
  const [starProgress, setStarProgress] = useState<DailyTaskBaseline>()
  useEffect(() => {
    let active = true
    void afterStar?.then(value => { if (active) setStarProgress(value) })
    return () => { active = false }
  }, [afterStar])
  const changes = dailyChanges(baseline, afterPost ?? starProgress).filter(task =>
    task.type !== 'comments' && task.type !== 'likes' && task.type !== 'starComment')
  changes.push(...dailyChanges(baseline, starProgress).filter(task => task.type === 'starComment'))
  const complete = !baseline.complete && (starProgress ?? afterPost)?.complete === true &&
    changes.some(task => task.done) && baseline.tasks.every(task =>
      task.done || changes.some(change => change.type === task.type && change.done))
  return <DailyTaskFeedback changes={changes} complete={complete} cardKey={cardKeyFor(baseline)}
    progressKey={`${baseline.worldId}:${postId}`} onPulse={onPulse} />
}

function DailyTaskFeedback({ changes, complete, cardKey, progressKey, onPulse }: {
  changes: DailyChange[]; complete: boolean; cardKey: string; progressKey?: string; onPulse: (cue: ResultCue) => void
}) {
  const root = useRef<HTMLElement>(null)
  const rewardFlight = useRef<(() => void) | undefined>(undefined)
  const changeKey = changes.map(task => `${task.type}:${task.progress}:${task.done}`).join(',')
  useLayoutEffect(() => {
    const motion = window.matchMedia('(prefers-reduced-motion: reduce)')
    const still = document.hidden || motion.matches || new URLSearchParams(location.search).has('shot')
    root.current?.querySelectorAll<HTMLElement>('[data-daily-effect]').forEach(element => {
      if (firstCelebration(element.getAttribute('data-daily-effect')!) && !still) element.dataset.dailyAnimate = ''
    })
  }, [progressKey, cardKey, changeKey, complete])

  useEffect(() => {
    const motion = window.matchMedia('(prefers-reduced-motion: reduce)')
    const stop = () => {
      rewardFlight.current?.()
      root.current?.querySelectorAll('[data-daily-animate]').forEach(element => element.removeAttribute('data-daily-animate'))
    }
    const pause = () => { if (document.hidden || motion.matches) stop() }
    document.addEventListener('visibilitychange', pause)
    window.addEventListener('pagehide', stop)
    motion.addEventListener('change', pause)
    return () => {
      rewardFlight.current?.()
      document.removeEventListener('visibilitychange', pause)
      window.removeEventListener('pagehide', stop)
      motion.removeEventListener('change', pause)
    }
  }, [])

  function showReward(label: HTMLElement) {
    onPulse('day')
    const counter = document.querySelector<HTMLElement>('.game-page [data-onboarding-crystal-counter]')
    const end = counter?.querySelector('.onboarding-crystal-icon')?.getBoundingClientRect()
    if (!counter || !end?.width || !end.height) return
    const start = label.getBoundingClientRect()
    const flights = Array.from({ length: dailyReward }, (_, index) => flyCrystal(start, end, index))
    let pulse: Animation | undefined
    const stop = () => {
      flights.forEach(({ gem, animation }) => { animation.cancel(); gem.remove() })
      pulse?.cancel()
      rewardFlight.current = undefined
    }
    rewardFlight.current?.()
    rewardFlight.current = stop
    flights.forEach(({ gem, animation }) => {
      void animation.finished.then(() => {
        gem.remove()
        if (rewardFlight.current !== stop || !counter.isConnected) return
        pulse?.cancel()
        pulse = pulseCrystalCounter(counter)
      }, () => gem.remove())
    })
  }

  if (!changes.length) return null
  return <section ref={root} className="daily-result" aria-label="Прогресс заданий дня" role="status" aria-live="polite">
    {complete && <div className="daily-result-reward" data-daily-effect={`${cardKey}:reward`}
      onAnimationStart={event => {
        const label = event.currentTarget.lastElementChild
        if (event.animationName === 'daily-reward-flash' && label instanceof HTMLElement) showReward(label)
      }}>
      <b>Все задания выполнены</b>
      <span><span aria-hidden="true">💎</span> +{dailyReward} кристалла</span>
    </div>}
    {changes.map(task => {
      const effect = task.done ? `${cardKey}:${task.type}` : `${cardKey}:${progressKey}:${task.type}:progress`
      const fraction = (progress: number) => Math.min(1, progress / Math.max(task.target, 1))
      return <div className={`daily-result-row${task.done ? ' is-complete' : ''}`} key={`${effect}:${task.progress}`}
        data-daily-effect={effect} data-wide-count={task.target >= 10_000 || undefined}>
        <span className="daily-result-mark" aria-hidden="true"
          onAnimationStart={event => { if (event.animationName === 'daily-task-check') onPulse('task') }}>{task.done ? '✓' : '↗'}</span>
        <div>
          {task.done ? <b className="daily-result-value">
            <span className="daily-result-before" aria-hidden="true">Задание дня</span>
            <span className="daily-result-after">Задание дня выполнено</span>
          </b> : <b>Пост пошёл в зачёт</b>}
          <p>{task.title}</p>
          <div className="daily-result-bar" role="progressbar" aria-label={`Прогресс: ${task.title}`}
            aria-valuenow={Math.min(task.progress, task.target)} aria-valuemax={task.target}>
            <span style={{ '--daily-from': fraction(task.previousProgress), '--daily-to': fraction(task.progress) } as CSSProperties} />
          </div>
        </div>
        <span className="daily-result-count daily-result-value">
          <span className="daily-result-before" aria-hidden="true">{formatDailyNumber(task.previousProgress)} из {formatDailyNumber(task.target)}</span>
          <span className="daily-result-after">{formatDailyNumber(Math.min(task.progress, task.target))} из {formatDailyNumber(task.target)}</span>
        </span>
      </div>
    })}
  </section>
}

type Notice = { screen: string; cardKey: string; worldId?: string; changes: DailyChange[]; complete: boolean }

export function DailyTaskNotice({ player, screen }: { player: Player | null; screen: string }) {
  const previous = useRef<DailyTaskBaseline | undefined>(undefined)
  const [notice, setNotice] = useState<Notice>()
  useEffect(() => {
    const before = previous.current
    const after = player ? dailyTaskBaseline(player) : undefined
    previous.current = after
    if (!before || !after) return
    const resultFlow = ['02-2_live', '02-3_result', '02-4_flop', '02-5_viral'].includes(screen)
    const cardKey = cardKeyFor(after)
    const changes = dailyChanges(before, after).filter(task => task.done &&
      (!resultFlow || task.type === 'comments' || task.type === 'likes') &&
      !hasCelebrated(`${cardKey}:${task.type}`))
    if (!changes.length) return
    const complete = !before.complete && after.complete && !hasCelebrated(`${cardKey}:reward`)
    setNotice(current => ({
      screen, cardKey, worldId: after.worldId,
      changes: current?.screen === screen && current.cardKey === cardKey
        ? [...current.changes.filter(previous => !changes.some(task => task.type === previous.type)), ...changes] : changes,
      complete,
    }))
  }, [player, screen])
  useEffect(() => {
    if (!notice) return
    if (notice.screen !== screen) { setNotice(undefined); return }
    const timer = window.setTimeout(() => setNotice(undefined), 6500)
    return () => window.clearTimeout(timer)
  }, [notice, screen])
  if (!player || notice?.screen !== screen || notice.worldId !== player.worldInstanceId ||
      notice.cardKey !== `${player.id}:${player.dailyChallenge?.day}`) return null
  return <DailyNoticeCard key={notice.cardKey} notice={notice} />
}

function DailyNoticeCard({ notice }: { notice: Notice }) {
  const pulse = useResultHaptics()
  return <div className="daily-result-notice">
    <DailyTaskFeedback changes={notice.changes} complete={notice.complete} cardKey={notice.cardKey} onPulse={pulse} />
  </div>
}
