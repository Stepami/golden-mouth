import { hasAccess, needsRegistration } from '../api/auth'
import { clearLandingWorld } from '../app/landing/experiment'
import {PushNotifications} from '../app/push/PushNotifications'
import {InstallApp} from '../app/install/InstallApp'
import { Resources } from '../ui/Resources'
import { WorldSwitchDialog } from '../ui/WorldSwitchDialog'
import { WorldCover } from '../ui/WorldCover'
import { useEffect, useState } from 'react'
import { api, ApiError } from '../api/client'
import { worldDefinitionOf, worldDocumentOf, worlds } from '../api/worldContent'
import type { WorldDocument } from '../api/types'
import { useNav } from '../app/nav'
import { useStore } from '../app/store'
import { blankWorld, clearWorldDrafts, getWorldDraft, savedWorldDraft, selectDraftOwner, setWorldDraft, updateWorldDraft, useWorldDraft } from '../app/worldDraft'
import { pendingReferral, clearReferral } from '../app/referral'
import { Avatar, BottomCTA, Button, Card, Segmented, TopBar } from '../ui'
import { WorldAdvanced } from './worldAdvanced'
import { isDevWorldPrototype, worldArt } from '../app/worldPrototype'
import { QuickChannelForm, QuickWorldForm, DramaPicker } from './worldQuick'
import type { AgeBand } from '../app/ageBand'
import { avatarPersonalization, setAvatarPersonalization } from '../app/avatarPersonalization'

