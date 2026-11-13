import type * as Wire from './generated/reki-api'
import type * as Ui from './types'

// Generated stream validators use bigint for int64; the UI consistently uses JS numbers.
type Input<T> = T extends number ? number | string | bigint : T extends (infer Item)[] ? Input<Item>[] : T extends object ? { [K in keyof T]: Input<T[K]> } : T
const number = (value: number | string | bigint): number => Number(value)
function skillKey(value: string): Ui.SkillId {
  switch (value) { case 'brave': case 'humor': case 'topic': return value; default: throw new Error('Unknown skill') }
}
export function world(value: Input<Wire.WorldResponse>): Ui.WorldDocument {
  const difficulty = value.difficulty ?? 'normal'
  if (difficulty !== 'easy' && difficulty !== 'normal' && difficulty !== 'hard') throw new Error('Invalid difficulty')
  return { ...value, difficulty, subscribers: number(value.subscribers ?? 0),
    unlockSkill: value.unlockSkill ? skillKey(value.unlockSkill) : undefined, unlockLevel: value.unlockLevel == null ? undefined : number(value.unlockLevel),
    stars: value.stars.map(star => ({ ...star, trait: number(star.trait), channelSubscribers: star.channelSubscribers == null ? star.channelSubscribers : number(star.channelSubscribers) })),
  }
}
export function player(value: Input<Wire.PlayerResponse>): Ui.PlayerDocument {
  const band = value.ageBand
  if (band && band !== '18+' && !['14-17', '18-24', '25-34', '35-44', '45+'].includes(band)) throw new Error('Unknown age band')
  const ageBand = band === '14-17' || band === '18-24' || band === '25-34' || band === '35-44' || band === '45+' ? band : undefined
  const skill = (id: Ui.SkillId): Ui.Skill => {
    const item = value.skills[id]
    if (!item) throw new Error('Missing player skill')
    return { level: number(item.level), progress: number(item.progress) }
  }
  return { ...value, energyWallet: value.energyWallet == null ? value.energyWallet : energyWallet(value.energyWallet), ageBand, energy: number(value.energy), streak: { ...value.streak, days: number(value.streak.days) }, skills: { brave: skill('brave'), humor: skill('humor'), topic: skill('topic') } }
}
export function energyWallet(value: Input<Wire.EnergyWalletResponse>): Wire.EnergyWalletResponse {
  return { ...value, amount: number(value.amount), cap: number(value.cap), regenSeconds: number(value.regenSeconds),
    crystals: number(value.crystals),
    exchangeQuote: { ...value.exchangeQuote, crystalCost: number(value.exchangeQuote.crystalCost), targetEnergy: number(value.exchangeQuote.targetEnergy) },
  }
}
export const comment = (value: Input<Wire.CommentResponse>): Ui.CommentT => ({ ...value, depth: number(value.depth) })
export const post = (value: Input<Wire.PostDetailResponse>): Ui.PostDetailResponse => ({ ...value, views: number(value.views), likes: number(value.likes), likedBy: value.likedBy, comments: value.comments.map(comment) })
export const channelPost = (value: Input<Wire.ChannelPostResponse>): Ui.ChannelPost => ({ ...value, views: number(value.views), likes: number(value.likes), likedBy: value.likedBy, commentCount: number(value.commentCount) })
export const result = (value: Input<Wire.PostResultResponse>): Ui.PostResult => ({ ...value, delta: number(value.delta), views: number(value.views), bestComment: value.bestComment ?? undefined, ribbons: value.ribbons.map(ribbon => ({ ...ribbon, sub: ribbon.sub ?? undefined })) })
export function friend(value: Input<Wire.FriendResponse>): Ui.Friend {
  if (value.pronoun !== 'её' && value.pronoun !== 'его') throw new Error('Unknown friend pronoun')
  return { ...value, pronoun: value.pronoun, subs: number(value.subs) }
}
export const challenge = (value: Input<Wire.ChallengeResponse> | null): Ui.Challenge | null => value ? ({ ...value, rival: friend(value.rival), day: number(value.day), days: number(value.days), myScore: number(value.myScore), rivalScore: number(value.rivalScore) }) : null
export const dm = (value: Input<Wire.DmResponse>): Ui.Dm => ({ ...value, star: { ...value.star, channelSubscribers: value.star.channelSubscribers == null ? value.star.channelSubscribers : number(value.star.channelSubscribers) }, repliesLeft: number(value.repliesLeft) })
export const feed = (value: Input<Wire.FeedItemResponse>): Ui.FeedItem => ({ ...value,
  repostId: value.repostId ?? undefined, authorId: value.authorId ?? undefined, postId: value.postId ?? undefined, author: value.author ?? undefined, avatar: value.avatar ?? undefined, role: value.role ?? undefined, ago: value.ago ?? undefined, repost: value.repost ?? undefined,
  likes: value.likes == null ? undefined : number(value.likes), likedBy: value.likedBy ?? undefined,
  views: value.views == null ? undefined : number(value.views), commentCount: value.commentCount == null ? undefined : number(value.commentCount),
})
