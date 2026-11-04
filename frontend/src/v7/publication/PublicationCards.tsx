import type { Player, PostResult } from '../../api/types';
import { Button } from '../ui';
import { QuestReward } from '../storyplay/StorySurfaces';
export type PublicationReceipt = { worldId: string; result: PostResult; before: Player['skills']; after?: Player['skills']; questReward?: {amount:number;title:string;questId:string} };
export function PublicationWaiting({ pending, unknown, text, status, onContinue, onStop }: {
  pending: boolean; unknown: boolean; text: string; status: string; onContinue: () => void; onStop: () => void;
}) {
  return <section className="section"><article className="publication-card stack" aria-label="Состояние отправки">
    <h2>{pending ? status === 'ready_for_moderation' ? 'Проверяем публикацию' : 'Готовим отклики' : unknown ? 'Проверь отправку' : 'Черновик сохранён'}</h2>
    <p role="status">{pending ? 'Ждём подтверждения сервера. Пока неизвестно, опубликована ли запись.' : unknown ? 'Результат пока неизвестен. Проверка использует тот же запрос и не создаёт повторный пост.' : 'Публикация не подтверждена. Можно изменить текст и попробовать снова.'}</p>
    <blockquote>{text}</blockquote>
    {pending ? <Button variant="secondary" onClick={onStop}>Остановить ожидание</Button> : <Button onClick={onContinue}>{unknown ? 'Проверить публикацию' : 'Продолжить публикацию'}</Button>}
  </article></section>;
}
export function PublicationResult({ receipt, onRead, animateReward=false }: { receipt: PublicationReceipt; onRead: () => void; animateReward?:boolean }) {
  const { result, before, after } = receipt;
  return <section className="section"><article className="publication-card publication-card--result stack" aria-label="Результат публикации">
    <p className="caption muted">Запись опубликована · Получены реакции</p><h2>{result.headline}</h2>
    {receipt.questReward && <QuestReward amount={receipt.questReward.amount} title={receipt.questReward.title} animate={animateReward}/>}
    <strong>{result.delta > 0 ? '+' : ''}{result.delta} подписчиков</strong>
    {after && <ul className="publication-changes">{(['brave', 'humor', 'topic'] as const).map((id, i) => <li key={id}>{['Смелость', 'Юмор', 'В тему'][i]}: {before[id].level !== after[id].level ? `ур. ${before[id].level} → ${after[id].level}` : `прогресс ${Math.round(before[id].progress * 100)}% → ${Math.round(after[id].progress * 100)}%`}</li>)}</ul>}
    <Button onClick={onRead}>В комментарии →</Button>
    <details><summary>Все изменения</summary><div className="stack">
      {result.whys.map((why, i) => <p key={i}><strong>{why.label}</strong> · {why.value}</p>)}
      {result.ribbons.map((ribbon, i) => <p key={i}>{ribbon.title}{ribbon.sub ? ` · ${ribbon.sub}` : ''}</p>)}
      <p className="caption muted">{result.views} просмотров</p>
      {!after && <p className="caption muted">Навыки обновятся после получения данных персонажа.</p>}
    </div></details>
  </article></section>;
}