export function ChooseWorld() {
  const nav = useNav(); const { player, setPlayer } = useStore()
  const [owned, setOwned] = useState<WorldDocument[]>(() => api.ownedWorldsSnapshot() ?? [])
  const [error, setError] = useState('')
  const [pending, setPending] = useState<boolean | null>(null)
  useEffect(() => { let alive = true; if (player) api.ownedWorlds().then(found => { if (alive) setOwned(found) }).catch(() => { if (alive) setError('Не получилось загрузить твои миры.') }); return () => { alive = false } }, [player?.id]) // eslint-disable-line react-hooks/exhaustive-deps
  function startOwn(previewOnly = false, confirmed = false) {
    selectDraftOwner(player?.id ?? api.playerId() ?? 'onboarding')
    const previous = getWorldDraft()
    if (!confirmed && previous && (previous.definition.templateId !== null || previous.editingId)) { setPending(previewOnly); return }
    const restored = savedWorldDraft('custom')
    if (restored && !!restored.previewOnly === previewOnly) { setWorldDraft(restored); nav.push('05-7b_own_world', {}); return }
    const definition = blankWorld()
    const profile = player ?? api.onboardingProfile()
    setWorldDraft({ definition, heroOrder: [], advanced: !previewOnly, previewOnly, address: profile?.address ?? 'nick', group: profile ? (profile.ageBand === '14-17' ? '14-17' : '18+') : (nav.params.group as '14-17' | '18+') ?? '14-17', ageBand: profile?.ageBand ?? nav.params.ageBand as AgeBand | undefined, personalizeAvatar: player ? avatarPersonalization(player.id) : true })
    nav.push('05-7b_own_world', {})
  }
  const currentWorld = player ? owned.find(world => world.id === player.worldInstanceId) ?? { ...worldDocumentOf(player.world), id: player.worldInstanceId ?? player.world, name: player.name, avatar: player.handle, subscribers: player.subs } : undefined
  const otherWorlds = owned.filter(world => world.id !== currentWorld?.id)
  const resume = async (world: WorldDocument) => { try { const next = await api.me(world.id); clearLandingWorld(); setPlayer(next); nav.push('03-5_stage') } catch { setError('Не получилось открыть мир.') } }
  return <div className="screen no-tab"><TopBar title="Миры" rightPlain right={player ? <Resources player={player} /> : undefined} />
    <div className="pad world-catalog-page worlds-home"><InstallApp entry="banner" />{player && <PushNotifications key={player.id} playerId={player.id} entry="banner" />}{!player && <><h1>Выбери свою историю</h1><p className="muted">Готовые миры доступны сразу.</p></>}
      {getWorldDraft() && <Button secondary onClick={() => nav.push(getWorldDraft()?.definition.templateId === null ? '05-7b_own_world' : '01-4_channel')}>Продолжить настройку</Button>}
      {currentWorld && <><h2>Продолжить историю</h2><button className="world-resume card" onClick={() => resume(currentWorld)}><Avatar seed={currentWorld.avatar} avatarStyle={currentWorld.avatarStyle} profile={player ? { address: player.address, ageBand: player.ageBand, personalizeAvatar: avatarPersonalization(player.id) } : undefined} size={48} /><div><b>{currentWorld.name}</b><p className="small muted">{currentWorld.emoji} {currentWorld.title} · {currentWorld.subscribers} подписчиков</p><span>Продолжить →</span></div></button></>}
      {otherWorlds.length > 0 && <><h2>Мои истории</h2><div className="owned-stories">{otherWorlds.map(world => <button className="continue-world card" key={world.id} onClick={() => resume(world)}><Avatar seed={world.avatar} avatarStyle={world.avatarStyle} size={36} /><div><b>{world.name}</b><div className="small muted">{world.emoji} {world.title} · {world.subscribers} подписчиков</div></div><span>Играть →</span></button>)}</div></>}
      <h2>Выбери новую историю</h2><div className="world-catalog">{[...worlds].sort((a, b) => ['b3a1a522-0263-4796-a197-57a8731fc291', 'aec9494b-4118-4525-8458-e85d759c8dd9', '8c02a7fb-3b55-4eb0-b957-1b4869ed9834', '5720946a-ea7c-4a10-9c50-51ce3deefc53'].indexOf(a.id) - ['b3a1a522-0263-4796-a197-57a8731fc291', 'aec9494b-4118-4525-8458-e85d759c8dd9', '8c02a7fb-3b55-4eb0-b957-1b4869ed9834', '5720946a-ea7c-4a10-9c50-51ce3deefc53'].indexOf(b.id)).map((world, index) => { const art = worldArt[world.id]; const definition = worldDefinitionOf(world.id); return <button key={world.id} className="world-cover-card" onClick={() => nav.push('01-3b_world_overview', { ...nav.params, world: world.id })} aria-label={`Обзор мира ${world.title}`}>
        {art && <div className="world-cover-image"><WorldCover world={world.id} catalog priority={index === 0} /><span className="world-genre">{world.emoji} {art.genre}</span></div>}
        <div className="world-cover-copy"><div className="row"><b>{world.title}</b><span className="small muted">{{ easy: 'Легко', normal: 'Средне', hard: 'Сложно' }[world.difficulty]}</span></div><p>{art?.pitch ?? world.summary}</p><div className="world-roster">{definition.stars.slice(0, 3).map(star => <Avatar key={star.id} seed={star.avatarSeed} avatarStyle={definition.avatarStyle} mode="default" size={26} />)}<span>Обзор мира →</span></div></div>
      </button> })}</div>
      <div className="card own-world-unlock"><h2>✦ Создай свой мир</h2>{player?.hitOwnWorldMilestone ? <><p className="small muted">Придумай место, героев и начало истории.</p><Button onClick={() => startOwn()}>Создать свой мир</Button></> : <>
        <div className="own-unlock-goal"><b>Пригласи 1 друга</b><span>0/1</span></div>
        <progress aria-label="Друзья с первым постом до открытия своего мира" value={0} max={1} />
        <p className="small muted">Мир откроется, когда друг опубликует первый пост.</p>
        {player && <Button onClick={() => nav.push('04-3_invite')}>Пригласить друга</Button>}
        <p className="small muted own-unlock-alternative">Или набери 500 подписчиков в одном мире.</p>
      </>}
        {isDevWorldPrototype && new URLSearchParams(location.search).has('devtools') && <details className="editor-extra"><summary>DEV: пример настройки</summary><Button secondary onClick={() => startOwn(true)}>Попробовать быструю настройку · DEV</Button></details>}
      </div>
      {error && <p role="alert">{error}</p>}
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
export function DescriptionSetup() { return <Ready>{draft => draft.advanced ? <StarsForm draft={draft} /> : <QuickWorldForm draft={draft} />}</Ready> }
export function VoiceSetup() { return <Ready>{draft => <StarsForm draft={draft} quick={!draft.advanced} />}</Ready> }
export function AdvancedSetup() { return <Ready>{draft => <StarsForm draft={draft} />}</Ready> }
export function StarsSetup() { return <Ready>{draft => <StarsForm draft={draft} />}</Ready> }
function StarsForm({ draft, quick = false }: { draft: NonNullable<ReturnType<typeof useWorldDraft>>; quick?: boolean }) {
  const nav = useNav(); const { setPlayer } = useStore(); const d = draft.definition
  const [saving, setSaving] = useState(false); const [error, setError] = useState(''); const [uncertain, setUncertain] = useState(false)
  useEffect(() => { if (error) document.querySelector('[role="alert"]')?.scrollIntoView({ block: 'center', behavior: 'smooth' }) }, [error])
  const devPreview = import.meta.env.DEV && new URLSearchParams(location.search).has('screen') && !hasAccess() && !needsRegistration()
  const save = async () => {
    if (saving || uncertain) return
    if (draft.previewOnly || devPreview) { nav.push('05-7c_world_preview'); return }
    setSaving(true); setError('')
    let creatingWorld = false
    try {
      if (!api.hasPlayer()) { const identity = await api.onboarding({ birthGroup: draft.group, ageBand: draft.ageBand, address: draft.address, referralCode: pendingReferral() }); clearReferral(); setWorldDraft(null); clearWorldDrafts('onboarding'); selectDraftOwner(identity.id); setWorldDraft(draft) }
      await api.settings({ address: draft.address })
      creatingWorld = !draft.editingId
      const player = draft.editingId ? await api.saveWorld(draft.editingId, d) : await api.createWorld(d)
      setAvatarPersonalization(player.id, draft.personalizeAvatar !== false)
      clearLandingWorld(); setWorldDraft(null); setPlayer(player); nav.replace('03-5_stage', { firstPostConfirmed: creatingWorld })
    } catch (cause) {
      if (creatingWorld && !(cause instanceof ApiError)) { setUncertain(true); setError('Не удалось подтвердить создание. Сначала проверь список миров: повтор может создать ещё один экземпляр.') }
      else setError(cause instanceof ApiError && cause.status === 409 ? 'Мир изменился или создание с нуля пока недоступно. Твои изменения сохранены в черновике.' : cause instanceof ApiError && cause.status === 503 ? 'Проверка истории временно недоступна. Настройки сохранены — попробуй начать ещё раз чуть позже.' : 'Не получилось сохранить мир. Проверь поля и попробуй снова.')
    } finally { setSaving(false) }
  }
  if (quick) return <div className="screen no-tab"><TopBar title="Настроение истории" onBack={saving ? undefined : nav.back} /><div className="pad"><DramaPicker definition={d} beforeOptions={<div className="drama-world-edit"><Button secondary onClick={() => nav.push('05-12_world_advanced')}>Редактировать мир</Button><p className="small muted">Мир можно будет отредактировать позже</p></div>} />
    <div className="quick-summary"><Avatar seed={d.avatar} avatarStyle={d.avatarStyle} profile={{ address: draft.address, ageBand: draft.ageBand, personalizeAvatar: draft.personalizeAvatar }} /><div><b>{d.name}</b><div className="small muted">{d.title} · {d.role}</div></div></div>
    {draft.previewOnly && <p className="small prototype-note">DEV-прототип: дальше — пример ленты, без создания мира в базе.</p>}
    {devPreview && <p className="small prototype-note">DEV-предпросмотр без сохранения.</p>}{error && <p role="alert">{error}</p>}{uncertain && <Button onClick={() => nav.replace('01-3_world')}>Проверить список миров</Button>}
  </div><BottomCTA main={saving ? 'Сохраняем…' : draft.previewOnly ? 'Посмотреть пример' : draft.editingId ? 'Сохранить изменения' : 'Начать историю'} disabled={saving || uncertain || !d.voice.trim() || d.stars.length === 0} onMain={save} /></div>
  return <WorldAdvanced draft={draft} saving={saving} error={error} uncertain={uncertain} onSave={save} onCheck={() => nav.replace('01-3_world')} />
}
