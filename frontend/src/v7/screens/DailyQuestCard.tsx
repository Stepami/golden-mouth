import {Icon} from '../ui';

/** Presentation data only; bind after the daily quest API is agreed. */
export function DailyQuestCard({task}:{task?:{text:string;rewardCrystals:number}}){
 return <section className="daily-quest" aria-label="Задание на сегодня">
  <p className="caption muted">Задание на сегодня</p>
  <div className="daily-quest-card"><p>{task?.text??'Новое задание скоро появится'}</p>{task&&<span className="daily-quest-reward" aria-label={`Награда в кристаллах: ${task.rewardCrystals}`}><Icon name="crystal"/>+{task.rewardCrystals}</span>}</div>
 </section>;
}
