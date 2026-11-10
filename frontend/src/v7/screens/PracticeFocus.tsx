import {Icon} from '../ui';
import {SkillArt} from '../ui/GameArt';
import {practiceFor} from '../skillPractice';

export function PracticeFocus({skills,compact=false}:{skills:string[];compact?:boolean}) {
 if(!skills.length)return null;
 return <details className={`practice-focus${compact?' practice-focus--compact':''}`}><summary><span><strong>Твоя практика</strong>{!compact&&<span className="practice-summary-skills">{skills.map(name=><span key={name}><SkillArt name={name}/>{name}</span>)}</span>}</span><Icon name="chevron"/></summary><div className="practice-guides">{skills.map(name=><div key={name}><strong className="skill-name"><SkillArt name={name}/>{name}</strong><p>{practiceFor(name).action}</p></div>)}</div></details>;
}
