import type {Player} from '../../api/types';
import {Progress} from '../ui';
import {SkillArt} from '../ui/GameArt';
export function CurrentSkills({skills,labels}:{skills:Player['skills'];labels:string[]}){
 return <section className="section"><h2 className="section-title">Навыки</h2><div className="current-skills">{(['brave','humor','topic'] as const).map((id,i)=><article key={id}><div className="row between"><strong className="skill-name"><SkillArt name={labels[i]}/>{labels[i]}</strong><span>Уровень {skills[id].level}</span></div><Progress label={`${labels[i]}: до следующего уровня`} value={skills[id].progress} max={1}/></article>)}</div></section>;
}
