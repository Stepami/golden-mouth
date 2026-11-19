import { useEffect, useLayoutEffect, useRef, useState, type ReactNode } from 'react'
import { api } from '../api/client'
import type { Player, World } from '../api/types'
import { optionalWorldTemplate, worldDefinitionOf, worldDocumentOf, worlds } from '../api/worldContent'
import { WorldSwitchDialog } from '../ui/WorldSwitchDialog'
import { avatarPersonalization } from './avatarPersonalization'
import { dismissNextStory, isGameOnboardingPreview, isNextStoryPreview, nextStoryOfferShown, trackNextStory, useGameOnboarding, useNextStory } from './GameOnboardingState'
import { useNav } from './nav'
import { getWorldDraft, savedWorldDraft, selectDraftOwner, setWorldDraft, withPersona, type WorldDraft } from './worldDraft'

const evening = 'b3a1a522-0263-4796-a197-57a8731fc291'
const hogwarts = 'aec9494b-4118-4525-8458-e85d759c8dd9'
const hooks: Record<string, { title: string; text: string }> = {
  [evening]: { title: 'Кто попал в кадр у леса?', text: 'Гроза сорвала бейсбол. Изабелла успела сделать последний снимок.' },
  [hogwarts]: { title: 'Школа или отель?', text: 'Хогвартс закрылся. Гарри хочет вернуть учеников, а Драко предлагает открыть в замке отель.' },
  '8c02a7fb-3b55-4eb0-b957-1b4869ed9834': { title: 'Кто придёт на помощь Северу?', text: 'В Драконьем Логове увидели мертвеца. Теперь каждый дом должен решить, за кого он готов сражаться.' },
  '5720946a-ea7c-4a10-9c50-51ce3deefc53': { title: 'Курс обещает работу. А что получат выпускники?', text: 'Меркантил продаёт «Вкат», Куляш требует доказательств, а Ника ищет людей с опытом.' },
}

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
  const other = alternatives.find(world => world.id === (same?.id === evening ? hogwarts : evening)) ?? alternatives[0]
  useEffect(() => {
    let active = true
    void api.loadWorldTemplates().then(() => { if (active) setReady(true) }).catch(() => { /* The optional offer can try again when returning to Channel. */ })
    return () => { active = false }
  }, [])
  useEffect(() => { if (other) nextStoryOfferShown(other.id) }, [other])
  if (!other) return null
  const hook = hooks[other.id] ?? { title: other.title, text: other.description }

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

  return <NextStoryDock label="Другая история">
    <div className="game-onboarding-meta"><span>Другая история</span><button type="button" className="next-story-dismiss" aria-label="Закрыть предложение" onClick={() => dismissNextStory(other.id)}>×</button></div>
    <h2>{hook.title}</h2><p>{hook.text}</p>
    <button type="button" className="game-onboarding-collect" onClick={() => begin(other)}>Попробовать «{other.title}»</button>
    {same && <button type="button" className="next-story-same" onClick={() => begin(same)}>Сыграть за другого героя в этом мире</button>}
    {pending && <WorldSwitchDialog title={getWorldDraft()?.definition.title ?? ''} onConfirm={() => begin(pending, true)} onCancel={() => setPending(undefined)} />}
  </NextStoryDock>
}

export function NextStoryChannelHint({ player }: { player: Player }) {
  const story = useNextStory(), original = useGameOnboarding()
  const worldId = player.worldInstanceId ?? player.world
  if (story?.phase === 'post' && story.worldId === worldId) return <NextStoryProgress step={3}>Идею можно переписать в редакторе.</NextStoryProgress>
  if (story && story.phase !== 'offered') return null
  if (original && !original.completionSeen) return null
  const completedHere = original?.phase === 'done' && original.worldId === worldId && original.completionSeen
  if (!isNextStoryPreview() && (isGameOnboardingPreview() || !completedHere)) return null
  return <NextStoryOffer player={player} />
}

export function NextStoryEditorHint({ player }: { player: Player }) {
  const story = useNextStory()
  if (story?.phase !== 'post' || story.worldId !== (player.worldInstanceId ?? player.world)) return null
  return <NextStoryProgress step={3}>Дополни идею или расскажи эту сцену своими словами.</NextStoryProgress>
}
