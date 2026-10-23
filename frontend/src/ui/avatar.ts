import { createContext } from 'react'
import type { WorldId } from '../api/types'
import type { AvatarProfile } from './avatarProfile'
export type { AvatarProfile } from './avatarProfile'
export const AvatarWorld = createContext<WorldId>('edfccf53-65c6-4eaf-8321-2391bc1758bd')
export const AvatarPeople = createContext<Record<string, AvatarProfile>>({})
