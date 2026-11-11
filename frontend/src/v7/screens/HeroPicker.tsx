import {customPortrait} from '../avatarBuilder';
import {characterPortrait,hogwartsPortraits,roleLooks} from '../portraits';
export type HeroOption = { id: string; name: string; role: string; goal: string; motivation?:string; skills?: import('../story/roles').RoleSkill[] };
export function heroPortraitSource(id: string, seed: string, worldId: string, _style: string) {
  if(id==='greedy_wolf')return '/assets/avatars/3d/merkantil-v9.png';
  if(id==='2kotleti_news'||id==='alexey_nl')return `/assets/avatars/3d/${id==='2kotleti_news'?'burger':'kulyash'}-v9.png`;
  return id==='custom' ? customPortrait(seed,worldId) : hogwartsPortraits[id] || characterPortrait(roleLooks[id]||id);
}

export async function heroPortrait(id:string,seed:string,worldId:string,style:string) { return heroPortraitSource(id,seed,worldId,style); }
