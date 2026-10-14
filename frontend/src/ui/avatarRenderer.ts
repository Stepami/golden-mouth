import { Avatar as DiceBearAvatar, Style } from '@dicebear/core'
import type { WorldId } from '../api/types'
import { worldOf } from '../api/worldContent'
import { appearanceOf, sample, variants, type AvatarProfile } from './avatarProfile'
export type { AvatarProfile } from './avatarProfile'
const definitions = {
  adventurer: () => import('@dicebear/styles/adventurer.json'),
  lorelei: () => import('@dicebear/styles/lorelei.json'),
  notionists: () => import('@dicebear/styles/notionists.json'),
  pixelArt: () => import('@dicebear/styles/pixel-art.json'),
  thumbs: () => import('@dicebear/styles/thumbs.json'),
  shapes: () => import('@dicebear/styles/shapes.json'),
}
const styles = new Map<string, Promise<Style>>()
function loadStyle(style: keyof typeof definitions): Promise<Style> {
  let pending = styles.get(style)
  if (!pending) {
    pending = definitions[style]().then(module => new Style(module.default)).catch(error => {
      styles.delete(style)
      throw error
    })
    styles.set(style, pending)
  }
  return pending
}
const cache = new Map<string, string>()
export async function avatarUri(seed: string, world: WorldId = 'stage', profile?: AvatarProfile, styleOverride?: string): Promise<string> {
  let style = styleOverride ?? 'lorelei'
  if (!styleOverride) { try { style = worldOf(world).avatarStyle } catch { /* empty custom world uses portrait */ } }
  const chosenStyle = (Object.hasOwn(definitions, style) ? style : 'lorelei') as keyof typeof definitions
  const active = await loadStyle(chosenStyle)
  const adventurerHair = chosenStyle === 'adventurer'
    ? Object.keys((await definitions.adventurer()).default.components.hair.variants) : []
  const personalized = profile && profile.personalizeAvatar !== false
  const key = `${world}:${style}:${seed}:${personalized ? `${profile.address}:${profile.ageBand ?? 'unknown'}` : 'default'}:v2`
  const got = cache.get(key); if (got) return got
  const base = { seed, size: 96, borderRadius: 50 }
  if (!personalized) {
    const definition = active
    const uri = new DiceBearAvatar(definition, base).toDataUri()
    cache.set(key, uri); return uri
  }
  const a = appearanceOf(seed, profile)
  const pick = <T extends string,>(values: readonly T[], feature: string) => [values[Math.floor(sample(seed, feature) * values.length)]]
  const avatar = style === 'pixelArt' ? new DiceBearAvatar(active, { ...base,
    hairVariant: pick(variants.pixelArt[a.hair === 'normal' ? a.length : a.hair], 'hairVariant'),
    beardProbability: a.facial === 'none' ? 0 : 100, beardVariant: pick(a.facial === 'mustache' ? ['variant01', 'variant02'] : a.facial === 'stubble' ? ['variant06', 'variant08'] : ['variant04', 'variant05', 'variant07'], 'beardVariant'),
    accessoriesProbability: a.jewelry ? 100 : 0, glassesProbability: a.glasses ? 100 : 0, mouthVariant: ['happy01', 'happy02'], mouthColor: ['#d29985', '#c98276'], hairColor: ['#603a14', '#83623b', '#28150a', '#cab188'] })
    : style === 'notionists' ? new DiceBearAvatar(active, { ...base,
      hairVariant: a.hat ? ['hat'] : pick(variants.notionists[a.length], 'hairVariant'),
      beardProbability: a.facial === 'none' ? 0 : 100, beardVariant: pick(a.facial === 'stubble' ? ['variant05', 'variant07'] : a.facial === 'mustache' ? ['variant04', 'variant06'] : ['variant08', 'variant09', 'variant10'], 'beardVariant'), glassesProbability: a.glasses ? 100 : 0 })
    : style === 'adventurer' ? new DiceBearAvatar(active, { ...base,
      hairVariant: pick(adventurerHair.filter(value => value.startsWith(a.length === 'short' ? 'short' : 'long')), 'hairVariant'),
      hairProbability: a.hair === 'bald' ? 0 : 100, earringsProbability: a.jewelry ? 100 : 0, glassesProbability: a.glasses ? 100 : 0 })
    : style === 'thumbs' ? new DiceBearAvatar(active, base)
    : style === 'shapes' ? new DiceBearAvatar(active, base)
    : new DiceBearAvatar(active, { ...base,
      hairVariant: pick(variants.lorelei[a.hair === 'normal' ? a.length : a.hair], 'hairVariant'),
      // Lorelei поддерживает только бороду. Нормализуем доступные категории 70:12.
      beardProbability: profile.address === 'he' && profile.ageBand && profile.ageBand !== '14-17' && sample(seed, 'facial') >= 70 / 82 ? 100 : 0,
      earringsProbability: a.jewelry ? 100 : 0, hairAccessoriesProbability: 0, glassesProbability: a.glasses ? 100 : 0, mouthVariant: ['happy01', 'happy02', 'happy04'], hairColor: ['#000000', '#603a14', '#83623b'], mouthColor: ['#000000'] })
  const uri = avatar.toDataUri(); cache.set(key, uri); return uri
}
