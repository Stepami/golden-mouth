import type { Player } from '../../api/types';
import {useEffect,useRef,useState} from 'react';
import {RewardArt} from '../ui/RewardArt';
import { Icon } from '../ui';

export function dayCount(value: number) {
  const n = Math.max(0, Math.floor(value));
  const ending = n % 100 >= 11 && n % 100 <= 14 ? 'дней' : n % 10 === 1 ? 'день' : n % 10 >= 2 && n % 10 <= 4 ? 'дня' : 'дней';
  return `${n} ${ending}`;
}
export function WorldProgress({streak, onStreak}: {
  streak: Player['streak']; onStreak: () => void;
}) {
  const previousDays=useRef(streak.days);
  const [earned,setEarned]=useState(false);
  useEffect(()=>{const grew=streak.days>previousDays.current;previousDays.current=streak.days;if(!grew)return;setEarned(true);const timer=setTimeout(()=>setEarned(false),600);return()=>clearTimeout(timer);},[streak.days]);
  return <div className="world-progress">
    <button className={`streak-access${earned?' is-earned':''}`} onClick={onStreak} aria-label={`Серия: ${dayCount(streak.days)}. Посмотреть дни`}>
      <RewardArt kind="flame"/><span className="streak-copy"><span className="streak-heading"><strong>{dayCount(streak.days)}</strong><small>в серии</small></span><span className="streak-mini" aria-hidden="true">{Array.from({ length: 7 }, (_, i) => <i key={i} className={streak.week[i] ? 'is-done' : ''}>{streak.week[i]?<Icon name="check"/>:i+1}</i>)}</span></span>
    </button>
  </div>;
}
export function StreakDetails({ streak }: { streak: Player['streak'] }) {
  return <div className="stack streak-detail">
    <div className="streak-detail-hero"><RewardArt kind="flame"/><strong>{dayCount(streak.days)}</strong></div>
    <ol className="streak-days" aria-label="Семь отметок серии">{Array.from({ length: 7 }, (_, i) => <li key={i}><span className={`streak-day-mark${streak.week[i] ? ' is-done' : ''}`} aria-hidden="true">{streak.week[i] && <Icon name="check"/>}</span><span className="caption">{i + 1}</span><span className="sr-only">{streak.week[i] ? 'Есть отметка' : 'Нет отметки'}</span></li>)}</ol>

  </div>;
}
