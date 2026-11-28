import { clearGuestHandoff, readGuestHandoff, setRegistrationConsent } from '../app/landing/guestHandoff'
import { draftKey, hasPostDraft, savePostDraft } from '../app/postDraft'
import { event, tap, trackInviteAction } from '../app/analytics'
import { registerGameOnboarding, onboardingWorldSaved, nextStoryWorldSaved } from '../app/GameOnboardingState'
import { NextStoryProgress } from '../app/NextStory'
import { hasAccess, needsRegistration } from '../api/auth'
import { clearLandingWorld, pendingLandingWorld } from '../app/landing/experiment'
import {PushNotifications} from '../app/push/PushNotifications'
import {InstallApp} from '../app/install/InstallApp'
import { Resources } from '../ui/Resources'
import { WorldSwitchDialog } from '../ui/WorldSwitchDialog'
import { WorldCover } from '../ui/WorldCover'
import { useEffect, useState, type ReactNode } from 'react'
import { api, ApiError } from '../api/client'
import { optionalWorldTemplate, worldDefinitionOf, worldDocumentOf, worlds } from '../api/worldContent'
import type { Player, WorldDefinition, WorldDocument } from '../api/types'
import { useNav } from '../app/nav'
import { useStore } from '../app/store'
import { blankWorld, clearWorldDrafts, getWorldDraft, isOwnWorldSetup, ownWorldRole, savedWorldDraft, selectDraftOwner, setWorldDraft, useWorldDraft, withPersona, type WorldDraft } from '../app/worldDraft'
import { pendingReferral, clearReferral } from '../app/referral'
import { Avatar, BottomCTA, Button, TopBar, fmt, plural } from '../ui'
import { WorldAdvanced } from './worldAdvanced'
import { isDevWorldPrototype, worldArt } from '../app/worldPrototype'
import { QuickChannelForm, QuickWorldForm, DramaPicker, PersonaOption } from './worldQuick'
import type { AgeBand } from '../app/ageBand'
import { avatarPersonalization, setAvatarPersonalization } from '../app/avatarPersonalization'
import { displayHandle, heroHandle } from '../app/handle'

function SavedWorldCard({ world, player, duplicate, opening, onOpen }: { world: WorldDocument; player?: Player; duplicate?: number; opening: string | null; onOpen: () => void }) {
  const cover = optionalWorldTemplate(world)?.id ?? world.id
  return <button data-game-onboarding-world={world.id} className={`world-resume card${player ? ' world-resume-current' : ''}`} disabled={opening !== null} onClick={onOpen}>
    {worldArt[cover] && <WorldCover world={cover} catalog className="world-resume-cover" />}
    <div className="world-resume-body"><Avatar seed={world.avatar} avatarStyle={world.avatarStyle} profile={player ? { address: player.address, ageBand: player.ageBand, personalizeAvatar: avatarPersonalization(player.id) } : undefined} size={42} />
    <div>
      <div className="world-resume-heading"><b>{world.emoji} {world.title}</b>{player && <span className="world-resume-label">Текущая</span>}</div>
      <p className="small muted">{[world.name, world.role].filter(Boolean).join(' · ')}</p>
      <p className="small muted">{fmt(world.subscribers)} {plural(world.subscribers, 'подписчик', 'подписчика', 'подписчиков')}{duplicate ? ` · История ${duplicate}` : ''}</p>
      <span className="world-resume-action">{opening === world.id ? 'Открываем…' : 'Продолжить'}</span>
    </div></div>
  </button>
}

