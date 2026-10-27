import welcomeSki from '../assets/welcome-ski.jpg'
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
import { Avatar, BottomCTA, Button, Card, Segmented, TopBar, Sheet } from '../ui'
import { WorldAdvanced } from './worldAdvanced'
import { worldArt } from '../app/worldPrototype'
import { QuickChannelForm, QuickWorldForm, DramaPicker } from './worldQuick'
import type { AgeBand } from '../app/ageBand'
import { avatarPersonalization, setAvatarPersonalization } from '../app/avatarPersonalization'

export function ChooseWorld() {
  const nav = useNav(); const { player, setPlayer } = useStore()
  const [owned, setOwned] = useState<WorldDocument[]>(() => api.ownedWorldsSnapshot() ?? [])
  const [category, setCategory] = useState('Все')
  const [unlockInfo, setUnlockInfo] = useState(false)
  const [error, setError] = useState('')
  const [pending, setPending] = useState<boolean | null>(null)
  useEffect(() => { let alive = true; if (player) api.ownedWorlds().then(found => { if (alive) setOwned(found) }).catch(() => { if (alive) setError('Не получилось загрузить твои миры.') }); return () => { alive = false } }, [player?.id]) // eslint-disable-line react-hooks/exhaustive-deps
  function startOwn(previewOnly = false, confirmed = false) {
    selectDraftOwner(player?.id ?? 'onboarding')
    const previous = getWorldDraft()
    if (!confirmed && previous && (previous.definition.templateId !== null || previous.editingId)) { setPending(previewOnly); return }
    const restored = savedWorldDraft('custom')
    if (restored && !!restored.previewOnly === previewOnly) { setWorldDraft(restored); nav.push('05-7b_own_world', {}); return }
    const definition = blankWorld()
    setWorldDraft({ definition, heroOrder: [], advanced: !previewOnly, previewOnly, address: player?.address ?? 'nick', group: player?.ageBand ? (player.ageBand === '14-17' ? '14-17' : '18+') : (nav.params.group as '14-17' | '18+') ?? '14-17', ageBand: player?.ageBand ?? nav.params.ageBand as AgeBand | undefined, personalizeAvatar: player ? avatarPersonalization(player.id) : true })
    nav.push('05-7b_own_world', {})
  }
  const currentWorld = player ? owned.find(world => world.id === player.worldInstanceId) ?? { ...worldDocumentOf(player.world), id: player.worldInstanceId ?? player.world, name: player.name, avatar: player.handle, subscribers: player.subs } : undefined
  const otherWorlds = owned.filter(world => world.id !== currentWorld?.id)
  const resume = async (world: WorldDocument) => { try { const next = await api.me(world.id); setPlayer(next); nav.push('03-5_stage') } catch { setError('Не получилось открыть мир.') } }
  return <div className="screen no-tab"><header className="catalog-header"><span className="welcome-brand">реки.</span>{player && <button aria-label="Мой профиль" onClick={() => nav.push('05-5_collection')}><Avatar seed={player.handle} size={36} /></button>}</header>
    <div className="pad world-catalog-page worlds-home">
      <button className="own-world-prototype" onClick={() => player?.hitOwnWorldMilestone ? startOwn() : setUnlockInfo(true)}>
        <span className="own-prototype-art" aria-hidden="true"><span className="own-prototype-photo"><img src={welcomeSki} alt="" /><span>свои люди · новые истории</span></span><span className="own-prototype-sticker">а что, если…</span></span>
        <strong>Воплоти свои<br />фантазии</strong>
        <span className="own-prototype-value"><span className="own-prototype-mic" aria-hidden="true"><svg viewBox="0 0 24 24"><path d="m4 16 12-12 4 4L8 20H4v-4Zm10-10 4 4" /></svg></span><span>Придумай свой мир.<br />Ты и твои собеседники.</span></span>
        {player?.hitOwnWorldMilestone ? <span className="own-prototype-cta">Создать свой мир ↗</span> : <><span className="own-prototype-locked"><b>Свой мир откроется</b><span><strong>{Math.min(500, Math.max(player?.subs ?? 0, ...owned.map(world => world.subscribers)))}</strong> / 500</span></span><span className="own-prototype-track" role="progressbar" aria-label="Подписчики для открытия своего мира" aria-valuemin={0} aria-valuemax={500} aria-valuenow={Math.min(500, Math.max(player?.subs ?? 0, ...owned.map(world => world.subscribers)))}>{Array.from({ length: 10 }, (_, index) => <i key={index} style={{ background: `linear-gradient(to right, #17221b ${Math.max(0, Math.min(100, (Math.max(player?.subs ?? 0, ...owned.map(world => world.subscribers)) - index * 50) * 2))}%, #17221b22 0)` }} />)}</span><span className="own-prototype-alternative">или пригласи первого друга ↗</span></>}
      </button>
      {unlockInfo && <Sheet onClose={() => setUnlockInfo(false)}><h2>Как открыть свой мир</h2><p>Набери 500 подписчиков в одном канале или пригласи друга — мир откроется после его первого поста.</p><p className="small muted">Голосовое создание пока недоступно. Сейчас можно настроить мир текстом.</p>{player && <Button onClick={() => { setUnlockInfo(false); nav.push('04-3_invite') }}>Пригласить друга →</Button>}<Button secondary onClick={() => setUnlockInfo(false)}>Понятно</Button></Sheet>}

      {getWorldDraft() && <Button secondary onClick={() => nav.push(getWorldDraft()?.definition.templateId === null ? '05-7b_own_world' : '01-4_channel')}>Продолжить настройку</Button>}
      {currentWorld && <><h2>Твои миры</h2><button className="world-resume card" onClick={() => resume(currentWorld)}><Avatar seed={currentWorld.avatar} avatarStyle={currentWorld.avatarStyle} profile={player ? { address: player.address, ageBand: player.ageBand, personalizeAvatar: avatarPersonalization(player.id) } : undefined} size={48} /><div><b>{currentWorld.name}</b><p className="small muted">{currentWorld.emoji} {currentWorld.title} · {currentWorld.subscribers} подписчиков</p><span>Продолжить →</span></div></button></>}
      {otherWorlds.length > 0 && <><h2>Другие твои миры</h2><div className="owned-stories">{otherWorlds.map(world => <button className="continue-world card" key={world.id} onClick={() => resume(world)}><Avatar seed={world.avatar} avatarStyle={world.avatarStyle} size={36} /><div><b>{world.name}</b><div className="small muted">{world.emoji} {world.title} · {world.subscribers} подписчиков</div></div><span>Играть →</span></button>)}</div></>}
      <div className="catalog-heading"><h1>Новый мир.<br /><span>Новая компания.</span></h1><p className="muted">Выбери мир, а затем — имя и роль.</p></div><nav className="catalog-categories" aria-label="Категории новых миров">{['Все', 'Тайны', 'Романтика', 'Фэнтези', 'Айти'].map(value => <button key={value} aria-pressed={category === value} onClick={() => setCategory(value)}>{value}</button>)}</nav><div className="world-catalog">{[...worlds].filter(world => category === 'Все' || (category === 'Тайны' || category === 'Романтика' ? world.id === 'b3a1a522-0263-4796-a197-57a8731fc291' : category === 'Айти' ? world.id === '5720946a-ea7c-4a10-9c50-51ce3deefc53' : ['aec9494b-4118-4525-8458-e85d759c8dd9', '8c02a7fb-3b55-4eb0-b957-1b4869ed9834'].includes(world.id))).sort((a, b) => ['b3a1a522-0263-4796-a197-57a8731fc291', 'aec9494b-4118-4525-8458-e85d759c8dd9', '8c02a7fb-3b55-4eb0-b957-1b4869ed9834', '5720946a-ea7c-4a10-9c50-51ce3deefc53'].indexOf(a.id) - ['b3a1a522-0263-4796-a197-57a8731fc291', 'aec9494b-4118-4525-8458-e85d759c8dd9', '8c02a7fb-3b55-4eb0-b957-1b4869ed9834', '5720946a-ea7c-4a10-9c50-51ce3deefc53'].indexOf(b.id)).map((world, index) => { const art = worldArt[world.id]; const definition = worldDefinitionOf(world.id); return <button key={world.id} className="world-cover-card" onClick={() => nav.push('01-3b_world_overview', { ...nav.params, world: world.id })} aria-label={`Обзор мира ${world.title}`}>
        {art && <div className="world-cover-image"><WorldCover world={world.id} catalog priority={index === 0} /><span className="world-mode">Свободное общение</span><span className="world-genre">{art.genre}</span></div>}
        <div className="world-cover-copy"><div className="row"><b>{world.title}</b><span aria-hidden="true">↗</span></div><p>{art?.pitch ?? world.summary}</p><div className="world-roster">{definition.stars.slice(0, 3).map(star => <Avatar key={star.id} seed={star.avatarSeed} avatarStyle={definition.avatarStyle} mode="default" size={26} />)}<span>Выбрать мир →</span></div></div>
      </button> })}</div>
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
  if (quick) return <div className="screen no-tab"><TopBar title="Настроение истории" onBack={saving ? undefined : nav.back} /><div className="pad"><DramaPicker definition={d} beforeOptions={<div className="drama-world-edit"><Button secondary onClick={() => nav.push('05-12_world_advanced')}>Редактировать мир</Button><p className="small muted">Мир можно будет отредактировать позже</p></div>} />
    <div className="quick-summary"><Avatar seed={d.avatar} avatarStyle={d.avatarStyle} profile={{ address: draft.address, ageBand: draft.ageBand, personalizeAvatar: draft.personalizeAvatar }} /><div><b>{d.name}</b><div className="small muted">{d.title} · {d.role}</div></div></div>
    {draft.previewOnly && <p className="small prototype-note">DEV-прототип: дальше — пример ленты, без создания мира в базе.</p>}
    {error && <p role="alert">{error}</p>}{uncertain && <Button onClick={() => nav.replace('01-3_world')}>Проверить список миров</Button>}
  </div><BottomCTA main={saving ? 'Сохраняем…' : draft.previewOnly ? 'Посмотреть свою историю' : draft.editingId ? 'Сохранить изменения' : 'Начать историю'} disabled={saving || uncertain || !d.voice.trim() || d.stars.length === 0} onMain={save} /></div>
  return <WorldAdvanced draft={draft} saving={saving} error={error} uncertain={uncertain} onSave={save} onCheck={() => nav.replace('01-3_world')} />
}
