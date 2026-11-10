export type Activity = { id: string; worldId: string; title: string; text: string; read: boolean; kind?:'reaction'|'mention'|'follow'; actorName?:string; relation?:number };

export function activitiesForWorld(items: Activity[], worldId?: string): Activity[] {
  return items.filter(item => item.worldId === worldId);
}
export function markWorldActivitiesRead(items: Activity[], worldId?: string): Activity[] {
  return items.map(item => item.worldId === worldId ? { ...item, read: true } : item);
}
