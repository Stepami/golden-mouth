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
  worldInstanceId?: string; avatarStyle?: string
  id: string; name: string; handle: string; role: string; address: Address
  world: WorldId; subs: number; energy: number
  streak: { days: number; week: boolean[]; nextReward: string }
  skills: Record<SkillId, Skill>; wants: Want[]; collection: CollectionCard[]
  bio: string; type: string                     // «дерзкая с самоиронией»
  nextWorld?: { id: WorldId; title: string; need: string }
  drama: Drama; voice: string
  settings: { notifications: string; quiet: string; plus: boolean }
  /** A/B-флаги, например { plus_offer: '10' } — аналитика прикладывает их к каждому событию */
  experiments?: Record<string, string>
}

export interface Reaction { emoji: string; count: number }
export interface CommentReaction { authorId: string; author: string; avatar: string; star: boolean; me: boolean; emoji: string }
export interface CommentT {
  id: string; author: string; avatar: string; handle: string; text: string; star?: boolean; me?: boolean; hidden?: boolean
  authorId?: string; role?: string; parentCommentId?: string | null; requestId?: string | null; depth?: number; reactions?: CommentReaction[]
}
/** Пост канала. `canReply` есть только при загрузке с сервера (GET /posts/:id): отвечать можно лишь под своими постами. */
export interface Post { id: string; text: string; views: number; reactions: Reaction[]; comments: CommentT[]; inRivers?: boolean; at?: string; canReply?: boolean; demoComments?: boolean }
/** Wire-контракт GET /posts/:id: сервер называет идентификатор postId. */
export interface PostDetailResponse extends Omit<Post, 'id'> { postId: string }
export interface ChannelPost { postId: string; text: string; createdAtUtc: string; views: number; reactions: Reaction[]; commentCount: number }
/** SSE одного ответа: промежуточный текст не опубликован; completed подтверждает транзакцию. */
export interface ReplyLiveEvent {
  status: 'generating' | 'completed' | 'failed'; requestId: string
  text?: string | null; post?: PostDetailResponse | null; energyLeft?: number | null; subscribers?: number | null; code?: string | null
}

export interface Why { icon: string; label: string; value: string }
export interface PostResult {
  postId: string; delta: number; headline: string; whys: Why[]
  ribbons: { icon: string; title: string; sub?: string }[]
  bestComment?: { avatar: string; text: string }
  viral: boolean; flop: boolean; views: number
  hook?: string                                 // нить: на постах 1–9 закрывается через 1–3 хода
}
/** Элемент массива живой реакции (JSON из POST /posts/publications/{id}/moderation) */
export type LiveEvent = { type: 'views'; views: number } | { type: 'react'; emoji: string } | { type: 'comment'; comment: CommentT } | { type: 'typing'; who: string } | { type: 'result'; result: PostResult }
export type PublicationStatus = 'pending' | 'ready_for_moderation' | 'completed' | 'failed'
export interface PublicationState { publicationId: string; status: PublicationStatus; result: PostResult | null }

export interface Friend { id: string; name: string; avatar: string; world: string; subs: number; lastPost: string; pronoun: 'её' | 'его' }
export interface Challenge { id: string; me: string; rival: Friend; day: number; days: number; myScore: number; rivalScore: number; bestPost: string }
export interface GameEvent { id: string; kind: 'collab' | 'hate' | 'gossip' | 'brand'; badge: string; title: string; text: string; ideas: string[]; reward: string; note: string }
export interface DmMessage { from: 'star' | 'me'; text: string }
export interface Dm { star: Star; status: string; memory: string[]; messages: DmMessage[]; ideas: string[]; repliesLeft: number }
/** Карточка серверной ленты 03-3. У постов стабильный `postId`; новости без `postId` не открываются. */
export interface FeedItem { kind: 'news' | 'post'; postId?: string; author?: string; avatar?: string; role?: string; ago?: string; text: string; reactions?: Reaction[]; views?: number; commentCount?: number; repost?: string }
export type WorldUpdate = Partial<Pick<WorldDocument, 'title' | 'description' | 'rules' | 'drama' | 'voice'>>

/** Шаблон или экземпляр мира из MongoDB; ID экземпляра отличается от ключа баланса. */
export interface WorldDocument extends Omit<World, 'id'> {
  id: string; balanceKey: WorldId; avatarStyle: string; where: string
  name: string; avatar: string; role: string; subscribers: number; voice: string; drama: Drama
  roles: string[]; voices: string[]; postIdeas: string[]; demoPosts: string[]
  firstContext: string; firstPrompt: string
  rules: { text: string; locked?: boolean }[]; wants: Want[]; achievements: CollectionCard[]
  stars: (Omit<Star, 'trait'> & { trait: number; voice: string; avatarSeed: string })[]
  unlockSkill?: SkillId; unlockLevel?: number
}

/** Полное описание для создания нового экземпляра; источник может быть шаблоном. */
export type WorldDefinition = Omit<WorldDocument, 'id' | 'subscribers' | 'lock'>

export interface Invitation { bonus: number }

export interface PlayerDocument {
  id: string; address: Address; energy: number
  streak: Player['streak']; skills: Player['skills']; experiments: Record<string, string>
  settings: { notifications: string; quiet: string; plus: boolean }
}
