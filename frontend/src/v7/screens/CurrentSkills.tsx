import type {Player,RoleSkillState} from '../../api/types';
import type {RoleSkill} from '../story/roles';
import {Progress} from '../ui';
import {SkillArt} from '../ui/GameArt';
export function CurrentSkills({skills,labels,roleSkills,values}:{skills:Player['skills'];labels:string[];roleSkills?:RoleSkill[];values?:RoleSkillState[]}){
 const items=roleSkills?roleSkills.map(skill=>({name:skill.name,value:values?.find(v=>v.name===skill.name)})):(['brave','humor','topic'] as const).map((id,i)=>({name:labels[i],value:skills[id]}));
 return <section className="section"><h2 className="section-title">Навыки</h2><div className="current-skills">{items.map(({name,value})=><article key={name}><div className="row between"><strong className="skill-name"><SkillArt name={name}/>{name}</strong><span>{value?`Уровень ${value.level}`:'—'}</span></div>{value&&<Progress label={`${name}: до следующего уровня`} value={value.progress} max={1}/>}</article>)}</div></section>;
}
