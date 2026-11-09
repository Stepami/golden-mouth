import type { Player } from '../../api/types';
import type { World } from '../domain/types';
import { Avatar, Button } from '../ui';
import { CurrentSkills } from './CurrentSkills';
import { GoalCard, TaskCard } from '../ui/GameBrief';
import {PracticeFocus} from './PracticeFocus';
import type {PublicationReceipt} from '../publication/PublicationCards';

export function WorldBrief({ world, player, avatar, task, firstVisit, hasDraft, onWrite, onFeed, practiceSkills=[], receipt, onRead }: {
  world: World; player: Player; avatar?: string; task: { title: string; prompt: string };
  firstVisit: boolean; hasDraft: boolean; onWrite: () => void; onFeed: () => void; practiceSkills?:string[]; receipt?:PublicationReceipt; onRead?:()=>void;
}) {
  const outcome=receipt?.questOutcome;
  const status=outcome?.status==='success'?'Задание выполнено':outcome?.status==='partial'?'Есть что дополнить':outcome?.status==='failed'?'Попробуй другой подход':receipt?'Пост опубликован':hasDraft?'Есть черновик':'Можно начинать';
  return <div className="world-brief">
    <header className="brief-cover">
      <img className="brief-cover-image" src={world.hero} alt="" />
      <div className="brief-cover-copy"><p className="eyebrow">{world.title}</p><h1>{firstVisit ? 'Твоя история начинается' : 'Цель и задачи'}</h1></div>
    </header>
    <section className="section brief-identity"><Avatar name={player.name} src={avatar} size="lg" /><div><h2>{player.name}</h2>{world.role !== player.name && <p className="caption muted">{world.role}</p>}</div></section>
    <div className="brief-cards">
     <TaskCard title={task.title} prompt={task.prompt} status={status} outcome={outcome?.status&&outcome.status!=='unverified'?outcome.reason:'Оценка выполнения задания пока не подключена. Посты, отклики и развитие навыков сохраняются.'}>
      {receipt&&<Button className="full" onClick={onRead}>Читать отклики</Button>}
      {(!receipt||hasDraft)&&<Button className="full" variant={receipt?'secondary':'primary'} onClick={onWrite}>{hasDraft ? 'Продолжить черновик' : 'Начать пост'} <span className="caption">· 1 энергия</span></Button>}
     </TaskCard>
     <GoalCard goal={world.goal}/>
     <PracticeFocus skills={practiceSkills}/>
    </div>
    <CurrentSkills skills={player.skills} labels={['Смелость', 'Юмор', 'В тему']} />
    <section className="section brief-footer"><Button variant="ghost" className="full" onClick={onFeed}>Перейти в ленту</Button></section>
  </div>;
}
