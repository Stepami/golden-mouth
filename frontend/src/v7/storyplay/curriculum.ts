import type {World} from '../domain/types';
import type {PilotCampaign} from '../story/pilot';
import {starterFor} from './starters';
export type PlayQuest=PilotCampaign['days'][number]['quests'][number];
export type Curriculum=Pick<PilotCampaign,'id'|'days'|'goalWeights'> & {actorOrder?:string[];initialActors?:string[]};
/** A short legacy tutorial uses this instance's content; it never changes API settings. */
export function legacyCurriculum(world:World,templateId?:string):Curriculum {
 const first=starterFor(templateId,world),ideas=world.quests.slice(0,3);
 const titles=[ideas[0]?.title||first.title,ideas[1]?.title||'Уточни взгляд собеседника',ideas[2]?.title||'Подведи итог первого разговора'];
 const quests=titles.map((title,i)=>({id:`legacy:${world.id}.d1q${i+1}`,title,prompt:i===0?first.prompt:ideas[i]?.prompt||title,criterion:'Сохранён шаг по теме в локальном прохождении.',target:i===1&&world.cast[0]?world.cast[0].id:'audience',actionKinds:['post','comment'],baseXP:[10,12,14][i],focusSkill:['primary','secondary','tertiary'][i],drafts:[{id:'ask',title:'Начать разговор',text:i===0?first.draft:`${ideas[i]?.prompt||title}. Хочу услышать твою версию и понять, что можно проверить.`,minSkill:1}]}));
 return {id:`legacy:${world.id}`,days:[{day:1,title:'Первый игровой день',weight:1,quests}],goalWeights:[1],actorOrder:world.cast.map(p=>p.id),initialActors:world.cast.slice(0,1).map(p=>p.id)};
}
