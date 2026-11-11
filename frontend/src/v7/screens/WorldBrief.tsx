import type { Player } from '../../api/types';
import type { World } from '../domain/types';
import { Avatar, Button } from '../ui';
import { CurrentSkills } from './CurrentSkills';
import { GoalCard } from '../ui/GameBrief';

export function WorldBrief({ world, player, avatar, firstVisit, hasDraft, onWrite, onFeed, roleSkills }: {
  world: World; player: Player; avatar?: string;
  roleSkills?:import('../story/roles').RoleSkill[];
  firstVisit: boolean; hasDraft: boolean; onWrite: () => void; onFeed: () => void;
}) {
  return <div className="world-brief">
    <header className="brief-cover">
      <img className="brief-cover-image" src={world.hero} alt="" />
      <div className="brief-cover-copy"><p className="eyebrow">{world.title}</p><h1>{firstVisit ? 'Твоя история начинается' : 'Твоя цель'}</h1></div>
    </header>
    <section className="section brief-identity"><Avatar name={player.name} src={avatar} size="lg" /><div><h2>{player.name}</h2>{world.role !== player.name && <p className="caption muted">{world.role}</p>}</div></section>
    <div className="brief-cards">
     <GoalCard goal={world.goal}/>
     <Button className="full" onClick={onWrite}>{hasDraft ? 'Продолжить черновик' : 'Начать пост'} <span className="caption">· 1 энергия</span></Button>
    </div>
    <CurrentSkills skills={player.skills} labels={roleSkills?.map(s=>s.name)??['Смелость', 'Юмор', 'В тему']} roleSkills={roleSkills} values={player.roleSkills} />
    <section className="section brief-footer"><Button variant="ghost" className="full" onClick={onFeed}>Перейти в ленту</Button></section>
  </div>;
}
