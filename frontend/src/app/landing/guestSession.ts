import { createContext, useContext } from 'react'
import type { Player, Post, PostResult, WorldDefinition } from '../../api/types'

export type GuestPlayer = Pick<Player, 'id' | 'world' | 'worldInstanceId' | 'name' | 'handle' | 'subs' | 'streak' | 'postsLeft' | 'ageBand' | 'energyWallet'>

interface GuestSession {
  world: WorldDefinition
  player: GuestPlayer
  goal?: { goal: string; firstStep: string; context?: string }
  chooseHero: (id: string) => void
  post: Post
  result: PostResult
  published: boolean
  speaker: string
  postIdea: string
  text?: string
  editPost: (text: string) => void
  reply: string
  replySubmitted: boolean
  replyDone: boolean
  editReply: (text: string) => void
  sendReply: () => void
  crystals: number
}

export const GuestSessionContext = createContext<GuestSession | null>(null)
export const useGuestSession = () => useContext(GuestSessionContext)
