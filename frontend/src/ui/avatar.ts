import { createContext } from 'react'
import type { WorldId } from '../api/types'
import type { AvatarProfile } from './avatarProfile'
export type { AvatarProfile } from './avatarProfile'
export const AvatarWorld = createContext<WorldId>('stage')
export const AvatarPeople = createContext<Record<string, AvatarProfile>>({})
