import { useEffect, useLayoutEffect, useRef, useState, type ReactNode } from 'react'
import { api } from '../api/client'
import type { Player, World } from '../api/types'
import { contentOf, optionalWorldTemplate, worldDefinitionOf, worldDocumentOf, worlds } from '../api/worldContent'
import { WorldSwitchDialog } from '../ui/WorldSwitchDialog'
import { avatarPersonalization } from './avatarPersonalization'
import { worldArt } from './worldArt'
import { dismissNextStory, isGameOnboardingPreview, isNextStoryPreview, nextStoryOfferShown, trackNextStory, useGameOnboarding, useNextStory } from './GameOnboardingState'
import { useNav } from './nav'
import { getWorldDraft, savedWorldDraft, selectDraftOwner, setWorldDraft, withPersona, type WorldDraft } from './worldDraft'

function NextStoryDock({ label, children }: { label: string; children: ReactNode }) {
  const element = useRef<HTMLElement>(null)
  useLayoutEffect(() => {
    const footer = element.current?.closest<HTMLElement>('.bottom-cta')
    const screen = footer?.closest<HTMLElement>('.screen')
    if (!footer || !screen) return
    const measure = () => screen.style.setProperty('--next-story-bottom-space', `${Math.max(0, innerHeight - footer.getBoundingClientRect().top) + 12}px`)
    measure()
    const observer = new ResizeObserver(measure)
    observer.observe(footer)
    window.addEventListener('resize', measure)
    return () => {
      observer.disconnect()
      window.removeEventListener('resize', measure)
      screen.style.removeProperty('--next-story-bottom-space')
    }
  }, [])
  return <aside ref={element} className="game-onboarding-progress is-inline next-story-dock" aria-label={label}>{children}</aside>
}

export function NextStoryProgress({ step, children }: { step: 1 | 2 | 3; children: ReactNode }) {
  return <NextStoryDock label={`Новая история · ${step} из 3`}>
    <div className="game-onboarding-meta"><span><span className="game-onboarding-segments" aria-hidden="true">{[1, 2, 3].map(index => <i key={index} className={index <= step ? 'on' : ''} />)}</span>Новая история · {step} из 3</span></div>
    <p>{children}</p>
  </NextStoryDock>
}

function NextStoryOffer({ player }: { player: Player }) {
  const nav = useNav()
  const [ready, setReady] = useState(false)
  const [pending, setPending] = useState<World>()
  const source = worldDocumentOf(player.world)
  const same = ready ? optionalWorldTemplate(source) : undefined
  const alternatives = ready ? worlds.filter(world => world.id !== same?.id && world.title !== source.title) : []
  const played = new Set(api.ownedWorldsSnapshot()?.map(world => optionalWorldTemplate(world)?.id))
  const other = alternatives.find(world => !played.has(world.id)) ?? alternatives[0]
  const target = other ?? same
  useEffect(() => {
    let active = true
    void api.loadWorldTemplates().then(() => { if (active) setReady(true) }).catch(() => { /* The optional offer can try again when returning to Channel. */ })
    return () => { active = false }
  }, [])
  useEffect(() => { if (target) nextStoryOfferShown(target.id) }, [target])
  if (!target) return null
  const art = worldArt[target.id]
  const pitch = art?.pitch || target.summary?.trim() || target.description

  const begin = (world: World, confirmed = false) => {
    selectDraftOwner(player.id)
    const previous = getWorldDraft()
    if (!confirmed && previous && (previous.definition.templateId !== world.id || previous.editingId)) { setPending(world); return }
    const definition = worldDefinitionOf(world.id)
    const sameTemplate = world.id === same?.id
    const sourceStar = sameTemplate ? definition.stars.find(star => source.description.includes(`\nПерсона игрока: ${star.name}.`) || (star.name === source.name && star.role === source.role)) : undefined
    const stored = savedWorldDraft(world.id)
    const draft: WorldDraft = stored && !stored.previewOnly && !stored.advanced ? stored : {
      definition, address: player.address, group: player.ageBand === '14-17' || !player.ageBand ? '14-17' : '18+',
      ageBand: player.ageBand, personalizeAvatar: avatarPersonalization(player.id),
    }
    const chooseHero = (!draft.personaChosen && !draft.persona) || (!!sourceStar && draft.persona?.star.id === sourceStar.id)
    const cast = draft.definition.stars
    const hero = cast.length > 1 || draft.persona ? cast.find(star => star.id !== sourceStar?.id) : undefined
    setWorldDraft(chooseHero ? withPersona(draft, hero) : draft)
    trackNextStory('accepted', world.id, { kind: sameTemplate ? 'same_world' : 'other_world' })
    nav.push('01-4_channel', { nextStory: true, sourceStarId: sourceStar?.id, world: world.id })
  }

  return <aside className="card next-story-offer" aria-label="Другая история">
    <div className="next-story-offer-head">
      <button type="button" className="next-story-world" aria-label={other ? `Попробовать «${target.title}»` : 'Сыграть за другого героя'} onClick={() => begin(target)}>
        <span className="next-story-thumb" aria-hidden="true">{art ? <img src={art.smallCover} alt="" decoding="async" /> : target.emoji}</span>
        <span className="next-story-copy"><span className="tiny muted">{other ? 'Другая история' : 'Другая роль'}</span><b>{target.title}</b></span>
        <span className="next-story-arrow" aria-hidden="true">›</span>
      </button>
      <button type="button" className="next-story-dismiss" aria-label="Закрыть предложение" onClick={() => dismissNextStory(target.id)}>×</button>
    </div>
    {pitch && <p className="small muted next-story-pitch">{pitch}</p>}
    {(other && same || alternatives.length > 1) && <div className="next-story-links">
      {other && same && <button type="button" className="text-button small" aria-label="Сыграть за другого героя в этом мире" onClick={() => begin(same)}>Другая роль</button>}
      {alternatives.length > 1 && <button type="button" className="text-button small" onClick={() => nav.push('01-3_world', { nextStory: true })}>Все миры →</button>}
    </div>}
    {pending && <WorldSwitchDialog title={getWorldDraft()?.definition.title ?? ''} onConfirm={() => begin(pending, true)} onCancel={() => setPending(undefined)} />}
  </aside>
}

export function NextStoryChannelOffer({ player }: { player: Player }) {
  const story = useNextStory(), original = useGameOnboarding()
  if (story && story.phase !== 'offered') return null
  if (original && (!original.completionSeen || original.crystalShopIntroPending)) return null
  const completedHere = original?.phase === 'done' && original.worldId === (player.worldInstanceId ?? player.world) && original.completionSeen
  if (!isNextStoryPreview() && (isGameOnboardingPreview() || !completedHere)) return null
  return <NextStoryOffer player={player} />
}

export function NextStoryChannelHint({ player }: { player: Player }) {
  const story = useNextStory()
  if (story?.phase !== 'post' || story.worldId !== (player.worldInstanceId ?? player.world)) return null
  return <NextStoryProgress step={3}>{contentOf(player.world).ideas.some(idea => idea.trim()) ? 'Идею можно переписать в редакторе.' : 'Напиши первый пост от лица своего героя.'}</NextStoryProgress>
}

export function NextStoryEditorHint({ player }: { player: Player }) {
  const story = useNextStory()
  if (story?.phase !== 'post' || story.worldId !== (player.worldInstanceId ?? player.world)) return null
  return <NextStoryProgress step={3}>{contentOf(player.world).ideas.some(idea => idea.trim()) ? 'Дополни идею или расскажи эту сцену своими словами.' : 'Расскажи, с чего начинается история твоего героя.'}</NextStoryProgress>
}
