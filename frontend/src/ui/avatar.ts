import { createAvatar } from '@dicebear/core'
import { lorelei } from '@dicebear/collection'

/**
 * Стиль аватаров. Пока один на все миры: стиль по мирам ждём из справочника (#68),
 * поэтому точка замены ровно одна, эта константа.
 */
export const AVATAR_STYLE = 'lorelei'

const cache = new Map<string, string>()

/**
 * Аватар рисуется из сида в самом приложении, без обращения к внешнему API.
 * Один и тот же сид всегда даёт одну и ту же картинку, поэтому результат кэшируем:
 * в ленте на экране десятки аватарок, и пересобирать каждую на каждый рендер незачем.
 */
export function avatarUri(seed: string): string {
  const got = cache.get(seed)
  if (got) return got
  const uri = createAvatar(lorelei, { seed, size: 96, radius: 50 }).toDataUri()
  cache.set(seed, uri)
  return uri
}
