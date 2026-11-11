import type { Player } from '../../api/types';
import type { World } from '../domain/types';
import { Avatar, Button } from '../ui';
import { CurrentSkills } from './CurrentSkills';
import { GoalCard, TaskCard } from '../ui/GameBrief';
import {PracticeFocus} from './PracticeFocus';
import type {PublicationReceipt} from '../publication/PublicationCards';

export function WorldBrief({ world, player, avatar, task, firstVisit, hasDraft, onWrite, onFeed, practiceSkills=[], receipt, onRead, roleSkills, campaignProgress, campaignComplete }: {
  world: World; player: Player; avatar?: string; task?: { title: string; prompt: string; context?:string };
  roleSkills?:import('../story/roles').RoleSkill[]; campaignProgress?:number; campaignComplete?:boolean;
  firstVisit: boolean; hasDraft: boolean; onWrite: () => void; onFeed: () => void; practiceSkills?:string[]; receipt?:PublicationReceipt; onRead?:()=>void;
}) {
  const outcome=receipt?.questOutcome;
  const status=outcome?.status==='success'?'Задание выполнено':outcome?.status==='partial'?'Есть что дополнить':outcome?.status==='failed'?'Попробуй другой подход':undefined;
  return <div className="world-brief">
    <header className="brief-cover">
      <img className="brief-cover-image" src={world.hero} alt="" />
      <div className="brief-cover-copy"><p className="eyebrow">{world.title}</p><h1>{firstVisit ? 'Твоя история начинается' : 'Цель и задачи'}</h1></div>
    </header>
    <section className="section brief-identity"><Avatar name={player.name} src={avatar} size="lg" /><div><h2>{player.name}</h2>{world.role !== player.name && <p className="caption muted">{world.role}</p>}</div></section>
    <div className="brief-cards">
     {task?<TaskCard title={task.context??task.title} prompt={task.prompt} status={status} outcome={outcome?.status&&outcome.status!=='unverified'?outcome.reason:undefined}>
      {receipt&&<Button className="full" onClick={onRead}>Читать отклики</Button>}
      {(!receipt||outcome?.status!=='success'||hasDraft)&&<Button className="full" variant={receipt?'secondary':'primary'} onClick={onWrite}>{hasDraft ? 'Продолжить черновик' : 'Начать пост'} <span className="caption">· 1 энергия</span></Button>}
     </TaskCard>:campaignComplete?<article className="task-card"><h2>Эпизод завершён</h2><p>Твоя история остаётся в ленте. Можно продолжить разговор.</p><Button onClick={onWrite}>Написать пост</Button></article>:null}
     {roleSkills&&<div className="campaign-progress"><span>Прогресс истории</span><strong>{campaignProgress===undefined?'—':`${campaignProgress}%`}</strong>{campaignProgress!==undefined&&<progress value={campaignProgress} max={100} aria-label="Прохождение истории"/>}</div>}
     <GoalCard goal={world.goal}/>
     <PracticeFocus skills={practiceSkills}/>
    </div>
    <CurrentSkills skills={player.skills} labels={roleSkills?.map(s=>s.name)??['Смелость', 'Юмор', 'В тему']} roleSkills={roleSkills} values={player.roleSkills} />
    <section className="section brief-footer"><Button variant="ghost" className="full" onClick={onFeed}>Перейти в ленту</Button></section>
  </div>;
}
