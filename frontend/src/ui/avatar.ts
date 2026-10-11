import { createAvatar } from '@dicebear/core'
import { lorelei, notionists, pixelArt, thumbs, shapes } from '@dicebear/collection'
import { createContext } from 'react'
import type { WorldId } from '../api/types'
import { contentOf } from '../api/worldContent'

/** Мир определяет стиль; сид определяет персонажа. */
export const AvatarWorld = createContext<WorldId>('stage')

const cache = new Map<string, string>()

/**
 * Аватар рисуется из сида в самом приложении, без обращения к внешнему API.
 * Один и тот же сид всегда даёт одну и ту же картинку, поэтому результат кэшируем:
 * в ленте на экране десятки аватарок, и пересобирать каждую на каждый рендер незачем.
 */
export function avatarUri(seed: string, world: WorldId = 'stage'): string {
  const key = `${world}:${seed}`
  const got = cache.get(key)
  if (got) return got
  const options = { seed, size: 96, radius: 50 }
  const style = contentOf(world).avatarStyle
  const avatar = style === 'pixelArt' ? createAvatar(pixelArt, options)
    : style === 'notionists' ? createAvatar(notionists, options)
    : style === 'thumbs' ? createAvatar(thumbs, options)
    : style === 'shapes' ? createAvatar(shapes, options) : createAvatar(lorelei, options)
  const uri = avatar.toDataUri()
  cache.set(key, uri)
  return uri
}
