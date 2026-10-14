import type { Address } from '../api/types'
import type { AgeBand } from '../app/ageBand'
export interface AvatarProfile { address: Address; ageBand?: AgeBand; personalizeAvatar?: boolean }
export interface Appearance {
  hair: 'normal' | 'bald' | 'receding'; length: 'short' | 'medium' | 'long'; facial: 'none' | 'stubble' | 'mustache' | 'beard'; jewelry: boolean; hat: boolean; glasses: boolean
}
/** Независимые детерминированные выборки: смена обращения не меняет палитру и очки. */
export function sample(seed: string, feature: string) {
  let hash = 2166136261
  for (const char of `${seed}:appearance-v1:${feature}`) { hash ^= char.charCodeAt(0); hash = Math.imul(hash, 16777619) }
  return (hash >>> 0) / 4294967296
}
export function appearanceOf(seed: string, profile: AvatarProfile): Appearance {
  const masculine = profile.address === 'he'; const feminine = profile.address === 'she'
  const adult = masculine && !!profile.ageBand && profile.ageBand !== '14-17'
  const hair = sample(seed, 'hair'); const facial = sample(seed, 'facial'); const length = sample(seed, 'length')
  const short = masculine ? .65 : feminine ? .10 : .35; const medium = masculine ? .25 : feminine ? .35 : .40
  return { hair: adult && hair >= .85 ? hair < .90 ? 'bald' : 'receding' : 'normal',
    length: length < short ? 'short' : length < short + medium ? 'medium' : 'long',
    facial: !adult || facial < .70 ? 'none' : facial < .80 ? 'stubble' : facial < .88 ? 'mustache' : 'beard',
    jewelry: sample(seed, 'jewelry') < (masculine ? 0 : feminine ? .40 : .15), hat: sample(seed, 'hat') < .08, glasses: sample(seed, 'glasses') < .15 }
}
export const variants = {
  lorelei: { short: ['variant01', 'variant02', 'variant03', 'variant05', 'variant07', 'variant09', 'variant12', 'variant22', 'variant39'], medium: ['variant10', 'variant13', 'variant15', 'variant17', 'variant19', 'variant20', 'variant23', 'variant37', 'variant40', 'variant41', 'variant42'], long: ['variant16', 'variant31', 'variant35', 'variant45', 'variant46', 'variant48'], receding: ['variant25', 'variant28'], bald: ['variant34'] },
  notionists: { short: ['variant01', 'variant05', 'variant07', 'variant15', 'variant22', 'variant25', 'variant31', 'variant44', 'variant49', 'variant53'], medium: ['variant03', 'variant11', 'variant20', 'variant33', 'variant36', 'variant43'], long: ['variant02', 'variant08', 'variant10', 'variant23', 'variant28', 'variant37', 'variant41', 'variant46', 'variant58', 'variant62', 'variant63'] },
  pixelArt: { short: ['short01', 'short02', 'short03', 'short06', 'short09', 'short10', 'short14', 'short15', 'short16', 'short17', 'short18', 'short19', 'short22'], medium: ['long04', 'long05', 'long06', 'long07', 'long11', 'long14', 'long16', 'long17'], long: ['long01', 'long02', 'long03', 'long08', 'long09', 'long13', 'long18', 'long19', 'long20', 'long21'], receding: ['short12', 'short23'], bald: ['short07'] },
} as const
