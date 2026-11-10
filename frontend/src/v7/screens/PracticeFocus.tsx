import {Icon} from '../ui';
import {SkillArt} from '../ui/GameArt';
import {practiceFor} from '../skillPractice';

export function PracticeFocus({skills}:{skills:string[]}) {
 if(!skills.length)return null;
 return <details className="practice-focus"><summary><span><strong>Твоя практика</strong><span className="practice-summary-skills">{skills.map(name=><span key={name}><SkillArt name={name}/>{name}</span>)}</span></span><Icon name="chevron"/></summary><div className="practice-guides">{skills.map(name=><div key={name}><strong className="skill-name"><SkillArt name={name}/>{name}</strong><p>{practiceFor(name).action}</p></div>)}</div></details>;
}
