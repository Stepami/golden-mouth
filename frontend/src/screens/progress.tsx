import { useState } from 'react'
import { usePlayer } from '../app/store'
import { useNav } from '../app/nav'
import { resultHistory } from '../app/resultHistory'
import { GameHeader, SKILLS } from '../ui/GameHeader'
import { usePresetGoal } from '../ui/StoryGoal'
import { PublicationResult } from '../ui/PublicationResult'
import { Avatar, Button } from '../ui'
import { Icon } from '../ui/Icon'
import { register } from '.'

function Progress() {
  const p = usePlayer(); const nav = useNav(); const goal = usePresetGoal(p.world)
  const [tab, setTab] = useState<'story' | 'results'>(nav.current === '05-13b_results' ? 'results' : 'story')
  const history = resultHistory(p)
  return <div className="screen progress-screen"><GameHeader compact />{tab === 'results' && <div className="page-tabs"><button onClick={() => setTab('story')}>Твоя история</button><button className="active">История действий</button></div>}
    <div className="progress-content">{tab === 'story' ? <>
      <section className="progress-heading"><h1>Твоя история</h1></section>
      {goal && <section className="arc-overview"><span className="eyebrow">Главная цель · {p.name}</span><h2>{goal.goal}</h2><details className="arc-growth-details"><summary>Зачем и как развивать героя</summary><p>{goal.firstStep}</p><p>Публикуй записи, продолжай разговоры и развивай навыки.</p></details></section>}
      <section className="arc-active"><span className="eyebrow">Продолжить историю</span><h2>Твой следующий ход</h2><p>{goal?.firstStep ?? p.wants[0]?.text ?? 'Поделись событием из своего мира и продолжи разговор.'}</p><Button onClick={() => nav.push('02-1_compose')}>Продолжить историю →</Button><details className="arc-step-details"><summary>О сюжетном прогрессе</summary><p>Этапы и их выполнение пока не возвращаются сервером. Здесь показана подсказка, а не подтверждённый этап истории.</p></details></section>
      <details className="arc-road"><summary>Твой путь</summary><p>Записи и разговоры сохраняются в мире. Подтверждённые результаты на этом устройстве доступны в истории действий.</p><button className="text-button" onClick={() => nav.push('05-13b_results')}>История действий →</button></details>
      <section className="skills-explained"><div className="skills-title"><h2>Навыки</h2></div><div className="skill-rows">{SKILLS.map(({ id, icon, name }) => <div className="skill-row" key={id}><span className="skill-row-icon">{icon}</span><span className="skill-row-copy"><span><strong>{name}</strong><b>Ур. {p.skills[id].level}</b></span><progress max={1} value={p.skills[id].progress} aria-label={`${name}: ${Math.round(p.skills[id].progress * 100)}% уровня`} /><em>{Math.round(p.skills[id].progress * 100)}% · дальше ур. {p.skills[id].level + 1}</em></span></div>)}</div><small className="shared-resource-note">Навыки — общие для всех миров.</small></section>
      <details className="account-activity"><summary>Энергия и посещения</summary><p>Пост или ответ: −1 энергии. Чтение бесплатно. Запас обновляется в 09:00 МСК.</p><p>Серия посещений: {p.streak.days}. {p.streak.nextReward}</p><div className="streak-week" aria-label="Активность за неделю">{['пн','вт','ср','чт','пт','сб','вс'].map((day,index) => <span key={day} className={p.streak.week[index] ? 'done' : ''}><b>{p.streak.week[index] ? '✓' : '·'}</b><small>{day}</small></span>)}</div></details>
    </> : <>
      <h1>Результаты записей</h1><p className="small muted history-note">Здесь сохраняются подтверждённые результаты на этом устройстве. Результаты прежних сессий и других устройств пока не возвращаются сервером.</p>
      {!history.length && <section className="empty-state"><Icon name="chart" size={40} /><h2>Первый результат ещё впереди</h2><p className="muted">Опубликуй запись — здесь появится её влияние на аудиторию и навыки.</p><Button onClick={() => nav.push('02-1_compose')}>Написать запись</Button></section>}
      {history.map(item => <article className="history-entry" key={item.post.id}><div className="row"><Avatar seed={p.handle} size={36} /><div><b>{p.name}</b><div className="small muted">{new Date(item.at).toLocaleString('ru-RU')}</div></div></div><p>{item.post.text}</p><PublicationResult result={item.result} skills={item.skills} /><button className="text-button" onClick={() => nav.push('02-6_thread', { postId: item.post.id, returnTo: 'history' })}>Открыть обсуждение →</button></article>)}
    </>}</div></div>
}

register({ '05-13_progress': { component: Progress, title: 'Твоя история' }, '05-13b_results': { component: Progress, title: 'История действий' } }, { '05-13_progress': 0, '05-13b_results': 4 })
