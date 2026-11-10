export type Activity = { id: string; worldId: string; title: string; text: string; read: boolean; postId?:string; count?:number; kind?:'reaction'|'mention'|'follow'|'streak'|'likes'; actorName?:string; relation?:number };

export function activitiesForWorld(items: Activity[], worldId?: string): Activity[] {
  return items.filter(item => item.worldId === worldId);
}
export function markWorldActivitiesRead(items: Activity[], worldId?: string): Activity[] {
  return items.map(item => item.worldId === worldId ? { ...item, read: true } : item);
}
