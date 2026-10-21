// Wire models are generated from OpenAPI. These types only adapt them for the UI/demo.
import type * as Wire from './generated/types.gen'

export type SkillId = 'brave' | 'humor' | 'topic'
export type WorldId = 'stage' | 'academy' | 'it' | 'stream' | 'own' | 'evening' | 'sorcerers' | 'houses'
export type Address = Wire.Address
export type Drama = Wire.Drama
export interface World {
  id: WorldId; title: string; emoji: string; description: string
  difficulty: 1 | 2 | 3; summary: string; lock?: { skill: SkillId; level: number }
}
export type Star = Omit<Wire.StarResponse, 'relation'> & { relation: number }
export type Skill = { [K in keyof Wire.SkillResponse]: number }
export type Want = Wire.WantResponse
export type CollectionCard = Wire.CollectionCardResponse
export type PlayerDocument = Omit<Wire.PlayerResponse, 'energy' | 'skills' | 'streak' | 'ageBand'> & {
  energy: number; skills: Record<SkillId, Skill>
  streak: Omit<Wire.StreakResponse, 'days'> & { days: number }
  ageBand?: import('../app/ageBand').AgeBand
}
export interface Player extends Omit<PlayerDocument, 'energy' | 'experiments'> {
  worldInstanceId?: string; avatarStyle?: string
  name: string; handle: string; role: string; world: WorldId
  subs: number; postsLeft: number; postsCap: number; firstPostInviteBonus: number
  wants: Want[]; collection: CollectionCard[]; bio: string; type: string
  nextWorld?: { id: WorldId; title: string; need: string }; drama: Drama; voice: string
  experiments?: Wire.PlayerResponse['experiments']
}
export type Reaction = Omit<Wire.ReactionResponse, 'count'> & { count: number }
export type CommentReaction = Wire.CommentReactionResponse
// Demo comments can omit server-enriched author/thread metadata.
type CommentMetadata = 'star' | 'me' | 'hidden' | 'parentCommentId' | 'requestId' | 'authorId' | 'role' | 'reactions'
export type CommentT = Omit<Wire.CommentResponse, CommentMetadata | 'depth'> & Partial<Pick<Wire.CommentResponse, CommentMetadata>> & { depth?: number }
export type PostDetailResponse = Omit<Wire.PostDetailResponse, 'views' | 'reactions' | 'comments' | 'authorId'> & {
  authorId?: string; views: number; reactions: Reaction[]; comments: CommentT[]
}
export type Post = Omit<PostDetailResponse, 'postId'> & { id: string; inRivers?: boolean; at?: string }
export type ChannelPost = Omit<Wire.ChannelPostResponse, 'views' | 'commentCount' | 'reactions'> & { views: number; commentCount: number; reactions: Reaction[] }
export type ReplyLiveEvent = Pick<Wire.ReplyLiveEvent, 'status' | 'requestId'> & Partial<Pick<Wire.ReplyLiveEvent, 'text' | 'code'>> & {
  post?: PostDetailResponse | null; energyLeft?: number | null; subscribers?: number | null
}
export type Why = Wire.WhyResponse
export type PostResult = Omit<Wire.PostResultResponse, 'delta' | 'views' | 'ribbons' | 'bestComment' | 'hook'> & {
  delta: number; views: number
  ribbons: (Omit<Wire.RibbonResponse, 'sub'> & { sub?: string })[]
  bestComment?: Wire.BestCommentResponse; hook?: string
  completedWants?: Pick<Want, 'id' | 'text' | 'reward'>[]
}
type UiLiveEvent<E> = E extends { type: 'views' } ? Omit<E, 'views'> & { views: number }
  : E extends { type: 'comment' } ? Omit<E, 'comment'> & { comment: CommentT }
  : E extends { type: 'result' } ? Omit<E, 'result'> & { result: PostResult } : E
export type LiveEvent = UiLiveEvent<Wire.LiveEvent>
export type PublicationStatus = Wire.PublicationStatus
export type PublicationFailure = Wire.PublicationFailure
export type PublicationState = Omit<Wire.PublicationStatusResponse, 'result' | 'failure'> & { result: PostResult | null; failure?: PublicationFailure }
export type Friend = Omit<Wire.FriendResponse, 'subs' | 'pronoun'> & { subs: number; pronoun: 'её' | 'его' }
export type Challenge = Omit<NonNullable<Wire.ChallengeResponse>, 'rival' | 'day' | 'days' | 'myScore' | 'rivalScore'> & { rival: Friend; day: number; days: number; myScore: number; rivalScore: number }
export type GameEvent = Wire.GameEventResponse
export type DmMessage = Wire.DirectMessageResponse
export type Dm = Omit<Wire.DmResponse, 'star' | 'repliesLeft'> & { star: Star; repliesLeft: number }
// Feed rows omit absent wire values; demo rows also carry local catalog metadata.
type FeedMetadata = Exclude<keyof Wire.FeedItemResponse, 'kind' | 'text' | 'views' | 'commentCount' | 'reactions'>
export type FeedItem = Pick<Wire.FeedItemResponse, 'kind' | 'text'> & { [K in FeedMetadata]?: NonNullable<Wire.FeedItemResponse[K]> } & {
  reactions?: Reaction[]; views?: number; commentCount?: number; demo?: boolean; starId?: string; world?: WorldId
}
export type WorldDocument = Omit<Wire.WorldResponse, 'balanceKey' | 'difficulty' | 'subscribers' | 'stars' | 'unlockSkill' | 'unlockLevel'> & {
  balanceKey: WorldId; difficulty: 1 | 2 | 3; subscribers: number; lock?: World['lock']
  stars: (Omit<Wire.WorldStarResponse, 'trait' | 'relation'> & { trait: number; relation: number })[]
  unlockSkill?: SkillId; unlockLevel?: number
}
export type WorldDefinition = Omit<WorldDocument, 'id' | 'subscribers' | 'lock'>
export type WorldUpdate = Partial<Pick<WorldDocument, 'title' | 'description' | 'rules' | 'drama' | 'voice'>>