export function ChooseWorld() {
  const nav = useNav(); const { player, setPlayer } = useStore()
  const [owned, setOwned] = useState<WorldDocument[]>(() => api.ownedWorldsSnapshot() ?? [])
  const [error, setError] = useState('')
  const [pending, setPending] = useState<boolean | null>(null)
  const [opening, setOpening] = useState<string | null>(null)
  useEffect(() => { let alive = true; if (player) api.ownedWorlds().then(found => { if (alive) setOwned(found) }).catch(() => { if (alive) setError('Не получилось загрузить твои миры.') }); return () => { alive = false } }, [player?.id]) // eslint-disable-line react-hooks/exhaustive-deps
  function startOwn(previewOnly = false, confirmed = false) {
    selectDraftOwner(player?.id ?? api.playerId() ?? 'onboarding')
    const previous = getWorldDraft()
    if (!confirmed && previous && (previous.definition.templateId !== null || previous.editingId)) { setPending(previewOnly); return }
    const restored = savedWorldDraft('custom')
    if (restored && !!restored.previewOnly === previewOnly) { setWorldDraft(previewOnly ? restored : { ...restored, advanced: true }); nav.push('05-7b_own_world', {}); return }
    const definition = blankWorld()
    const profile = player ?? api.onboardingProfile()
    setWorldDraft({ definition, heroOrder: [], advanced: !previewOnly, previewOnly, address: profile?.address ?? 'nick', group: profile ? (profile.ageBand === '14-17' ? '14-17' : '18+') : (nav.params.group as '14-17' | '18+') ?? '14-17', ageBand: profile?.ageBand ?? nav.params.ageBand as AgeBand | undefined, birthYear: nav.params.birthYear as number | undefined, personalizeAvatar: player ? avatarPersonalization(player.id) : true })
    nav.push('05-7b_own_world', {})
  }
  const currentWorld = player ? owned.find(world => world.id === player.worldInstanceId) ?? { ...worldDocumentOf(player.world), id: player.worldInstanceId ?? player.world, name: player.name, avatar: player.handle, subscribers: player.subs } : undefined
  const otherWorlds = owned.filter(world => world.id !== currentWorld?.id)
  const stories = currentWorld ? [currentWorld, ...otherWorlds] : otherWorlds
  const matching = (world: WorldDocument) => {
    const copies = owned.filter(item => item.title === world.title && item.name === world.name && item.role === world.role && item.subscribers === world.subscribers)
    return copies.length > 1 ? copies.findIndex(item => item.id === world.id) + 1 : undefined
  }
  const resume = async (world: WorldDocument) => {
    if (opening) return
    setOpening(world.id); setError('')
    try {
      const next = await api.me(world.id)
      const created = getWorldDraft()?.createdWorldId === world.id
      if (created) setWorldDraft(null)
      clearLandingWorld(); setPlayer(next)
      if (created) nav.reset('05-7d_world_created')
      else nav.push('03-5_stage')
    }
    catch { setError('Не получилось открыть мир. Попробуй ещё раз.') }
    finally { setOpening(null) }
  }
  return <div className="screen no-tab"><TopBar title="Миры" onBack={nav.params.nextStory === true ? nav.back : undefined} rightPlain right={player ? <Resources player={player} /> : undefined} />
    <div className="pad world-catalog-page worlds-home"><InstallApp entry="banner" />{player && <PushNotifications key={player.id} playerId={player.id} entry="banner" />}{!player && <><h1>Выбери свою историю</h1><p className="muted">Готовые миры доступны сразу.</p></>}
      {getWorldDraft() && <Button secondary onClick={() => nav.push(getWorldDraft()?.definition.templateId === null ? '05-7b_own_world' : '01-4_channel')}>Продолжить настройку</Button>}
      {stories.length > 0 && <><h2>Мои истории</h2><div className="owned-stories">{stories.map(world => <SavedWorldCard key={world.id} world={world} player={world.id === currentWorld?.id ? player ?? undefined : undefined} duplicate={matching(world)} opening={opening} onOpen={() => resume(world)} />)}</div></>}
      {error && <p role="alert">{error}</p>}
      {player?.hitOwnWorldMilestone && <Button secondary onClick={() => startOwn()}>Создать свой мир</Button>}
      <h2>Выбери новую историю</h2><div className="world-catalog">{[...worlds].sort((a, b) => ['b3a1a522-0263-4796-a197-57a8731fc291', 'aec9494b-4118-4525-8458-e85d759c8dd9', '8c02a7fb-3b55-4eb0-b957-1b4869ed9834', '5720946a-ea7c-4a10-9c50-51ce3deefc53'].indexOf(a.id) - ['b3a1a522-0263-4796-a197-57a8731fc291', 'aec9494b-4118-4525-8458-e85d759c8dd9', '8c02a7fb-3b55-4eb0-b957-1b4869ed9834', '5720946a-ea7c-4a10-9c50-51ce3deefc53'].indexOf(b.id)).map((world, index) => { const art = worldArt[world.id]; const definition = worldDefinitionOf(world.id); return <button key={world.id} className="world-cover-card" onClick={() => { tap('world_open_overview', { template_id: world.id }); nav.push('01-3b_world_overview', { ...nav.params, world: world.id }) }} aria-label={`Обзор мира ${world.title}`}>
        {art && <div className="world-cover-image"><WorldCover world={world.id} catalog priority={index === 0} /><span className="world-genre">{world.emoji} {art.genre}</span></div>}
        <div className="world-cover-copy"><div className="row"><b>{world.title}</b><span className="small muted">{{ easy: 'Легко', normal: 'Средне', hard: 'Сложно' }[world.difficulty]}</span></div><p>{art?.pitch ?? world.summary}</p><div className="world-roster">{definition.stars.slice(0, 3).map(star => <Avatar key={star.id} seed={star.avatarSeed} avatarStyle={definition.avatarStyle} mode="default" size={26} />)}<span>Обзор мира →</span></div></div>
      </button> })}</div>
      {!player?.hitOwnWorldMilestone && <div className="card own-world-unlock"><h2>✦ Создай свой мир</h2>
        <div className="own-unlock-goal"><b>Пригласи 1 друга</b><span>0/1</span></div>
        <progress aria-label="Друзья с первым постом до открытия своего мира" value={0} max={1} />
        <p className="small muted">Мир откроется, когда друг зарегистрируется по твоей ссылке.</p>
        {player && <Button onClick={() => { trackInviteAction('invite_opened'); nav.push('04-3_invite') }}>Пригласить друга</Button>}
        <p className="small muted own-unlock-alternative">Или набери 500 подписчиков в одном мире.</p>
      </div>}
      {isDevWorldPrototype && new URLSearchParams(location.search).has('devtools') && <details className="editor-extra"><summary>DEV: пример настройки</summary><Button secondary onClick={() => startOwn(true)}>Попробовать быструю настройку · DEV</Button></details>}
      {pending !== null && <WorldSwitchDialog title={getWorldDraft()?.definition.title ?? ''} onConfirm={() => { const choice = pending; setPending(null); startOwn(choice, true) }} onCancel={() => setPending(null)} />}
    </div></div>
}
function Ready({ children }: { children: (draft: NonNullable<ReturnType<typeof useWorldDraft>>) => React.ReactNode }) {
  const draft = useWorldDraft(); const nav = useNav()
  if (!draft) return <div className="screen no-tab"><TopBar title="Настройка мира" onBack={nav.back} /><div className="pad"><p>Выбери шаблон, чтобы начать настройку.</p><Button onClick={() => nav.replace('01-3_world')}>Выбрать мир</Button></div></div>
  return <>{children(draft)}</>
}
export function ChannelSetup() {
  return <Ready>{draft => draft.advanced ? <StarsForm draft={draft} /> : <QuickChannelForm draft={draft} />}</Ready>
}
export function DescriptionSetup() { return <Ready>{draft => draft.advanced || draft.customEditing || isOwnWorldSetup(draft) ? <StarsForm draft={draft} /> : <QuickWorldForm draft={draft} />}</Ready> }
export function VoiceSetup() { return <Ready>{draft => <StarsForm draft={draft} quick={!draft.advanced} />}</Ready> }
export function AdvancedSetup() { return <Ready>{draft => <StarsForm draft={draft} />}</Ready> }
export function StarsSetup() { return <Ready>{draft => <StarsForm draft={draft} />}</Ready> }

