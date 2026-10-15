import type { ChannelPost, CommentT, FeedItem, Player, Post, WorldId, WorldDocument } from './types'
import { contentOf, wantsFor, worlds, worldDocumentOf } from './worldContent'
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
export function demoPlayer(worldId?: string): Player {
  if (worldId && currentPlayer) {
    const selected = read<Player>(`reki:demo-world-player:${currentPlayer.id}:${worldId}`)
    if (selected) { currentPlayer = selected; save(PLAYER_KEY, selected) }
  }
  if (currentPlayer) return structuredClone(currentPlayer)
  const query = new URLSearchParams(location.search)
  if (query.has('screen') || query.has('gallery')) {
    const world = (query.get('world') ?? worlds[0]?.id ?? 'it') as WorldId
    const profile = contentOf(world)
    return { ...structuredClone(examplePlayer), world, name: profile.name, handle: profile.handle, role: profile.roles[0], bio: '', type: 'автор своего канала', wants: wantsFor(world), voice: profile.voices[0], drama: profile.drama }
  }
  throw Object.assign(new Error('Player not found'), { status: 404 })
}
export function saveDemoPlayer(player: Player): Player {
  currentPlayer = structuredClone(player)
  save(PLAYER_KEY, currentPlayer)
  save(`reki:demo-world-player:${player.id}:${demoWorldKey(player)}`, currentPlayer)
  const world = worldDocumentOf(player.world)
  saveDemoWorld(player.id, { ...world, id: demoWorldKey(player), name: player.name, avatar: player.handle, subscribers: player.subs })
  return structuredClone(currentPlayer)
}
export function resetDemoPlayer(playerId = currentPlayer?.id) {
  if (playerId) {
    try {
      for (const key of Object.keys(localStorage)) {
        if (key === `reki:demo-posts:${playerId}` || key.startsWith(`reki:demo-posts:v2:${playerId}:`) || key === `reki:demo-worlds:${playerId}` || key.startsWith(`reki:demo-world-player:${playerId}:`) || key === `reki:first-star:${playerId}` || key.startsWith(`reki:demo-comments:${playerId}:`) || key.startsWith(`reki:reply-draft:${playerId}:`)) localStorage.removeItem(key)
      }
    } catch { /* optional storage */ }
  }
  comments.clear()
  currentPlayer = undefined
  try { localStorage.removeItem(PLAYER_KEY) } catch { /* optional storage */ }
}
export const demoWorlds = (playerId: string): WorldDocument[] => read<WorldDocument[]>(`reki:demo-worlds:${playerId}`) ?? []
export function saveDemoWorld(playerId: string, world: WorldDocument) { save(`reki:demo-worlds:${playerId}`, [world, ...demoWorlds(playerId).filter(item => item.id !== world.id)]) }
export const demoWorldKey = (player?: Pick<Player, 'world' | 'worldInstanceId'>) => player?.worldInstanceId ?? player?.world ?? 'missing'
const postsKey = (playerId: string, worldId: string) => `reki:demo-posts:v2:${playerId}:${worldId}`
export const demoOwnPosts = (playerId: string, worldId: string): Post[] => read<Post[]>(postsKey(playerId, worldId)) ?? []
export const demoChannelPosts = (playerId: string, worldId: string): ChannelPost[] => demoOwnPosts(playerId, worldId).map(post => ({ postId: post.id, text: post.text,
  createdAtUtc: post.at ?? '', views: post.views, reactions: post.reactions, commentCount: withDemoComments(post, playerId).comments.length }))
export function saveDemoPost(playerId: string, post: Post, worldId: string) { save(postsKey(playerId, worldId), [post, ...demoOwnPosts(playerId, worldId).filter(item => item.id !== post.id)]) }
export const demoPost = (id: string): Post | undefined => {
  return posts.has(id) ? structuredClone(posts.get(id)!) : undefined
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
