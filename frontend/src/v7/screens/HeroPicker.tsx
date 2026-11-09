import {characterPortrait,roleLooks} from '../portraits';
export type HeroOption = { id: string; name: string; role: string; goal: string; motivation?:string; skills?: import('../story/roles').RoleSkill[] };
export function heroPortraitSource(id: string, seed: string, _worldId: string, _style: string) {
  const photos: Record<string, string> = { harry: 'harry', hermione: 'hermione', ron: 'ron', draco: 'draco' };
  return id==='custom' ? characterPortrait(seed) : photos[id] ? `/assets/avatars/${photos[id]}.jpg` : characterPortrait(roleLooks[id]||id);
}

export async function heroPortrait(id:string,seed:string,worldId:string,style:string) { return heroPortraitSource(id,seed,worldId,style); }
