import {avatarPersonalization} from '../app/avatarPersonalization';
import {generatedAvatarSource} from './generatedAvatar';
import type { FeedItem, WorldDocument, Player, Post as ServerPost } from '../api/types';
import type { World, Person, Post } from './domain/types';
import { worldArt } from '../app/worldArt';
import { worlds as catalogWorlds } from '../api/worldContent';
import { presetGoalFor } from '../app/presetGoals';
import {channelSubscribers} from './social/audience';
import { personaFor } from './persona';
import { roleScenarios } from './story/roles';

export function worldView(doc: WorldDocument): World {
  // Saved worlds have their own IDs and may retain an older version of the lore.
  // Resolve artwork only; this must not change template identity or game rules.
  const matches = catalogWorlds.filter(world => world.title === doc.title);
  const scenario = roleScenarios.find(s => s.title === doc.title || doc.firstContext.startsWith(s.opening));
  const art = worldArt[doc.id] ?? worldArt[scenario?.templateId ?? ''] ?? (matches.length === 1 ? worldArt[matches[0].id] : undefined);
  const cover = art?.cover ?? '/assets/world-placeholder.svg';
  const goal = presetGoalFor(doc);
  return { id: doc.id, title: doc.title, genre: art?.genre ?? 'Свои истории', cover: art?.smallCover ?? cover, hero: cover,
    members: '', tagline: doc.summary, description: doc.description, goal: goal?.goal ?? doc.summary,
    role: doc.role, playerName: doc.name, topic: doc.firstContext, quests: doc.postIdeas.slice(0, 3).map((text, i) => ({ id: String(i), title: text, context: doc.firstContext, prompt: text, draft: '' })),
    seedPost: doc.firstPrompt, cast: doc.stars.map(star => ({ id: star.id, name: star.name, handle: star.avatarSeed, ...(star.avatarSeed.startsWith('generated-')?{avatar:generatedAvatarSource(star.avatarSeed,doc.avatarStyle)}:{}), role: star.role, boundary: star.about, relation: star.relation, subscribers:channelSubscribers(star) })),
  };
}
export const playerView = (p: Player): Person => ({ id: 'player', name: p.name, handle: p.handle, role: p.role, boundary: '', subscribers:p.subs, ...(p.handle.startsWith('generated-')?{avatar:generatedAvatarSource(p.handle,p.avatarStyle??'lorelei',{address:p.address,ageBand:p.ageBand,personalizeAvatar:avatarPersonalization(p.id)})}:personaFor(p.id,p.world).avatar?{avatar:personaFor(p.id,p.world).avatar}:{}) });
export function feedView(item: FeedItem, player: Person): { post: Post; author: Person; count: number } {
  const mine = item.avatar === player.handle && item.author === player.name;
  return { post: { id: item.postId ?? '', text: item.text, authorId: mine ? 'player' : item.starId ?? item.author ?? 'npc',
    createdAt: formatDate(item.ago), likes: item.reactions?.reduce((n, r) => n + r.count, 0) ?? 0 },
    author: mine ? player : { id: item.author ?? 'npc', name: item.author ?? 'Собеседник', handle: item.avatar ?? '', role: item.role ?? 'AI-персонаж', boundary: '' }, count: item.commentCount ?? 0 };
}
export function detailView(p: ServerPost, world: World, player: Person): { post: Post; author: Person } {
  const cast = world.cast.find(person => `Star_${person.id}` === p.authorId);
  const mine = !p.authorId?.startsWith('Star_');
  return { post: { id: p.id, text: p.text, authorId: mine ? 'player' : p.authorId ?? 'npc', createdAt: '', likes: p.reactions.reduce((n, r) => n + r.count, 0) },
    author: mine ? player : cast ?? { id: p.authorId ?? 'npc', name: 'Собеседник', handle: '', role: 'AI-персонаж', boundary: '' } };
}
export function errorText(cause: unknown): string {
  const status = (cause as { status?: number })?.status;
  if (status === 401) return 'Открой Реки в Telegram, чтобы сохранить историю.';
  if (status === 403) return 'Это действие сейчас недоступно. Обнови данные и попробуй ещё раз.';
  if (status === 409) return 'Состояние истории изменилось. Обнови данные перед повтором.';
  if (status === 429) return 'Слишком много запросов. Попробуй чуть позже.';
  return 'Не получилось загрузить данные. Проверь соединение и попробуй ещё раз.';
}
export function readLocal<T>(key: string, fallback: T): T {
  try { return JSON.parse(localStorage.getItem(key) ?? 'null') ?? fallback; } catch { return fallback; }
}
export function saveLocal(key: string, value: unknown): boolean {
  try { localStorage.setItem(key, JSON.stringify(value)); return true; } catch { return false; }
}

export function formatDate(value?: string): string {
  if (!value) return '';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat('ru', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' }).format(date);
}
