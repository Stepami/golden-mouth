// Контракт фронт ↔ бэкенд (issue #10). Бэкенд может уточнить поля: меняем здесь и в mock.ts.
export type SkillId = 'brave' | 'humor' | 'topic'
export type WorldId = 'stage' | 'academy' | 'it' | 'stream' | 'own'
export type Address = 'she' | 'he' | 'nick'
export type Drama = 'soft' | 'mid' | 'hot' | 'off'

export interface World {
  id: WorldId; title: string; emoji: string; description: string
  difficulty: 1 | 2 | 3; summary: string      // «рост ×1,1 · хейт с 3k · ценят юмор»
  lock?: { skill: SkillId; level: number }     // нет поля = открыт
}
export interface Star { id: string; name: string; role: string; trait: string; about: string; relation: number }
export interface Skill { level: number; progress: number } // progress 0..1 до следующего уровня
export interface Want { id: string; text: string; reward: string; done: boolean }
export interface CollectionCard { id: string; emoji: string; title: string; open: boolean }

export interface Player {
  id: string; name: string; handle: string; role: string; address: Address
  world: WorldId; subs: number; postsLeft: number; postsCap: number
  streak: { days: number; week: boolean[]; nextReward: string }
  skills: Record<SkillId, Skill>; wants: Want[]; collection: CollectionCard[]
  bio: string; type: string                     // «дерзкая с самоиронией»
  nextWorld?: { id: WorldId; title: string; need: string }
  settings: { drama: Drama; notifications: string; quiet: string; voice: string; plus: boolean }
  /** A/B-флаги, например { plus_offer: '10' } — аналитика прикладывает их к каждому событию */
  experiments?: Record<string, string>
}

export interface Reaction { emoji: string; count: number }
export interface CommentT { id: string; author: string; avatar: string; handle: string; text: string; star?: boolean; me?: boolean; hidden?: boolean }
/** Пост канала. `canReply` есть только при загрузке с сервера (GET /posts/:id): отвечать можно лишь под своими постами. */
export interface Post { id: string; text: string; views: number; reactions: Reaction[]; comments: CommentT[]; inRivers?: boolean; at?: string; canReply?: boolean }
/** Wire-контракт GET /posts/:id: сервер называет идентификатор postId. */
export interface PostDetailResponse extends Omit<Post, 'id'> { postId: string }
export interface ChannelPost { postId: string; text: string; createdAtUtc: string; views: number; reactions: Reaction[]; commentCount: number }
/** Ответ на комментарий (02-6). Идемпотентен по replyId: повтор возвращает тот же сохранённый итог. */
export interface ReplyResult {
  replyId: string; commentId: string; subscriberBonus: number
  awardedReplies: number; replyLimit: number; subscriberTotal: number; reaction?: string
}

export interface Why { icon: string; label: string; value: string }
export interface PostResult {
  postId: string; delta: number; headline: string; whys: Why[]
  ribbons: { icon: string; title: string; sub?: string }[]
  bestComment?: { avatar: string; text: string }
  viral: boolean; flop: boolean; views: number
  hook?: string                                 // нить: на постах 1–9 закрывается через 1–3 хода
}
/** Поток живой реакции (SSE из POST /posts/publications/{id}/moderation) */
export type LiveEvent = { type: 'views'; views: number } | { type: 'react'; emoji: string } | { type: 'comment'; comment: CommentT } | { type: 'typing'; who: string } | { type: 'result'; result: PostResult }
export type PublicationStatus = 'pending' | 'ready_for_moderation' | 'completed' | 'failed'
export interface PublicationState { publicationId: string; status: PublicationStatus; result: PostResult | null }

export interface Friend { id: string; name: string; avatar: string; world: string; subs: number; lastPost: string; pronoun: 'её' | 'его' }
export interface Challenge { id: string; me: string; rival: Friend; day: number; days: number; myScore: number; rivalScore: number; bestPost: string }
export interface GameEvent { id: string; kind: 'collab' | 'hate' | 'gossip' | 'brand'; badge: string; title: string; text: string; ideas: string[]; reward: string; note: string }
export interface DmMessage { from: 'star' | 'me'; text: string }
export interface Dm { star: Star; status: string; memory: string[]; messages: DmMessage[]; ideas: string[]; repliesLeft: number }
/** Карточка ленты 03-3. У постов стабильный `postId`; `canReply` открывает ответ только под своими. Новости без `postId` не открываются. */
export interface FeedItem { kind: 'news' | 'post'; postId?: string; canReply?: boolean; author?: string; avatar?: string; role?: string; ago?: string; text: string; reactions?: Reaction[]; repost?: string }
export interface WorldRules { title: string; presets: string[]; description: string; rules: { text: string; locked?: boolean }[] }
