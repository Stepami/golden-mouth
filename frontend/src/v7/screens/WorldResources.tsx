import type { Player } from '../../api/types';
import {useEffect,useRef,useState} from 'react';
import {RewardArt} from '../ui/RewardArt';
import { Button, Icon } from '../ui';

export function dayCount(value: number) {
  const n = Math.max(0, Math.floor(value));
  const ending = n % 100 >= 11 && n % 100 <= 14 ? 'дней' : n % 10 === 1 ? 'день' : n % 10 >= 2 && n % 10 <= 4 ? 'дня' : 'дней';
  return `${n} ${ending}`;
}
export function WorldProgress({ streak, storyDay, boosterCount = '—', onStreak, onBoosters, onGift, giftClaimed=false }: {
  streak: Player['streak']; storyDay?: number; boosterCount?: string; onStreak: () => void; onBoosters?: () => void; onGift?:()=>void; giftClaimed?:boolean;
}) {
  const previousDays=useRef(streak.days);
  const [earned,setEarned]=useState(false);
  useEffect(()=>{const grew=streak.days>previousDays.current;previousDays.current=streak.days;if(!grew)return;setEarned(true);const timer=setTimeout(()=>setEarned(false),600);return()=>clearTimeout(timer);},[streak.days]);
  return <div className="world-progress">
    <button className={`streak-access${earned?' is-earned':''}`} onClick={onStreak} aria-label={`Серия: ${dayCount(streak.days)}. Посмотреть дни`}>
      <RewardArt kind="flame"/><span className="streak-copy"><span className="streak-heading"><strong>{dayCount(streak.days)}</strong><small>в серии</small></span><span className="streak-mini" aria-hidden="true">{Array.from({ length: 7 }, (_, i) => <i key={i} className={streak.week[i] ? 'is-done' : ''}>{streak.week[i]?<Icon name="check"/>:i+1}</i>)}</span></span>
    </button>
    {onGift&&<button className={`gift-access${giftClaimed?' is-claimed':''}`} onClick={onGift} disabled={giftClaimed} aria-label={giftClaimed?'Подарок сегодня получен':'Забрать подарок серии — локальная проба'}><RewardArt kind="gift"/><small>{giftClaimed?'Получен':'Забрать'}</small></button>}
    {storyDay !== undefined && <span className="story-day-label">День сюжета {storyDay}</span>}
    {onBoosters&&<button className="boosters-access" onClick={onBoosters}><RewardArt kind="booster"/> Бустеры <span className="caption muted">{boosterCount}</span></button>}
  </div>;
}
export function StreakDetails({ streak,preview=true }: { streak: Player['streak']; preview?:boolean }) {
  return <div className="stack streak-detail">
    <div className="streak-detail-hero"><RewardArt kind="flame"/><strong>{dayCount(streak.days)}</strong></div>
    <ol className="streak-days" aria-label="Семь отметок серии">{Array.from({ length: 7 }, (_, i) => <li key={i}><span className={`streak-day-mark${streak.week[i] ? ' is-done' : ''}`} aria-hidden="true">{streak.week[i] && <Icon name="check"/>}</span><span className="caption">{i + 1}</span><span className="sr-only">{streak.week[i] ? 'Есть отметка' : 'Нет отметки'}</span></li>)}</ol>
    {preview&&<><h3>Подарки сюжетной пробы</h3><p>В этой пробе каждый день серии даёт случайный подарок: энергию, кристаллы или бустер.</p></>}
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
  return <div className="stack"><p>Бустеры будут усиливать сюжетный XP за действие. Они не гарантируют согласие персонажа или выполнение задания.</p><section className="resource-option stack"><h3>Бустер сюжетного XP</h3><p className="caption muted">Инвентарь, сила усиления и применение пока недоступны.</p><Button disabled>Нет подключённого инвентаря</Button></section></div>;
}
