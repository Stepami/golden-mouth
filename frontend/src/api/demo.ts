import type { ChannelPost, CommentT, FeedItem, Player, Post, WorldId } from './types'
import { contentOf, wantsFor } from './worldContent'
import { player as examplePlayer } from './mock'

const PLAYER_KEY = 'reki:demo-player:v2'
const comments = new Map<string, CommentT[]>()
const posts = new Map<string, Post>()
function read<T>(key: string): T | undefined {
  try { return JSON.parse(localStorage.getItem(key) ?? 'null') ?? undefined } catch { return undefined }
}
function save(key: string, value: unknown) {
  try { localStorage.setItem(key, JSON.stringify(value)) } catch { /* Демо работает и без хранилища браузера. */ }
}
let currentPlayer = read<Player>(PLAYER_KEY)
export function demoPlayer(): Player {
  if (currentPlayer) return structuredClone(currentPlayer)
  const query = new URLSearchParams(location.search)
  if (query.has('screen') || query.has('gallery')) {
    const world = (query.get('world') ?? 'stage') as WorldId
    const profile = contentOf(world)
    return { ...structuredClone(examplePlayer), world, name: profile.name, handle: profile.handle, role: profile.roles[0], bio: '', type: 'автор своего канала', wants: wantsFor(world), voice: profile.voices[0], drama: profile.drama }
  }
  throw Object.assign(new Error('Player not found'), { status: 404 })
}
export function saveDemoPlayer(player: Player): Player {
  currentPlayer = structuredClone(player)
  save(PLAYER_KEY, currentPlayer)
  return structuredClone(currentPlayer)
}
export function resetDemoPlayer(playerId = currentPlayer?.id) {
  if (playerId) {
    try {
      for (const key of Object.keys(localStorage)) {
        if (key === `reki:demo-posts:${playerId}` || key === `reki:first-star:${playerId}` || key.startsWith(`reki:demo-comments:${playerId}:`) || key.startsWith(`reki:reply-draft:${playerId}:`)) localStorage.removeItem(key)
      }
    } catch { /* optional storage */ }
  }
  comments.clear()
  currentPlayer = undefined
  try { localStorage.removeItem(PLAYER_KEY) } catch { /* optional storage */ }
}
export const demoOwnPosts = (playerId: string): Post[] => read<Post[]>(`reki:demo-posts:${playerId}`) ?? []
export const demoChannelPosts = (playerId: string): ChannelPost[] => demoOwnPosts(playerId).map(post => ({ postId: post.id, text: post.text,
  createdAtUtc: post.at ?? '', views: post.views, reactions: post.reactions, commentCount: withDemoComments(post, playerId).comments.length }))
export function saveDemoPost(playerId: string, post: Post) { save(`reki:demo-posts:${playerId}`, [post, ...demoOwnPosts(playerId).filter(item => item.id !== post.id)]) }
export const demoPost = (id: string): Post | undefined => {
  const world = /^demo-(stage|academy|it|stream|own)-[0-4]$/.exec(id)?.[1] as WorldId | undefined
  if (world && !posts.has(id)) demoFeed(world)
  return posts.has(id) ? structuredClone(posts.get(id)!) : undefined
}

/** Fresh random portion on a feed visit, with stable IDs for opening and commenting. */
export function demoFeed(world: WorldId): FeedItem[] {
  const profile = contentOf(world)
  const choices = profile.posts.map((text, index) => {
    const author = profile.stars[index % profile.stars.length]
    const id = `demo-${world}-${index}`
    const post: Post = {
      id, text, views: 340 + index * 170,
      reactions: [{ emoji: '🔥', count: 12 + index * 5 }, { emoji: '😂', count: 7 + index * 3 }],
      comments: [{ id: `${id}-comment`, author: profile.stars[(index + 1) % profile.stars.length].name,
        avatar: profile.stars[(index + 1) % profile.stars.length].id, handle: profile.stars[(index + 1) % profile.stars.length].role, text: ['Вот это история. Расскажешь продолжение?', 'Знакомая ситуация! Спасибо за честность.', 'Утащу эту мысль в следующий пост.'][index % 3], star: true }],
      canReply: false, demoComments: true,
    }
    posts.set(id, post)
    return { kind: 'post' as const, postId: id, author: author.name, avatar: author.id, role: author.role, starId: author.id,
      text, ago: 'сегодня', reactions: post.reactions, views: post.views, commentCount: post.comments.length,
      demo: true, world }
  })
  return shuffle(choices).slice(0, 3)
}
export function shuffle<T>(items: T[]): T[] {
  const copy = [...items]
  for (let i = copy.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1)); [copy[i], copy[j]] = [copy[j], copy[i]]
  }
  return copy
}
const commentKey = (postId: string, playerId: string) => `reki:demo-comments:${playerId}:${postId}`
export function withDemoComments(post: Post, playerId: string): Post {
  const key = commentKey(post.id, playerId)
  const saved = comments.get(key) ?? read<CommentT[]>(key) ?? []
  comments.set(key, saved)
  return { ...post, comments: [...post.comments, ...saved] }
}
