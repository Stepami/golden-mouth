import { useLayoutEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { useNav } from '../app/nav'
import { gameOnboardingActive, useGameOnboarding } from '../app/GameOnboardingState'
import { tap } from '../app/analytics'
import { usePlayer, useStore } from '../app/store'
import type { Player } from '../api/types'
import { contentOf } from '../api/worldContent'
import { Sheet, plural } from '.'

export const formatDailyNumber = new Intl.NumberFormat('ru-RU').format
const dailyRefreshTime = '09:00 МСК'

const taskCopy = {
  posts: { title: (n: number) => `Напиши ${formatDailyNumber(n)} ${plural(n, 'пост', 'поста', 'постов')}`, compose: true },
  reposts: { title: (n: number) => `Сделай ${formatDailyNumber(n)} ${plural(n, 'репост', 'репоста', 'репостов')}`, compose: false },
  comments: { title: (n: number) => `Напиши ${formatDailyNumber(n)} ${plural(n, 'комментарий', 'комментария', 'комментариев')}`, compose: false },
  likes: { title: (n: number) => `Поставь ${formatDailyNumber(n)} ${plural(n, 'лайк', 'лайка', 'лайков')}`, compose: false },
  starComment: { title: () => 'Получи комментарий звезды', compose: true },
  subscribers: { title: (n: number) => `Привлеки ${formatDailyNumber(n)} ${plural(n, 'подписчика', 'подписчиков', 'подписчиков')} публикациями`, compose: true },
  unsubscribers: { title: (n: number) => `Получи ${formatDailyNumber(n)} ${plural(n, 'отписку', 'отписки', 'отписок')} после публикаций`, compose: true },
  postViews: { title: (n: number) => `Набери ${formatDailyNumber(n)} ${plural(n, 'просмотр', 'просмотра', 'просмотров')} на посте`, compose: true },
  postLikes: { title: (n: number) => `Набери ${formatDailyNumber(n)} ${plural(n, 'лайк', 'лайка', 'лайков')} на посте`, compose: true },
}
type TaskType = keyof typeof taskCopy
const taskSymbols: Record<TaskType, string> = {
  posts: '✎', reposts: '↻', comments: '💬', likes: '♡', starComment: '★',
  subscribers: '↗', unsubscribers: '↘', postViews: '◉', postLikes: '♥',
}

export function dailyTasksFor(player: Player) {
  const challenge = player.dailyChallenge
  const values = challenge?.day === new Date().toISOString().slice(0, 10) && player.worldInstanceId ? challenge.values[player.worldInstanceId] : undefined
  return Object.entries(values ?? {}).flatMap(([type, value]) => {
    if (!Object.hasOwn(taskCopy, type)) return []
    const copy = taskCopy[type as TaskType]
    const star = type === 'starComment' ? contentOf(player.world).stars.find(star => star.id === value.starId) : undefined
    return [{ type, ...value, title: star ? `Получи комментарий: ${star.name}` : copy.title(value.target), compose: copy.compose, done: challenge.completion[type] === true }]
  })
}

function DailyTaskPreview({ tasks, initialIndex }: { tasks: ReturnType<typeof dailyTasksFor>; initialIndex: number }) {
  const track = useRef<HTMLDivElement>(null)
  const positioned = useRef(false)
  const [shown, setShown] = useState(initialIndex)
  useLayoutEffect(() => {
    const list = track.current
    if (!list || positioned.current) return
    const slide = list.children.item(initialIndex)
    if (!(slide instanceof HTMLElement)) return
    list.scrollTo({ left: slide.offsetLeft, behavior: 'instant' })
    setShown(initialIndex)
    positioned.current = true
  }, [initialIndex, tasks.length])
  const index = Math.min(shown, Math.max(0, tasks.length - 1))
  const move = (to: number) => {
    const slide = track.current?.children.item(to) as HTMLElement | null | undefined
    if (slide) track.current?.scrollTo({ left: slide.offsetLeft })
  }

  return <section className="daily-editor card" aria-label="Ежедневные задания">
    <div className="daily-editor-head">
      <div>
        <h2>Задания дня</h2>
        {tasks.length > 1 && <p className="tiny muted">Все задания активны</p>}
      </div>
      {tasks.length > 1 && <div className="daily-editor-pager">
        <button type="button" className="text-button" aria-label="Предыдущая карточка" onClick={() => move(index - 1)} disabled={index === 0}>‹</button>
        <span className="tiny muted" aria-label={`Карточка ${index + 1} из ${tasks.length}`}>{index + 1}/{tasks.length}</span>
        <button type="button" className="text-button" aria-label="Следующая карточка" onClick={() => move(index + 1)} disabled={index === tasks.length - 1}>›</button>
      </div>}
    </div>
    {tasks.length ? <div ref={track} className="daily-editor-track" onScroll={event => {
      const target = event.currentTarget
      const second = target.children.item(1) as HTMLElement | null
      if (second?.offsetLeft) setShown(Math.max(0, Math.round(target.scrollLeft / second.offsetLeft)))
    }}>
      {tasks.map(task => <article key={task.type} className="daily-editor-slide" aria-label={task.title}>
        <div className="daily-task-title" data-wide-count={task.target >= 10_000 || undefined}>
          <b>{task.title}</b>
          <b className="muted">{formatDailyNumber(task.progress)}/{formatDailyNumber(task.target)}</b>
        </div>
        <progress aria-label={`Прогресс: ${task.title}`} value={task.progress} max={task.target} />
      </article>)}
    </div> : <p className="small muted">Задания пока недоступны.</p>}
  </section>
}

export function DailyTasks({ editor = false, repost = false, inline = false, source = 'channel' }: { editor?: boolean; repost?: boolean; inline?: boolean; source?: 'channel' | 'profile' }) {
  const player = usePlayer()
  const { refresh } = useStore()
  const nav = useNav()
  const learning = gameOnboardingActive(useGameOnboarding())
  const day = new Date().toISOString().slice(0, 10)
  const context = `${player.id}:${player.worldInstanceId}:${day}`
  const [opened, setOpened] = useState('')
  const [retry, setRetry] = useState({ context: '', busy: false, failed: false })
  const busy = retry.context === context && retry.busy
  const failed = retry.context === context && retry.failed
  const tasks = dailyTasksFor(player)
  const remaining = tasks.filter(task => !task.done)
  const done = tasks.length - remaining.length
  const completed = tasks.length > 0 && remaining.length === 0
  if (editor) {
    if (completed) return null
    const requested = remaining.findIndex(task => task.type === nav.params.dailyTask)
    const suggested = remaining.findIndex(task => repost ? task.type === 'reposts' : task.compose)
    const initialIndex = requested >= 0 ? requested : Math.max(0, suggested)
    return <DailyTaskPreview key={`${context}:${repost}:${done}:${requested}`} tasks={remaining} initialIndex={initialIndex} />
  }

  if (inline && completed) return <section className="daily-channel card" aria-label="Задания дня">
    <div className="daily-channel-head">
      <h2>Задания дня</h2>
      <span className="small daily-channel-done">{done} из {tasks.length} ✓</span>
    </div>
  </section>

  const show = () => {
    setOpened(context)
    tap('daily_tasks_viewed', { kind: source })
  }
  const reload = async () => {
    setRetry({ context, busy: true, failed: false })
    const next = await refresh()
    setRetry({ context, busy: false, failed: !next })
  }
  const action = (task: typeof tasks[number]) => {
    tap('daily_task_action', { kind: task.type, to: task.compose ? '02-1_compose' : '03-3_feed' })
    setOpened('')
    if (task.compose) nav.push('02-1_compose', { dailyTask: task.type })
    else nav.tab(1)
  }
  const status = tasks.length ? `${done}/${tasks.length}` : 'Посмотреть'
  return <>
    {inline ? <section className="daily-channel card" aria-label="Задания дня">
      <div className="daily-channel-head">
        <h2>Задания дня</h2>
        <span className="tiny muted">Обновление в {dailyRefreshTime}</span>
      </div>
      {tasks.length ? <ul className="daily-channel-list">
        {tasks.map(task => {
          const content = <>
            <span className="daily-quest-symbol" aria-hidden="true">{taskSymbols[task.type as TaskType]}</span>
            <span className="daily-channel-title">{task.title}</span>
            <span className={task.done ? 'daily-channel-done' : 'muted'} aria-label={task.done ? 'Выполнено' : `${task.progress} из ${task.target}`}>{task.done ? '✓' : `${formatDailyNumber(task.progress)}/${formatDailyNumber(task.target)}`}</span>
          </>
          return <li key={task.type}>
            {task.done || (learning && !task.compose) ? <div className="daily-channel-task">{content}</div> : <button type="button" className="daily-channel-task daily-channel-action" data-wide-count={task.target >= 10_000 || undefined} onClick={() => action(task)}>
              {content}<span className="daily-channel-arrow" aria-hidden="true">›</span>
            </button>}
          </li>
        })}
      </ul> : <div className="daily-channel-empty">
        <p className="small muted" role="status">{failed ? 'Не получилось обновить задания.' : 'Задания пока недоступны.'}</p>
        <button type="button" className="text-button" onClick={reload} disabled={busy}>{busy ? 'Обновляем…' : 'Обновить'}</button>
      </div>}
    </section> : <button className="daily-entry" onClick={show}><span>Задания дня</span><span data-daily-count>{status} ›</span></button>}
    {opened === context && createPortal(<Sheet onClose={() => setOpened('')}>
      <div className="daily-panel" role="dialog" aria-label="Задания дня" onKeyDown={e => { if (e.key === 'Escape') setOpened('') }}>
        <div className="daily-panel-head">
          <div><h2>Задания дня</h2><p>Плановое обновление в {dailyRefreshTime}</p></div>
          <button className="daily-icon-button" aria-label="Обновить задания" title="Обновить задания" disabled={busy} onClick={reload}>{busy ? '…' : '↻'}</button>
          <button className="daily-icon-button" aria-label="Закрыть задания" onClick={() => setOpened('')} autoFocus>×</button>
        </div>
        <div className="daily-panel-summary"><span>{done} из {tasks.length} выполнено</span><span>Сегодня</span></div>
        <div className="daily-quest-list">
          {tasks.map(task => <div className={`daily-quest-row${task.done ? ' is-done' : ''}`} key={task.type} data-kind={task.type}>
            <div className="daily-quest-heading" data-wide-count={!task.done && task.target >= 10_000 || undefined}><b><span className="daily-quest-symbol" aria-hidden="true">{taskSymbols[task.type as TaskType]}</span>{task.title}</b><span className="daily-quest-count">{task.done ? '✓' : `${formatDailyNumber(task.progress)}/${formatDailyNumber(task.target)}`}</span></div>
            {task.done ? <p className="small muted">Выполнено в одном из миров</p> : <div className="daily-quest-bottom">
              <progress aria-label={`Прогресс: ${task.title}`} value={task.progress} max={task.target} />
              {(task.compose || !learning) && <button className="daily-quest-action" onClick={() => action(task)}>{task.compose ? 'Написать пост' : 'Открыть ленту'}</button>}
            </div>}
          </div>)}
        </div>
        {!tasks.length && <p className="daily-note muted">Задания для этого мира пока недоступны. Можно продолжить играть.</p>}
        {failed && <p className="small daily-update-error" role="status">Не получилось обновить. Нажми ↻, чтобы повторить.</p>}
      </div>
    </Sheet>, document.body)}
  </>
}
