// Wire models are generated from OpenAPI. These types only adapt them for the UI/demo.
import type * as Wire from './generated/reki-api'

// OpenAPI allows unknown fields; UI mapped types operate on declared fields only.
type Fields<T> = { [K in keyof T as string extends K ? never : number extends K ? never : K]: T[K] }

export type SkillId = 'brave' | 'humor' | 'topic'
export type WorldId = string
export type WorldDifficulty = 'easy' | 'normal' | 'hard'
export type Address = Wire.PlayerResponse['address']
export type Drama = Wire.WorldDrama
export interface World {
  id: WorldId; title: string; emoji: string; description: string
  difficulty: WorldDifficulty; summary: string; lock?: { skill: SkillId; level: number }
}
export type Star = Omit<Fields<Wire.StarResponse>, 'relation'>
export type Skill = { [K in keyof Wire.SkillResponse]: number }
export type CollectionCard = Wire.CollectionCardResponse
export type PlayerDocument = Omit<Fields<Wire.PlayerResponse>, 'energy' | 'skills' | 'streak' | 'ageBand'> & {
  energy: number; skills: Record<SkillId, Skill>
  streak: Omit<Fields<Wire.StreakResponse>, 'days'> & { days: number }
  ageBand?: import('../app/ageBand').AgeBand
}
export interface Player extends Omit<PlayerDocument, 'energy' | 'experiments'> {
  worldInstanceId?: string; difficulty?: WorldDifficulty; avatarStyle?: string
  name: string; handle: string; role: string; world: WorldId
  subs: number; postsLeft: number; postsCap: number; firstPostInviteBonus: number
  collection: CollectionCard[]; bio: string; type: string
  nextWorld?: { id: WorldId; title: string; need: string }; drama: Drama; voice: string
  experiments?: Wire.PlayerResponse['experiments']
}
export type Reaction = { emoji: string; count: number }
export type CommentReaction = Wire.CommentReactionResponse
// Demo comments can omit server-enriched author/thread metadata.
type CommentMetadata = 'star' | 'me' | 'hidden' | 'parentCommentId' | 'requestId' | 'authorId' | 'role' | 'reactions'
export type CommentT = Omit<Fields<Wire.CommentResponse>, CommentMetadata | 'depth'> & Partial<Pick<Fields<Wire.CommentResponse>, CommentMetadata>> & { depth?: number }
export type PostDetailResponse = Omit<Fields<Wire.PostDetailResponse>, 'views' | 'likes' | 'likedBy' | 'reactions' | 'comments' | 'authorId' | 'repostId'> & Partial<Pick<Fields<Wire.PostDetailResponse>, 'repostId'>> & {
  authorId?: string; views: number; likes: number; likedBy: string[]; reactions: Reaction[]; comments: CommentT[]
}
export type Post = Omit<PostDetailResponse, 'postId'> & { id: string; inRivers?: boolean; at?: string }
export type ChannelPost = Omit<Fields<Wire.ChannelPostResponse>, 'views' | 'likes' | 'likedBy' | 'commentCount' | 'reactions' | 'repostId'> & Partial<Pick<Fields<Wire.ChannelPostResponse>, 'repostId'>> & { views: number; likes: number; likedBy: string[]; commentCount: number; reactions: Reaction[] }
export type Why = Wire.WhyResponse
export type PostResult = Omit<Fields<Wire.PostResultResponse>, 'delta' | 'views' | 'ribbons' | 'bestComment'> & {
  delta: number; views: number
  ribbons: (Omit<Fields<Wire.RibbonResponse>, 'sub'> & { sub?: string })[]
  bestComment?: Wire.BestCommentResponse
}
export type Friend = Omit<Fields<Wire.FriendResponse>, 'subs' | 'pronoun'> & { subs: number; pronoun: 'её' | 'его' }
export type Challenge = Omit<Fields<NonNullable<Wire.ChallengeResponse>>, 'rival' | 'day' | 'days' | 'myScore' | 'rivalScore'> & { rival: Friend; day: number; days: number; myScore: number; rivalScore: number }
export type DmMessage = Wire.DirectMessageResponse
export type Dm = Omit<Fields<Wire.DmResponse>, 'star' | 'repliesLeft'> & { star: Star; repliesLeft: number }
export type PostsPage<T> = Pick<Wire.PageResponseOfFeedItemResponse, 'nextCursor'> & { items: T[] }
export type FeedItem = Omit<Fields<Wire.FeedItemResponse>, 'views' | 'likes' | 'likedBy' | 'commentCount' | 'repostId'> & {
  likes: number; likedBy: string[]; reactions: Reaction[]; views: number; commentCount: number; repostId?: string; demo?: boolean; starId?: string; world?: WorldId
}
export type WorldDocument = Omit<Fields<Wire.WorldResponse>, 'difficulty' | 'subscribers' | 'stars' | 'unlockSkill' | 'unlockLevel'> & {
  difficulty: WorldDifficulty; subscribers: number; lock?: World['lock']
  stars: (Omit<Fields<Wire.WorldStarResponse>, 'trait'> & { trait: number })[]
  unlockSkill?: SkillId; unlockLevel?: number
}
export type WorldDefinition = Omit<WorldDocument, 'id' | 'subscribers' | 'lock'> & { templateId: WorldId | null }
export type WorldUpdate = Partial<Pick<WorldDocument, 'title' | 'description' | 'rules' | 'drama' | 'voice'>>
