import { createContext, useContext, type ReactNode } from 'react'
import type { Player, Post, PostResult, WorldDefinition } from '../../api/types'

export type GuestPlayer = Pick<Player, 'id' | 'world' | 'worldInstanceId' | 'name' | 'handle' | 'subs' | 'streak' | 'postsLeft' | 'ageBand' | 'energyWallet'>
export const guestSignupLabel = 'Зарегистрироваться и ответить'

interface GuestSession {
  world: WorldDefinition
  player: GuestPlayer
  goal?: { goal: string; firstStep: string; context?: string }
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
  signUp: () => void
  crystals: number
  hint?: ReactNode
}

export const GuestSessionContext = createContext<GuestSession | null>(null)
export const useGuestSession = () => useContext(GuestSessionContext)
