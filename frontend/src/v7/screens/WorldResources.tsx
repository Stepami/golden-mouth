import type { Player } from '../../api/types';
import { Button } from '../ui';

export function dayCount(value: number) {
  const n = Math.max(0, Math.floor(value));
  const ending = n % 100 >= 11 && n % 100 <= 14 ? 'дней' : n % 10 === 1 ? 'день' : n % 10 >= 2 && n % 10 <= 4 ? 'дня' : 'дней';
  return `${n} ${ending}`;
}
export function WorldProgress({ streak, storyDay, onStreak, onBoosters }: {
  streak: Player['streak']; storyDay?: number; onStreak: () => void; onBoosters: () => void;
}) {
  return <div className="world-progress">
    <button className="streak-access" onClick={onStreak} aria-label={`Серия: ${dayCount(streak.days)}. Посмотреть дни и награды`}>
      <span aria-hidden="true">🔥</span><span><strong>Серия · {dayCount(streak.days)}</strong><span className="streak-mini" aria-hidden="true">{Array.from({ length: 7 }, (_, i) => <i key={i} className={streak.week[i] ? 'is-done' : ''} />)}</span></span>
    </button>
    {storyDay !== undefined && <span className="story-day-label">День сюжета {storyDay}</span>}
    <button className="boosters-access" onClick={onBoosters}><span aria-hidden="true">✦</span> Бустеры <span className="caption muted">Скоро</span></button>
  </div>;
}
export function StreakDetails({ streak }: { streak: Player['streak'] }) {
  return <div className="stack">
    <p>В серии {dayCount(streak.days)}. Пропуск ставит её на паузу — серия не сгорает.</p>
    <ol className="streak-days" aria-label="Семь отметок серии">{Array.from({ length: 7 }, (_, i) => <li key={i}><span aria-hidden="true">{streak.week[i] ? '🔥' : '○'}</span><span className="caption">{i + 1}</span><span className="sr-only">{streak.week[i] ? 'Есть отметка' : 'Нет отметки'}</span></li>)}</ol>
    <p className="caption muted">Отметки серии, не календарь посещений.</p>
    <h3>Награды серии</h3>
    <ul className="streak-rewards"><li>3 дня — +2 энергии</li><li>7 дней — карточка «Неделя в эфире»</li><li>14 дней — рамка аватарки</li><li>30 дней — звезда-гость</li></ul>
  </div>;
}
export function CrystalExchange({ onBoosters }: { onBoosters: () => void }) {
  return <div className="stack">
    <p>Здесь можно будет обменять кристаллы на энергию и усиление сюжетного XP.</p>
    <p className="caption muted">Баланс и обмен пока недоступны. Кристаллы не списываются.</p>
    <section className="resource-option stack"><h3>Энергия</h3><p>Для публикаций и ответов во всех мирах.</p><Button disabled>Обменять на энергию — скоро</Button></section>
    <section className="resource-option stack"><h3>Усиление сюжетного XP</h3><p>Больше опыта за действие с подтверждённым сюжетным результатом.</p><Button disabled>Обменять на усиление — скоро</Button></section>
    <Button variant="secondary" onClick={onBoosters}>Посмотреть бустеры</Button>
  </div>;
}
export function BoosterDetails() {
  return <div className="stack"><p>Бустеры будут усиливать сюжетный XP за действие. Они не гарантируют согласие персонажа или выполнение задания.</p><section className="resource-option stack"><h3>Бустер сюжетного XP</h3><p className="caption muted">Инвентарь, сила усиления и применение пока недоступны.</p><Button disabled>Применить бустер — скоро</Button></section></div>;
}
