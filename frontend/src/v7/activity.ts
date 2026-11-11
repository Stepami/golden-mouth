export type Activity = { id: string; worldId: string; title: string; text: string; read: boolean; postId?:string; count?:number; postText?:string; kind?:'reaction'|'mention'|'follow'|'streak'|'likes'|'npc-post'; actorName?:string; relation?:number };

export function activitiesForWorld(items: Activity[], worldId?: string): Activity[] {
  return items.filter(item => item.worldId === worldId && item.kind !== 'npc-post');
}
export function markWorldActivitiesRead(items: Activity[], worldId?: string): Activity[] {
  return items.map(item => item.worldId === worldId ? { ...item, read: true } : item);
}
