import {Icon} from '../ui';

/** Presentation data only; bind after the daily quest API is agreed. */
export function DailyQuestCard({task}:{task?:{text:string;rewardCrystals:number}}){
 return <section className="daily-quest" aria-label="Задание на сегодня">
  <div className="daily-quest-heading"><p className="caption muted">Задание на сегодня</p><span className="daily-quest-reward" aria-label={task?`Награда в кристаллах: ${task.rewardCrystals}`:'Награда в кристаллах пока не определена'}><Icon name="crystal"/>{task?`+${task.rewardCrystals}`:'—'}</span></div>
  <div className="daily-quest-card"><p>{task?.text??'Новое задание скоро появится'}</p></div>
 </section>;
}
