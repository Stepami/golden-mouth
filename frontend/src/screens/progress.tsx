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
  const [tab, setTab] = useState<'story' | 'results'>('story')
  const history = resultHistory(p)
  return <div className="screen progress-screen"><GameHeader compact /><div className="page-tabs"><button className={tab === 'story' ? 'active' : ''} onClick={() => setTab('story')}>Твоя история</button><button className={tab === 'results' ? 'active' : ''} onClick={() => setTab('results')}>Результаты</button></div>
    <div className="pad">{tab === 'story' ? <>
      <h1>Твой путь</h1>
      {goal && <section className="card journey-goal"><span className="eyebrow small">Главная цель</span><h2>{goal.goal}</h2><details><summary>С чего начать</summary><p>{goal.firstStep}</p></details></section>}
      <section className="current-tasks"><div className="section-heading"><h2>Идеи для продолжения</h2></div>
        {p.wants.map(want => <article className="story-idea" key={want.id}><p>{want.text}</p><button className="text-button" onClick={() => nav.push('02-1_compose')}>Написать →</button></article>)}
        {!p.wants.length && <p className="muted">Продолжи разговор с героями или напиши свою запись.</p>}
      </section>
      <section className="skill-section"><div className="section-heading"><h2>Навыки</h2></div>{SKILLS.map(({ id, icon, name }) => <div className="card skill-detail" key={id}><div className="row"><span className="skill-symbol">{icon}</span><b>{name}</b><span className="small muted" style={{ marginLeft: 'auto' }}>Ур. {p.skills[id].level}</span></div><div className="bar"><i style={{ width: `${p.skills[id].progress * 100}%` }} /></div><p className="small muted">{Math.round(p.skills[id].progress * 100)}% до следующего уровня</p></div>)}</section>
      <section className="card streak-card"><div className="row"><span className="streak-symbol">🔥</span><div><h2>Серия: {p.streak.days}</h2><p className="small muted">{p.streak.nextReward}</p></div></div><div className="streak-week" aria-label="Активность за неделю">{['пн', 'вт', 'ср', 'чт', 'пт', 'сб', 'вс'].map((day, index) => <span key={day} className={p.streak.week[index] ? 'done' : ''}><b>{p.streak.week[index] ? '✓' : '·'}</b><small>{day}</small></span>)}</div></section>
    </> : <>
      <h1>Результаты записей</h1><p className="small muted history-note">Здесь сохраняются подтверждённые результаты на этом устройстве. Результаты прежних сессий и других устройств пока не возвращаются сервером.</p>
      {!history.length && <section className="empty-state"><Icon name="chart" size={40} /><h2>Первый результат ещё впереди</h2><p className="muted">Опубликуй запись — здесь появится её влияние на аудиторию и навыки.</p><Button onClick={() => nav.push('02-1_compose')}>Написать запись</Button></section>}
      {history.map(item => <article className="history-entry" key={item.post.id}><div className="row"><Avatar seed={p.handle} size={36} /><div><b>{p.name}</b><div className="small muted">{new Date(item.at).toLocaleString('ru-RU')}</div></div></div><p>{item.post.text}</p><PublicationResult result={item.result} skills={item.skills} /><button className="text-button" onClick={() => nav.push('02-6_thread', { postId: item.post.id, returnTo: 'history' })}>Открыть обсуждение →</button></article>)}
    </>}</div></div>
}

register({ '05-13_progress': { component: Progress, title: 'Твоя история' } }, { '05-13_progress': 0 })
