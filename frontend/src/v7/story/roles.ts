import scenarios from './roleScenarios.json';
import type {WorldDefinition, WorldDocument} from '../../api/types';
import type {Persona} from '../persona';

export type RoleScenario = typeof scenarios[number];
export type StoryRole = RoleScenario['roles'][number];
export type RoleSkill = StoryRole['skills'][number];
export const roleScenarios = scenarios;
export const scenarioForTemplate = (id?:string|null) => scenarios.find(s=>s.templateId===id);
export const roleFor = (templateId:string|undefined,roleId:string|undefined) => scenarioForTemplate(templateId)?.roles.find(r=>r.id===roleId);
export function roleForWorld(doc:WorldDocument,persona:Persona):StoryRole|undefined {
 if(persona.roleId&&persona.templateId)return roleFor(persona.templateId,persona.roleId);
 // The scenario opening identifies new instances without rewriting older story runs.
 const scenario=scenarios.find(s=>doc.firstContext.startsWith(s.opening));
 return scenario?.roles.find(r=>r.name===doc.name)??scenario?.roles.find(r=>r.id==='custom');
}
export function definitionForRole(base:WorldDefinition,roleId:string):WorldDefinition {
 const scenario=scenarioForTemplate(base.templateId)!;
 const role=scenario.roles.find(r=>r.id===roleId)!;
 return {...base,title:scenario.title,description:scenario.opening,
  name:roleId==='custom'?base.name:role.name,role:roleId==='custom'?'Свой персонаж':role.name,
  avatar:roleId==='custom'?base.avatar:roleId,
  firstPrompt:role.tasks[0].prompt,postIdeas:[],demoPosts:[],wants:[],achievements:[],roles:scenario.roles.map(r=>r.name),
  rules:[...base.rules.filter(rule=>rule.locked),{text:'Игрок действует постами и комментариями. NPC сохраняют свои цели и право отказать; слова игрока не назначают чужое согласие и не совершают физические действия.',locked:false}],
  firstContext:`${scenario.opening}\n\nТекущее задание: ${role.tasks[0].prompt}\nЦель персонажа: ${role.goal}`,
  stars:scenario.roles.filter(r=>r.id!=='custom'&&r.id!==roleId).map(r=>{
   const existing=base.stars.find(s=>s.id===r.id);
   return {id:r.id,name:r.name,role:r.name,voice:'Самостоятельно отвечай с учётом своей цели и известных событий.',avatarSeed:existing?.avatarSeed??r.id,trait:existing?.trait??1,relation:0,about:r.goal};
  }),
 };
}

// Editorial groupings for discovery only; they neither change skills nor award progress.
export const skillDirections = [
 {name:'Смелость',hint:'Высказываться и держать позицию',skills:['Лидерство','Самообладание','Напор','Решительность','Выдержка','Настойчивость','Стойкость','Держать удар','Прямота']},
 {name:'Внимание',hint:'Замечать и понимать',skills:['Проницательность','Наблюдательность','Внимание к деталям','Эрудиция','Редакторское чутьё','Чутьё на инфоповод','Нюх на буллшит','Глаз рекрутера','Второй вопрос']},
 {name:'Убеждение',hint:'Находить доводы и решения',skills:['Убеждение','Расчёт','Критическое мышление','Аргументация','Стратегия','Доказательная база','Каверзный вопрос','Расчёт смены','Чутьё на деньги']},
 {name:'Общение',hint:'Слышать других и договариваться',skills:['Харизма','Солидарность','Переговоры','Деликатность','Дипломатия','Торг','Прогрев','Дожим','Пробить фильтр']},
 {name:'Юмор',hint:'Шутить и менять угол зрения',skills:['Сухой панч','Самоирония','Подача','Находчивость','Смекалка']},
 {name:'Самовыражение',hint:'Находить свой подход',skills:['Самовыражение','Независимость','Изобретательность','Хитрость','Влияние']},
];
export function recommendRoles<T extends {skills?:RoleSkill[]}>(roles:T[],skills:string[],directions:string[]):T[]{
 const score=(role:T)=>(role.skills??[]).reduce((sum,skill)=>sum+(skills.includes(skill.name)?10:0)+skillDirections.filter(d=>directions.includes(d.name)&&d.skills.includes(skill.name)).length,0);
 return roles.map((role,index)=>({role,index,score:score(role)})).sort((a,b)=>b.score-a.score||a.index-b.index).map(v=>v.role);
}
