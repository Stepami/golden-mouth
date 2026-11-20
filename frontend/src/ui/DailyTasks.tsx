import { useState } from 'react'
import { createPortal } from 'react-dom'
import { useNav } from '../app/nav'
import { tap } from '../app/analytics'
import { usePlayer, useStore } from '../app/store'
import type { Player } from '../api/types'
import { contentOf } from '../api/worldContent'
import { Sheet, fmt, plural } from '.'

const taskCopy = {
  posts: { title: (n: number) => `Напиши ${fmt(n)} ${plural(n, 'пост', 'поста', 'постов')}`, compose: true },
  reposts: { title: (n: number) => `Сделай ${fmt(n)} ${plural(n, 'репост', 'репоста', 'репостов')}`, compose: false },
  comments: { title: (n: number) => `Напиши ${fmt(n)} ${plural(n, 'комментарий', 'комментария', 'комментариев')}`, compose: false },
  likes: { title: (n: number) => `Поставь ${fmt(n)} ${plural(n, 'лайк', 'лайка', 'лайков')}`, compose: false },
  starComment: { title: () => 'Получи комментарий звезды', compose: true },
  subscribers: { title: (n: number) => `Привлеки ${fmt(n)} ${plural(n, 'подписчика', 'подписчиков', 'подписчиков')} публикациями`, compose: true },
  unsubscribers: { title: (n: number) => `Получи ${fmt(n)} ${plural(n, 'отписку', 'отписки', 'отписок')} после публикаций`, compose: true },
  postViews: { title: (n: number) => `Набери ${fmt(n)} ${plural(n, 'просмотр', 'просмотра', 'просмотров')} на одном посте`, compose: true },
  postLikes: { title: (n: number) => `Набери ${fmt(n)} ${plural(n, 'лайк', 'лайка', 'лайков')} при публикации поста`, compose: true },
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

export function DailyTasks({ editor = false, repost = false, inline = false, source = 'channel' }: { editor?: boolean; repost?: boolean; inline?: boolean; source?: 'channel' | 'profile' }) {
  const player = usePlayer()
  const { refresh } = useStore()
  const nav = useNav()
  const day = new Date().toISOString().slice(0, 10)
  const context = `${player.id}:${player.worldInstanceId}:${day}`
  const [opened, setOpened] = useState('')
  const [retry, setRetry] = useState({ context: '', busy: false, failed: false })
  const busy = retry.context === context && retry.busy
  const failed = retry.context === context && retry.failed
  const tasks = dailyTasksFor(player)
  const done = tasks.filter(task => task.done).length
  const suitable = tasks.filter(task => repost ? task.type === 'reposts' : task.compose)
  const featured = suitable.find(task => !task.done) ?? suitable[0]
  const show = () => {
    setOpened(context)
    tap('daily_tasks_viewed', { kind: editor ? 'editor' : source })
  }
  const reload = async () => {
    setRetry({ context, busy: true, failed: false })
    const next = await refresh()
    setRetry({ context, busy: false, failed: !next })
  }
  const action = (task: typeof tasks[number]) => {
    tap('daily_task_action', { kind: task.type, to: task.compose ? '02-1_compose' : '03-3_feed' })
    setOpened('')
    if (task.compose) nav.push('02-1_compose')
    else nav.tab(1)
  }
  const status = tasks.length ? `${done}/${tasks.length}` : 'Посмотреть'
  return <>
    {editor ? <section className="daily-editor card" aria-label="Ежедневные задания">
      <div className="row small muted"><span>Ежедневные задания</span><span>{tasks.length ? `${done}/${tasks.length} ✓` : ''}</span></div>
      {featured ? <><div className="daily-task-title"><b>{featured.title}</b><b>{featured.progress}/{featured.target}</b></div><progress aria-label="Прогресс в этом мире" value={featured.progress} max={featured.target} /><p className="small muted">{featured.done ? 'Выполнено в одном из миров' : 'Прогресс в этом мире'}</p></> : <p className="small muted">{tasks.length ? 'Для этого действия нет отдельного задания.' : 'Задания пока недоступны.'}</p>}
      <p className="small muted daily-note">Учёт выполненных действий пока недоступен.</p>
      <button className="daily-more" onClick={show}>{featured ? '▸ Остальные ежедневные' : 'Открыть задания дня'}</button>
    </section> : inline && tasks.length ? <section className="daily-channel card" aria-label="Задания дня">
      <div className="daily-channel-head">
        <h2>Задания дня</h2>
        <button type="button" className="text-button" onClick={show} aria-haspopup="dialog">Подробнее ›</button>
      </div>
      <ul className="daily-channel-list">
        {tasks.map(task => <li className="daily-channel-task" key={task.type}>
          <span className="daily-quest-symbol" aria-hidden="true">{taskSymbols[task.type as TaskType]}</span>
          <span>{task.title}</span>
          <span className={task.done ? 'daily-channel-done' : 'muted'} aria-label={task.done ? 'Выполнено' : `${task.progress} из ${task.target}`}>{task.done ? '✓' : `${task.progress}/${task.target}`}</span>
        </li>)}
      </ul>
    </section> : <button className="daily-entry" onClick={show}><span>Задания дня</span><span data-daily-count>{status} ›</span></button>}
    {opened === context && createPortal(<Sheet onClose={() => setOpened('')}>
      <div className="daily-panel" role="dialog" aria-label="Задания дня" onKeyDown={e => { if (e.key === 'Escape') setOpened('') }}>
        <div className="daily-panel-head">
          <div><h2>Задания дня</h2><p>Обновятся в 03:00 по Москве</p></div>
          <button className="daily-icon-button" aria-label="Обновить задания" title="Обновить задания" disabled={busy} onClick={reload}>{busy ? '…' : '↻'}</button>
          <button className="daily-icon-button" aria-label="Закрыть задания" onClick={() => setOpened('')} autoFocus>×</button>
        </div>
        <div className="daily-panel-summary"><span>{done} из {tasks.length} выполнено</span><span>Сегодня</span></div>
        <div className="daily-quest-list">
          {tasks.map(task => <div className={`daily-quest-row${task.done ? ' is-done' : ''}`} key={task.type} data-kind={task.type}>
            <div className="daily-quest-heading"><b><span className="daily-quest-symbol" aria-hidden="true">{taskSymbols[task.type as TaskType]}</span>{task.title}</b><span className="daily-quest-count">{task.done ? '✓' : `${task.progress}/${task.target}`}</span></div>
            {task.done ? <p className="small muted">Выполнено в одном из миров</p> : <div className="daily-quest-bottom">
              <progress aria-label={`Прогресс: ${task.title}`} value={task.progress} max={task.target} />
              {!(editor && (task.compose || (repost && task.type === 'reposts'))) && <button className="daily-quest-action" onClick={() => action(task)}>{task.compose ? 'Написать пост' : 'Открыть ленту'}</button>}
            </div>}
          </div>)}
        </div>
        {!tasks.length && <p className="daily-note muted">Задания для этого мира пока недоступны. Можно продолжить играть.</p>}
        {failed && <p className="small daily-update-error" role="status">Не получилось обновить. Нажми ↻, чтобы повторить.</p>}
        <p className="daily-panel-note">Прогресс пока не обновляется</p>
      </div>
    </Sheet>, document.body)}
  </>
}
