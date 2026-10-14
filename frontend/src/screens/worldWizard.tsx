import { WorldCover } from '../ui/WorldCover'
import { useEffect, useRef, useState } from 'react'
import { api, ApiError } from '../api/client'
import { worldDefinitionOf, worldDocumentOf, worlds } from '../api/worldContent'
import type { WorldDocument, WorldId } from '../api/types'
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
  const [pending, setPending] = useState<{ world?: WorldId; previewOnly: boolean } | null>(null)
  const switchDialog = useRef<HTMLDialogElement>(null)
  useEffect(() => {
    if (!pending) return
    const previous = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    switchDialog.current?.showModal()
    return () => { switchDialog.current?.close(); document.body.style.overflow = previous }
  }, [pending])
  useEffect(() => { let alive = true; if (player) api.ownedWorlds().then(found => { if (alive) setOwned(found) }).catch(() => { if (alive) setError('Не получилось загрузить твои миры.') }); return () => { alive = false } }, [player?.id]) // eslint-disable-line react-hooks/exhaustive-deps
  function start(world?: WorldId, previewOnly = false, confirmed = false) {
    selectDraftOwner(player?.id ?? 'onboarding')
    const previous = getWorldDraft()
    if (!confirmed && previous && (previous.definition.balanceKey !== (world ?? 'own') || previous.editingId)) { setPending({ world, previewOnly }); return }
    const restored = savedWorldDraft(world ?? 'own')
    if (restored && !!restored.previewOnly === previewOnly) { setWorldDraft(restored); nav.push(world ? '01-3b_world_overview' : '05-7b_own_world', { world: restored.definition.balanceKey }); return }
    const definition = world ? worldDefinitionOf(world) : blankWorld()
    setWorldDraft({ definition, heroOrder: definition.stars.map(star => star.id), advanced: !world && !previewOnly, previewOnly, address: player?.address ?? 'nick', group: player?.ageBand ? (player.ageBand === '14-17' ? '14-17' : '18+') : (nav.params.group as '14-17' | '18+') ?? '14-17', ageBand: player?.ageBand ?? nav.params.ageBand as AgeBand | undefined, personalizeAvatar: player ? avatarPersonalization(player.id) : true })
    nav.push(world ? '01-3b_world_overview' : '05-7b_own_world', { world: definition.balanceKey })
  }
  const currentWorld = player ? owned.find(world => world.id === player.worldInstanceId) ?? { ...worldDocumentOf(player.world), id: player.worldInstanceId ?? player.world, name: player.name, avatar: player.handle, subscribers: player.subs } : undefined
  const otherWorlds = owned.filter(world => world.id !== currentWorld?.id)
  const resume = async (world: WorldDocument) => { try { const next = await api.me(world.id); setPlayer(next); nav.push('03-5_stage') } catch { setError('Не получилось открыть мир.') } }
  return <div className="screen no-tab"><TopBar title="Миры" onBack={nav.canBack ? nav.back : undefined} />
    <div className="pad world-catalog-page worlds-home">{!player && <><h1>Выбери свою историю</h1><p className="muted">Готовые миры доступны сразу.</p></>}
      {getWorldDraft() && <Button secondary onClick={() => nav.push(getWorldDraft()?.definition.balanceKey === 'own' ? '05-7b_own_world' : '01-4_channel')}>Продолжить настройку</Button>}
      {currentWorld && <><h2>Продолжить историю</h2><button className="world-resume card" onClick={() => resume(currentWorld)}><Avatar seed={currentWorld.avatar} avatarStyle={currentWorld.avatarStyle} profile={player ? { address: player.address, ageBand: player.ageBand, personalizeAvatar: avatarPersonalization(player.id) } : undefined} size={48} /><div><b>{currentWorld.name}</b><p className="small muted">{currentWorld.emoji} {currentWorld.title} · {currentWorld.subscribers} подписчиков</p><span>Продолжить →</span></div></button></>}
      {otherWorlds.length > 0 && <><h2>Мои истории</h2><div className="owned-stories">{otherWorlds.map(world => <button className="continue-world card" key={world.id} onClick={() => resume(world)}><Avatar seed={world.avatar} avatarStyle={world.avatarStyle} size={36} /><div><b>{world.name}</b><div className="small muted">{world.emoji} {world.title} · {world.subscribers} подписчиков</div></div><span>Играть →</span></button>)}</div></>}
      <h2>Выбери новую историю</h2><div className="world-catalog">{[...worlds].sort((a, b) => ['evening', 'sorcerers', 'houses', 'it'].indexOf(a.id) - ['evening', 'sorcerers', 'houses', 'it'].indexOf(b.id)).map((world, index) => { const art = worldArt[world.id]; const definition = worldDefinitionOf(world.id); return <button key={world.id} className="world-cover-card" onClick={() => start(world.id)} aria-label={`Обзор мира ${world.title}`}>
        {art && <div className="world-cover-image"><WorldCover world={world.id} catalog priority={index === 0} /><span className="world-genre">{world.emoji} {art.genre}</span></div>}
        <div className="world-cover-copy"><div className="row"><b>{world.title}</b><span className="small muted">{['', 'Легко', 'Средне', 'Сложно'][world.difficulty]}</span></div><p>{art?.pitch ?? world.summary}</p><div className="world-roster">{definition.stars.slice(0, 3).map(star => <Avatar key={star.id} seed={star.avatarSeed} avatarStyle={definition.avatarStyle} mode="default" size={26} />)}<span>Обзор мира →</span></div></div>
      </button> })}</div>
      <div className="card own-world-unlock"><h2>✦ Создай свой мир</h2>{player?.hitOwnWorldMilestone ? <><p className="small muted">Придумай место, героев и начало истории.</p><Button onClick={() => start()}>Создать свой мир</Button></> : <><p className="small muted own-unlock-summary">Откроется после 500 подписчиков в одном канале или после первого поста приглашённого друга.</p>{player && <><progress aria-label="Подписчики до открытия своего мира" value={Math.min(500, Math.max(player.subs, ...owned.map(world => world.subscribers)))} max={500} /><span className="small muted">{Math.min(500, Math.max(player.subs, ...owned.map(world => world.subscribers)))} / 500</span><button className="text-link" onClick={() => nav.push('04-3_invite')}>Пригласить друга →</button></>}</>}
        {isDevWorldPrototype && new URLSearchParams(location.search).has('devtools') && <details className="editor-extra"><summary>DEV: пример настройки</summary><Button secondary onClick={() => start(undefined, true)}>Попробовать быструю настройку · DEV</Button></details>}
      </div>
      {error && <p role="alert">{error}</p>}
      {pending && <dialog ref={switchDialog} className="world-switch-dialog" aria-labelledby="world-switch-title" onCancel={e => { e.preventDefault(); setPending(null) }}>
        <h2 id="world-switch-title">Открыть другой мир?</h2>
        <p className="muted">Настройка «{getWorldDraft()?.definition.title || 'Твой мир'}» сохранится на этом устройстве в текущей сессии. Когда снова выберешь этот мир, сможешь продолжить.</p>
        <Button onClick={() => { const choice = pending; setPending(null); start(choice.world, choice.previewOnly, true) }}>Открыть другой мир</Button>
        <Button secondary onClick={() => setPending(null)}>Остаться в текущем</Button>
      </dialog>}
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
export function StarsSetup() { return <Ready>{draft => <StarsForm draft={draft} />}</Ready> }
function StarsForm({ draft, quick = false }: { draft: NonNullable<ReturnType<typeof useWorldDraft>>; quick?: boolean }) {
  const nav = useNav(); const { setPlayer } = useStore(); const d = draft.definition
  const [saving, setSaving] = useState(false); const [error, setError] = useState(''); const [uncertain, setUncertain] = useState(false)
  useEffect(() => { if (error) document.querySelector('[role="alert"]')?.scrollIntoView({ block: 'center', behavior: 'smooth' }) }, [error])
  const save = async () => {
    if (saving || uncertain) return
    if (draft.previewOnly) { nav.push('05-7c_world_preview'); return }
    setSaving(true); setError('')
    try {
      if (!api.hasPlayer()) { const identity = await api.onboarding({ birthGroup: draft.group, ageBand: draft.ageBand, address: draft.address, referralCode: pendingReferral() }); clearReferral(); setWorldDraft(null); clearWorldDrafts('onboarding'); selectDraftOwner(identity.id); setWorldDraft(draft) }
      await api.settings({ address: draft.address })
      const player = draft.editingId ? await api.saveWorld(draft.editingId, d) : await api.createWorld(d)
      setAvatarPersonalization(player.id, draft.personalizeAvatar !== false)
      setWorldDraft(null); setPlayer(player); nav.replace('03-5_stage')
    } catch (cause) {
      if (!draft.editingId && !(cause instanceof ApiError)) { setUncertain(true); setError('Не удалось подтвердить создание. Сначала проверь список миров: повтор может создать ещё один экземпляр.') }
      else setError(cause instanceof ApiError && cause.status === 409 ? 'Мир изменился или создание с нуля пока недоступно. Твои изменения сохранены в черновике.' : cause instanceof ApiError && cause.status === 503 ? 'Проверка истории временно недоступна. Настройки сохранены — попробуй начать ещё раз чуть позже.' : 'Не получилось сохранить мир. Проверь поля и попробуй снова.')
    } finally { setSaving(false) }
  }
  if (quick) return <div className="screen no-tab"><TopBar title="Настроение истории" onBack={saving ? undefined : nav.back} /><div className="pad"><DramaPicker definition={d} />
    <div className="quick-summary"><Avatar seed={d.avatar} avatarStyle={d.avatarStyle} profile={{ address: draft.address, ageBand: draft.ageBand, personalizeAvatar: draft.personalizeAvatar }} /><div><b>{d.name}</b><div className="small muted">{d.title} · {d.role}</div></div></div>
    {draft.previewOnly && <p className="small prototype-note">DEV-прототип: дальше — пример ленты, без создания мира в базе.</p>}
    <button className="text-link" onClick={() => { setWorldDraft({ ...draft, advanced: true }); nav.push('01-4_channel') }}>Расширенные настройки →</button>
    {error && <p role="alert">{error}</p>}{uncertain && <Button onClick={() => nav.replace('01-3_world')}>Проверить список миров</Button>}
  </div><BottomCTA main={saving ? 'Сохраняем…' : draft.previewOnly ? 'Посмотреть свою историю' : draft.editingId ? 'Сохранить изменения' : 'Начать историю'} disabled={saving || uncertain || !d.voice.trim() || d.stars.length === 0} onMain={save} /></div>
  return <WorldAdvanced draft={draft} saving={saving} error={error} uncertain={uncertain} onSave={save} onCheck={() => nav.replace('01-3_world')} />
}
