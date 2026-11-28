import { useEffect, useLayoutEffect, useRef, useState, type RefObject } from 'react'
import { createPortal } from 'react-dom'
import { useNav } from '../app/nav'
import { gameOnboardingActive, useGameOnboarding } from '../app/GameOnboardingState'
import { event, tap } from '../app/analytics'
import { finishDailyTaskIntro, showDailyTaskIntro, useDailyTaskIntro } from '../app/dailyTaskIntro'
import { usePlayer, useStore } from '../app/store'
import type { Player } from '../api/types'
import { contentOf } from '../api/worldContent'
import { Sheet, plural } from '.'

export const formatDailyNumber = new Intl.NumberFormat('ru-RU').format
const dailyRefreshTime = '09:00 МСК'
const inlineViews = new Map<string, string>()

function observeView(element: HTMLElement, onView: () => void) {
  if (!('IntersectionObserver' in window)) return
  const observer = new IntersectionObserver(([entry]) => {
    if (document.hidden || !entry?.isIntersecting || entry.intersectionRatio < 0.5) return
    const { x, y, width, height } = entry.intersectionRect
    if (!element.contains(document.elementFromPoint(x + width / 2, y + height / 2))) return
    stop()
    onView()
  }, { threshold: 0.5 })
  const observe = () => {
    observer.disconnect()
    if (!document.hidden) observer.observe(element)
  }
  const stop = () => {
    observer.disconnect()
    document.removeEventListener('visibilitychange', observe)
  }
  document.addEventListener('visibilitychange', observe)
  observe()
  return stop
}

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