function heroPersonaAvatar(star: WorldDefinition['stars'][number]) {
  return heroHandle(star.avatarSeed).slice(1) || star.name.trim().replace(/[^\p{L}\p{N}_-]+/gu, '_').replace(/^_+|_+$/g, '') || 'hero'
}

function StarsForm({ draft, quick = false }: { draft: NonNullable<ReturnType<typeof useWorldDraft>>; quick?: boolean }) {
  const nav = useNav(); const { setPlayer } = useStore(); const d = draft.definition
  const own = isOwnWorldSetup(draft)
  const ownRole = own || draft.customEditing ? ownWorldRole(draft) : d.role
  const nextStory = nav.params.nextStory === true
  const [confirmRole, setConfirmRole] = useState(!!draft.createdWorldId)
  const [editRole, setEditRole] = useState(false)
  const [saving, setSaving] = useState(false); const [error, setError] = useState(''); const [uncertain, setUncertain] = useState(false)
  useEffect(() => { if (error) document.querySelector('[role="alert"]')?.scrollIntoView({ block: 'nearest' }) }, [error])
  const devPreview = import.meta.env.DEV && new URLSearchParams(location.search).has('screen') && !hasAccess() && !needsRegistration()
  const checkWorlds = () => nav.replace('01-3_world')
  const save = async (role = ownRole, hero?: WorldDefinition['stars'][number]) => {
    if (saving || uncertain) return
    if (draft.previewOnly || devPreview) { nav.push('05-7c_world_preview'); return }
    const nextStoryPlayerId = nextStory ? api.playerId() : undefined
    setSaving(true); setError('')
    let creatingWorld = false
    let createdWorldId = draft.createdWorldId
    const chosen = hero ? withPersona(draft, { ...hero, avatarSeed: heroPersonaAvatar(hero) }).definition : d
    const definition: WorldDefinition = own ? {
      ...chosen, role, title: d.title.trim(), name: chosen.name.trim(), avatar: chosen.avatar.trim(), voice: d.voice.trim() ? d.voice : 'default',
      emoji: d.emoji.trim() || '✨',
      summary: d.summary.trim() && d.summary !== 'Твоя история' ? d.summary.trim() : d.description.trim().slice(0, 200),
      firstContext: hero && !d.firstContext.trim() ? d.description.trim() : d.firstContext,
      stars: chosen.stars.map(star => ({ ...star, avatarSeed: star.avatarSeed.trim() || star.id })),
    } : draft.customEditing ? { ...d, role, voice: d.voice.trim() ? d.voice : 'default' } : d
    try {
      if (!api.hasPlayer()) { if (!draft.birthYear) { nav.push('01-2_age', { returnToCreation: true }); return } const identity = await api.onboarding({ birthYear: draft.birthYear, address: draft.address, referralCode: pendingReferral() }); registerGameOnboarding(identity.id); clearReferral(); setWorldDraft(null); clearWorldDrafts('onboarding'); selectDraftOwner(identity.id); setWorldDraft(draft) }
      if (!createdWorldId && api.onboardingProfile()?.address !== draft.address) await api.settings({ address: draft.address })
      creatingWorld = !draft.editingId
      const templateId = d.templateId
      const savingPlayerId = api.playerId()
      const player = draft.editingId ? await api.saveWorld(draft.editingId, definition) : createdWorldId ? await api.me(createdWorldId) : await api.createWorld(definition, worldId => {
        if (api.playerId() !== savingPlayerId) return
        if (own) {
          createdWorldId = worldId
          setWorldDraft({ ...draft, definition, createdWorldId })
        }
        if (nextStoryPlayerId && templateId) nextStoryWorldSaved(worldId, templateId)
      })
      if (creatingWorld) {
        onboardingWorldSaved(player.worldInstanceId ?? player.world)
        const guest = readGuestHandoff()
        if (guest && !guest.dialogue && guest.worldId === pendingLandingWorld() && guest.worldId === d.templateId && guest.text.trim()) {
          const key = draftKey(player)
          if (!hasPostDraft(key)) {
            savePostDraft(key, { text: guest.text, ownWords: true })
            clearGuestHandoff()
          }
        }
        setRegistrationConsent(false)
      }
      setAvatarPersonalization(player.id, draft.personalizeAvatar !== false)
      clearLandingWorld(); setWorldDraft(null); setPlayer(player)
      if (own) nav.reset('05-7d_world_created')
      else if (nextStory) nav.reset('03-5_stage', { firstPostConfirmed: creatingWorld })
      else nav.replace('03-5_stage', { firstPostConfirmed: creatingWorld })
    } catch (cause) {
      event('onboarding_step', { step: 'world_save', ok: false, kind: cause instanceof ApiError ? String(cause.status) : 'unconfirmed' })
      if (createdWorldId) setError('Мир создан. Не получилось его открыть. Попробуй войти ещё раз.')
      else if (creatingWorld && !(cause instanceof ApiError)) { setUncertain(true); setError('Не удалось подтвердить создание. Сначала проверь список миров: повтор может создать ещё один экземпляр.') }
      else setError(cause instanceof ApiError && cause.status === 409 ? 'Мир изменился или создание с нуля пока недоступно. Твои изменения сохранены в черновике.' : cause instanceof ApiError && cause.status === 503 ? 'Проверка истории временно недоступна. Настройки сохранены — попробуй начать ещё раз чуть позже.' : 'Не получилось сохранить мир. Проверь поля и попробуй снова.')
    } finally { setSaving(false) }
  }
  if (own && confirmRole) {
    const choice = draft.ownCharacter
    const ownSelected = !!choice && !choice.heroId
    const hero = d.stars.length > 1 ? d.stars.find(star => star.id === choice?.heroId) : undefined
    const role = draft.createdWorldId ? d.role : hero?.role.trim() ?? ownRole
    const incomplete = ownSelected && (!d.name.trim() || !d.avatar.trim() || !ownRole)
    const canCreate = !!role && (!!hero || ownSelected && !incomplete)
    const choose = (heroId?: string) => setWorldDraft({ ...draft, ownCharacter: { ...choice, heroId } })
    const edit = () => { setEditRole(true); setConfirmRole(false); setError('') }
    return <div className="screen no-tab own-world-role">
      <TopBar title="Твой персонаж" onBack={saving ? undefined : draft.createdWorldId ? checkWorlds : edit} />
      <div className="pad">
        <p className="small muted">{d.emoji} {d.title}</p>
        <h1>{draft.createdWorldId ? 'Мир создан' : 'Кем будешь играть?'}</h1>
        {draft.createdWorldId ? <div className="card editor-preview">
          <Avatar seed={d.avatar} mode="profile" avatarStyle={d.avatarStyle} profile={{ address: draft.address, ageBand: draft.ageBand, personalizeAvatar: draft.personalizeAvatar }} size={52} />
          <div><b>{d.name}</b><p className="small muted">{displayHandle(d.avatar)}</p><p>{role}</p></div>
        </div> : <div className="own-persona-options">
          {d.stars.map(star => <PersonaOption key={star.id} label={star.name}
            description={d.stars.length === 1 ? `${star.role}. Останется в мире и сможет отвечать на посты.` : star.role}
            portrait={<Avatar seed={d.stars.length > 1 ? heroPersonaAvatar(star) : star.avatarSeed || star.id} avatarStyle={d.avatarStyle} mode="default" />}
            selected={hero?.id === star.id} disabled={saving || uncertain || d.stars.length === 1} onClick={() => choose(star.id)} />)}
          <PersonaOption label="Свой персонаж" description={[d.name, ownRole].filter(Boolean).join(' · ') || 'Твоё имя, твоя роль в истории'}
            portrait={d.avatar ? <Avatar seed={d.avatar} mode="profile" avatarStyle={d.avatarStyle} profile={{ address: draft.address, ageBand: draft.ageBand, personalizeAvatar: draft.personalizeAvatar }} /> : undefined}
            selected={ownSelected} disabled={saving || uncertain} onClick={() => choose()} />
        </div>}
        {ownSelected && incomplete && !draft.createdWorldId && <p className="small muted">Для своего персонажа заполни имя, ник и роль в редакторе.</p>}
        {error && <p className="down" role="alert">{error}</p>}
        <Button disabled={saving || uncertain || (!draft.createdWorldId && !canCreate)} onClick={() => void save(role, hero)}>
          {saving ? <span role="status">{draft.createdWorldId ? 'Открываем мир…' : 'Создаём мир…'}</span> : draft.createdWorldId ? 'Открыть мир' : 'Создать мир'}
        </Button>
        {uncertain ? <Button secondary onClick={checkWorlds}>Проверить список миров</Button> : !draft.createdWorldId && <Button secondary disabled={saving} onClick={edit}>Изменить персонажа</Button>}
      </div>
    </div>
  }
  if (quick) return <QuickMoodSetup draft={draft} saving={saving} disabled={uncertain} action={saving ? 'Сохраняем…' : draft.previewOnly ? 'Посмотреть пример' : draft.editingId ? 'Сохранить изменения' : 'Начать историю'} onStart={() => void save()}>
    {draft.previewOnly && <p className="small prototype-note">DEV-прототип: дальше — пример ленты, без создания мира в базе.</p>}
    {devPreview && <p className="small prototype-note">DEV-предпросмотр без сохранения.</p>}{error && <p role="alert">{error}</p>}{uncertain && <Button onClick={() => nav.replace('01-3_world')}>Проверить список миров</Button>}
  </QuickMoodSetup>
  return <WorldAdvanced draft={draft} saving={saving} error={error} uncertain={uncertain} editRole={editRole} onCheck={checkWorlds} onSave={() => {
    if (own && !devPreview) { setConfirmRole(true); window.scrollTo(0, 0) }
    else void save()
  }} />
}

