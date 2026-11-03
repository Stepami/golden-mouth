import { Button, Notice } from '../ui';
import { storyPilot, type PilotCampaign } from './pilot';

export function StoryDay({ campaign, onWrite, onFeed }: {
  campaign: PilotCampaign;
  onWrite: (prompt: string) => void;
  onFeed: () => void;
}) {
  return <>
    <section className="goal-hero"><p className="caption muted">Сюжетная проба · Вечерело</p><h2>Улица на чужом снимке</h2><p>{storyPilot.premise}</p><h3>Твоя цель</h3><p>{campaign.goal}</p></section>
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
      <Button disabled>Продолжение истории — скоро</Button>
    </section>
  </>;
}