function DailyTaskPreview({ tasks, initialIndex, blockRef }: { tasks: ReturnType<typeof dailyTasksFor>; initialIndex: number; blockRef: RefObject<HTMLElement | null> }) {
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

  return <section ref={blockRef} className="daily-editor card" aria-label="Ежедневные задания">
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
    {tasks.length ? <div ref={track} data-daily-inline-content className="daily-editor-track" onScroll={event => {
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

export function DailyTasks({ editor = false, repost = false, inline = false, source = 'channel', firstPublishedPostId }: {
  editor?: boolean; repost?: boolean; inline?: boolean; source?: 'channel' | 'profile'; firstPublishedPostId?: string
}) {
  const player = usePlayer()
  const { refresh } = useStore()
  const nav = useNav()
  const onboarding = useGameOnboarding()
  const learning = gameOnboardingActive(onboarding)
  const intro = useDailyTaskIntro(player.id)
  const blockRef = useRef<HTMLElement>(null)
  const introRef = useRef<HTMLElement>(null)
  const day = new Date().toISOString().slice(0, 10)
  const context = `${player.id}:${player.worldInstanceId}:${day}`
  const [opened, setOpened] = useState<{ context: string; explain: boolean }>()
  const [retry, setRetry] = useState({ context: '', busy: false, failed: false })
  const busy = retry.context === context && retry.busy
  const failed = retry.context === context && retry.failed
  const tasks = dailyTasksFor(player)
  const remaining = tasks.filter(task => !task.done)
  const done = tasks.length - remaining.length
  const completed = tasks.length > 0 && remaining.length === 0
  const offerIntro = inline && tasks.length > 0 && (!onboarding || onboarding.completionSeen === true)
    && !!firstPublishedPostId && intro?.postId === firstPublishedPostId
  const kind = editor ? 'editor' : 'channel'
  const hasInlineContent = editor ? remaining.length > 0 : inline && tasks.length > 0
  const isOpen = opened?.context === context
  const requested = editor ? remaining.findIndex(task => task.type === nav.params.dailyTask) : -1
  const previewKey = editor ? `${context}:${repost}:${done}:${requested}` : ''
  useEffect(() => {
    if (offerIntro && !intro?.shown && introRef.current) {
      return observeView(introRef.current, () => showDailyTaskIntro(player.id))
    }
  }, [offerIntro, intro?.shown, player.id])
  useEffect(() => {
    if (!hasInlineContent || !player.worldInstanceId || isOpen || learning) return
    const key = `reki:daily-tasks-inline-view:v1:${player.id}:${player.worldInstanceId}:${kind}`
    if (inlineViews.get(key) === day) return
    const content = blockRef.current?.querySelector<HTMLElement>('[data-daily-inline-content]')
    if (!content) return
    return observeView(content, () => {
      if (inlineViews.get(key) === day) return
      inlineViews.set(key, day)
      try {
        if (localStorage.getItem(key) === day) return
        localStorage.setItem(key, day)
      } catch { /* Session fallback. */ }
      tap('daily_tasks_inline_viewed', { kind })
    })
  }, [hasInlineContent, player.id, player.worldInstanceId, day, kind, completed, isOpen, learning, offerIntro, previewKey])

  if (editor) {
    if (completed) return null
    const suggested = remaining.findIndex(task => repost ? task.type === 'reposts' : task.compose)
    const initialIndex = requested >= 0 ? requested : Math.max(0, suggested)
    return <DailyTaskPreview key={previewKey} tasks={remaining} initialIndex={initialIndex} blockRef={blockRef} />
  }

  const close = () => setOpened(undefined)
  const show = (explain = false) => {
    if (explain) finishDailyTaskIntro(player.id, 'opened')
    setOpened({ context, explain })
    tap('daily_tasks_viewed', { kind: source })
  }
  const reload = async () => {
    setRetry({ context, busy: true, failed: false })
    const next = await refresh()
    setRetry({ context, busy: false, failed: !next })
  }
  const action = (task: typeof tasks[number]) => {
    tap('daily_task_action', { kind: task.type, to: task.compose ? '02-1_compose' : '03-3_feed' })
    close()
    if (task.compose) nav.push('02-1_compose', { dailyTask: task.type })
    else nav.tab(1)
  }
  const status = tasks.length ? `${done}/${tasks.length}` : 'Посмотреть'
  return <>
    {inline ? <section ref={blockRef} className="daily-channel card" aria-label="Задания дня">
      <div className="daily-channel-head">
        <h2>Задания дня</h2>
        {completed ? <span className="small daily-channel-done" data-daily-inline-content>{done} из {tasks.length} ✓</span>
          : <span className="tiny muted">Обновление в {dailyRefreshTime}</span>}
      </div>
      {offerIntro && <aside className="daily-intro" ref={introRef} aria-label="Подсказка о заданиях">
        <b>Есть первый пост!</b>
        <p className="small">Задания подскажут, что можно попробовать дальше</p>
        <div className="daily-intro-actions">
          <button type="button" className="daily-quest-action" onClick={() => show(true)}>Посмотреть задания</button>
          <button type="button" className="text-button small" onClick={() => finishDailyTaskIntro(player.id, 'dismissed')}>Позже</button>
        </div>
      </aside>}
      {!completed && (tasks.length ? <ul className="daily-channel-list">
        {tasks.map((task, index) => {
          const content = <>
            <span className="daily-quest-symbol" aria-hidden="true">{taskSymbols[task.type as TaskType]}</span>
            <span className="daily-channel-title">{task.title}</span>
            <span className={task.done ? 'daily-channel-done' : 'muted'} aria-label={task.done ? 'Выполнено' : `${task.progress} из ${task.target}`}>{task.done ? '✓' : `${formatDailyNumber(task.progress)}/${formatDailyNumber(task.target)}`}</span>
          </>
          return <li key={task.type} data-daily-inline-content={index === 0 || undefined}>
            {task.done || (learning && !task.compose) ? <div className="daily-channel-task">{content}</div> : <button type="button" className="daily-channel-task daily-channel-action" data-wide-count={task.target >= 10_000 || undefined} onClick={() => action(task)}>
              {content}<span className="daily-channel-arrow" aria-hidden="true">›</span>
            </button>}
          </li>
        })}
      </ul> : <div className="daily-channel-empty">
        <p className="small muted" role="status">{failed ? 'Не получилось обновить задания.' : 'Задания пока недоступны.'}</p>
        <button type="button" className="text-button" onClick={reload} disabled={busy}>{busy ? 'Обновляем…' : 'Обновить'}</button>
      </div>)}
    </section> : <button className="daily-entry" onClick={() => show()}><span>Задания дня</span><span>{status} ›</span></button>}
    {isOpen && createPortal(<Sheet onClose={close}>
      <div className="daily-panel" role="dialog" aria-label="Задания дня" onKeyDown={e => { if (e.key === 'Escape') close() }}>
        <div className="daily-panel-head">
          <div><h2>Задания дня</h2><p>Плановое обновление в {dailyRefreshTime}</p></div>
          <button className="daily-icon-button" aria-label="Обновить задания" title="Обновить задания" disabled={busy} onClick={reload}>{busy ? '…' : '↻'}</button>
          <button className="daily-icon-button" aria-label="Закрыть задания" onClick={close} autoFocus>×</button>
        </div>
        {opened?.explain && <div className="daily-intro">
          <p className="small">Пиши посты и комментарии как обычно. После сохранения действия прогресс обновится автоматически.</p>
          <button type="button" className="text-button small" onClick={() => {
            setOpened({ context, explain: false })
            event('onboarding_step', { step: 'daily_tasks_intro_done', ok: true })
          }}>Понятно</button>
        </div>}
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