export function WorldCreated() {
  const nav = useNav(); const { player } = useStore()
  const world = worldDocumentOf(player!.world)
  return <div className="screen no-tab own-world-arrival">
    <div className="pad">
      <div className="own-world-arrival-mark" aria-hidden="true">{world.emoji}</div>
      <p className="small muted">Мир создан</p>
      <h1>{world.title}</h1>
      <div className="card editor-preview">
        <Avatar seed={world.avatar} avatarStyle={world.avatarStyle} mode="profile" size={46} />
        <div><span className="small muted">Ты играешь за</span><b>{world.name}</b><span>{world.role}</span></div>
      </div>
      <section>
        <h2>Кого встретишь</h2>
        <ul className="own-world-cast">{world.stars.map(star => <li key={star.id}>
          <Avatar seed={star.avatarSeed} avatarStyle={world.avatarStyle} mode="default" size={36} />
          <div><b>{star.name}</b><span className="small muted">{[star.role, heroHandle(star.avatarSeed)].filter(Boolean).join(' · ')}</span></div>
        </li>)}</ul>
      </section>
      <Button onClick={() => nav.reset('03-5_stage', { firstPostConfirmed: true })}>Войти в мир</Button>
    </div>
  </div>
}

export function QuickMoodSetup({ draft, saving = false, disabled = false, fixed = false, action = 'Начать историю', onStart, children }: {
  draft: NonNullable<ReturnType<typeof useWorldDraft>>
  saving?: boolean
  disabled?: boolean
  fixed?: boolean
  action?: string
  onStart: () => void
  children?: ReactNode
}) {
  const nav = useNav(); const d = draft.definition
  const nextStory = !fixed && nav.params.nextStory === true
  return <div className="screen no-tab story-setup mood-setup"><TopBar title="Настроение истории" onBack={saving ? undefined : nav.back} /><div className="pad">
    <DramaPicker definition={d} fixed={fixed} withDifficulty={!fixed} />
    {!fixed && !nextStory && <div className="drama-world-edit"><Button secondary onClick={() => nav.push('05-12_world_advanced')}>Редактировать мир</Button><p className="small muted">Мир можно будет отредактировать позже</p></div>}
    <div className="quick-summary"><Avatar seed={d.avatar} avatarStyle={d.avatarStyle} profile={fixed ? undefined : { address: draft.address, ageBand: draft.ageBand, personalizeAvatar: draft.personalizeAvatar }} /><div><b>{d.name}</b><div className="small muted">{d.title} · {d.role}</div></div></div>
    {children}
  </div><BottomCTA main={action} hint={nextStory && <NextStoryProgress step={2}>Выбери, как быстро расти и насколько горячими будут споры.</NextStoryProgress>} disabled={saving || disabled || (!fixed && ((!draft.customEditing && !d.voice.trim()) || d.stars.length === 0))} onMain={onStart} /></div>
}
