import {Icon} from '../ui';

/** Presentation data only; bind after the daily quest API is agreed. */
export function DailyQuestCard({task}:{task?:{text:string;rewardCrystals:number}}){
 const form=task?new Intl.PluralRules('ru').select(task.rewardCrystals):'many';
 const unit=form==='one'?'кристалл':form==='few'?'кристалла':'кристаллов';
 return <section className="daily-quest" aria-label="Задание на сегодня">
  <p className="caption muted daily-quest-heading">Задание на сегодня</p>
  <div className="daily-quest-card">
   <p>{task?.text??'Новое задание скоро появится'}</p>
   <div className="daily-quest-reward">{task?<><span>Выполни задание — получи</span><span className="daily-quest-amount"><Icon name="crystal"/>{task.rewardCrystals} {unit}</span></>:<span>За выполнение — награда в кристаллах</span>}</div>
  </div>
 </section>;
}
