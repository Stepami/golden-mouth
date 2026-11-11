import type { FeedItem, Post, WorldDocument } from '../api/types';

export type Activity = { id: string; worldId: string; title: string; text: string; read: boolean; postId?:string; count?:number; kind?:'reaction'|'mention'|'follow'|'streak'|'likes'|'npc-post'; actorName?:string; relation?:number };

// Feed names are presentation data. Only the detail's server author ID proves NPC ownership.
export function npcPostActivity(item: FeedItem, post: Post, world: Pick<WorldDocument, 'id' | 'stars'>): Activity | undefined {
  if (item.demo || item.kind !== 'post' || !item.postId || post.id !== item.postId) return;
  const actor = world.stars.find(star => `Star_${star.id}` === post.authorId);
  if (!actor) return;
  return { id: `npc-post:${post.id}`, postId: post.id, worldId: world.id, title: `${actor.name}: новая публикация`, text: post.text, actorName: actor.name, kind: 'npc-post', read: false };
}

export function activitiesForWorld(items: Activity[], worldId?: string): Activity[] {
  return items.filter(item => item.worldId === worldId);
}
export function markWorldActivitiesRead(items: Activity[], worldId?: string): Activity[] {
  return items.map(item => item.worldId === worldId ? { ...item, read: true } : item);
}
