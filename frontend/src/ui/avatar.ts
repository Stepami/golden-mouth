import { Avatar as DiceBearAvatar, Style } from '@dicebear/core'
import adventurerDefinition from '@dicebear/styles/adventurer.json'
import loreleiDefinition from '@dicebear/styles/lorelei.json'
import notionistsDefinition from '@dicebear/styles/notionists.json'
import pixelArtDefinition from '@dicebear/styles/pixel-art.json'
import thumbsDefinition from '@dicebear/styles/thumbs.json'
import shapesDefinition from '@dicebear/styles/shapes.json'
import { createContext } from 'react'
import type { WorldId } from '../api/types'
import { worldOf } from '../api/worldContent'
import { appearanceOf, sample, variants, type AvatarProfile } from './avatarProfile'
export type { AvatarProfile } from './avatarProfile'
export const AvatarWorld = createContext<WorldId>('stage')
export const AvatarPeople = createContext<Record<string, AvatarProfile>>({})
const adventurer = new Style(adventurerDefinition)
const lorelei = new Style(loreleiDefinition)
const notionists = new Style(notionistsDefinition)
const pixelArt = new Style(pixelArtDefinition)
const thumbs = new Style(thumbsDefinition)
const shapes = new Style(shapesDefinition)
const cache = new Map<string, string>()
export function avatarUri(seed: string, world: WorldId = 'stage', profile: AvatarProfile = { address: 'nick' }, styleOverride?: string): string {
  let style = styleOverride ?? 'lorelei'
  if (!styleOverride) { try { style = worldOf(world).avatarStyle } catch { /* empty custom world uses portrait */ } }
  const key = `${world}:${style}:${seed}:${profile.address}:${profile.ageBand ?? 'unknown'}:v1`
  const got = cache.get(key); if (got) return got
  const a = appearanceOf(seed, profile)
  const pick = <T extends string,>(values: readonly T[], feature: string) => [values[Math.floor(sample(seed, feature) * values.length)]]
  const base = { seed, size: 96, borderRadius: 50 }
  const avatar = style === 'pixelArt' ? new DiceBearAvatar(pixelArt, { ...base,
    hairVariant: pick(variants.pixelArt[a.hair === 'normal' ? a.length : a.hair], 'hairVariant'),
    beardProbability: a.facial === 'none' ? 0 : 100, beardVariant: pick(a.facial === 'mustache' ? ['variant01', 'variant02'] : a.facial === 'stubble' ? ['variant06', 'variant08'] : ['variant04', 'variant05', 'variant07'], 'beardVariant'),
    accessoriesProbability: a.jewelry ? 100 : 0, glassesProbability: a.glasses ? 100 : 0, mouthVariant: ['happy01', 'happy02'], mouthColor: ['#d29985', '#c98276'], hairColor: ['#603a14', '#83623b', '#28150a', '#cab188'] })
    : style === 'notionists' ? new DiceBearAvatar(notionists, { ...base,
      hairVariant: a.hat ? ['hat'] : pick(variants.notionists[a.length], 'hairVariant'),
      beardProbability: a.facial === 'none' ? 0 : 100, beardVariant: pick(a.facial === 'stubble' ? ['variant05', 'variant07'] : a.facial === 'mustache' ? ['variant04', 'variant06'] : ['variant08', 'variant09', 'variant10'], 'beardVariant'), glassesProbability: a.glasses ? 100 : 0 })
    : style === 'adventurer' ? new DiceBearAvatar(adventurer, base)
    : style === 'thumbs' ? new DiceBearAvatar(thumbs, base)
    : style === 'shapes' ? new DiceBearAvatar(shapes, base)
    : new DiceBearAvatar(lorelei, { ...base,
      hairVariant: pick(variants.lorelei[a.hair === 'normal' ? a.length : a.hair], 'hairVariant'),
      // Lorelei поддерживает только бороду. Нормализуем доступные категории 70:12.
      beardProbability: profile.address === 'he' && profile.ageBand && profile.ageBand !== '14-17' && sample(seed, 'facial') >= 70 / 82 ? 100 : 0,
      earringsProbability: a.jewelry ? 100 : 0, hairAccessoriesProbability: 0, glassesProbability: a.glasses ? 100 : 0, mouthVariant: ['happy01', 'happy02', 'happy04'], hairColor: ['#000000', '#603a14', '#83623b'], mouthColor: ['#000000'] })
  const uri = avatar.toDataUri(); cache.set(key, uri); return uri
}
