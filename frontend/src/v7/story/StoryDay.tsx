import { StoryBrief } from './StoryBrief';
import { Button, Notice } from '../ui';
import { storyForCampaign, type PilotCampaign } from './pilot';

export function StoryDay({ campaign, onWrite, onFeed }: {
  campaign: PilotCampaign;
  onWrite: (prompt: string) => void;
  onFeed: () => void;
}) {
  const story = storyForCampaign(campaign);
  return <>
    <section className="section stack"><h2>{story.scenario}</h2><p>{story.premise}</p></section>
    <StoryBrief goal={campaign.goal} />
    <section className="section stack story-day">
      <h2>День 1 · {campaign.day.title}</h2>
      <Notice title="Первый день — пробуем историю">Публикации и ответы сохраняются. Зачёт заданий, сюжетный XP и следующий день пока недоступны.</Notice>
      {campaign.day.quests.map((quest, index) => <article className="story-step stack" key={quest.id}>
        <p className="caption muted">Шаг {index + 1} · {quest.target === 'audience' ? 'Публикация' : 'Разговор с персонажем'}</p>
        <h3>{quest.title}</h3>{quest.target !== 'audience' && <p className="caption muted">Собеседник: {campaign.request.stars.find(star => star.id === quest.target)?.name}</p>}<p>{quest.prompt}</p>
        <p className="caption muted">Что важно: {quest.criterion}</p>
        {quest.target !== 'audience' && <p className="caption muted">Открой ответ персонажа в обсуждении и продолжи разговор. Его согласие нельзя объявить за него.</p>}
        <Button variant="secondary" onClick={() => onWrite(quest.prompt)}>Написать по теме</Button>
      </article>)}
      <Button variant="secondary" onClick={onFeed}>Посмотреть публикации и ответы</Button>
      <details className="story-roadmap"><summary>Посмотреть сюжет на 7 дней</summary><div className="stack"><p className="caption muted">Авторский план продолжения. Дни 2–7 пока нельзя проходить: сюжетный зачёт ещё не подключён.</p>{campaign.days.slice(1).map(day => <article className="story-step stack" key={day.day}><h3>День {day.day} · {day.title}</h3>{day.quests.map(quest => <div className="stack" key={quest.id}><strong>{quest.title}</strong><p>{quest.prompt}</p><p className="caption muted">{quest.criterion}</p></div>)}</article>)}</div></details>
      <Button disabled>Продолжение истории — скоро</Button>
    </section>
  </>;
}
