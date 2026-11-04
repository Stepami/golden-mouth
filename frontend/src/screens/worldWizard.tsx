import './entry-parity.css'
import welcomeSki from '../assets/welcome-ski.jpg'
import { WorldSwitchDialog } from '../ui/WorldSwitchDialog'
import { WorldCover } from '../ui/WorldCover'
import { useEffect, useState } from 'react'
import { api, ApiError } from '../api/client'
import { worldDefinitionOf, worldDocumentOf, worlds, optionalWorldTemplate } from '../api/worldContent'
import type { WorldDefinition, WorldDocument } from '../api/types'
import { useNav } from '../app/nav'
import { useStore } from '../app/store'
import { blankWorld, clearWorldDrafts, getWorldDraft, savedWorldDraft, selectDraftOwner, setWorldDraft, updateWorldDraft, useWorldDraft } from '../app/worldDraft'
import { pendingReferral, clearReferral } from '../app/referral'
import { Avatar, BottomCTA, Button, Card, Segmented, TopBar, Sheet } from '../ui'
import { WorldAdvanced } from './worldAdvanced'
import { dramaOptions, worldArt } from '../app/worldPrototype'
import { QuickChannelForm, QuickWorldForm } from './worldQuick'
import type { AgeBand } from '../app/ageBand'
import { displayHandle } from '../app/handle'
import { AvatarPersonalization } from '../ui/AvatarPersonalization'
import { avatarPersonalization, setAvatarPersonalization } from '../app/avatarPersonalization'

export function ChooseWorld() {
  const nav = useNav(); const { player, playerState, setPlayer, refresh } = useStore()
  const [ownership, setOwnership] = useState<{ owner: string; worlds: WorldDocument[]; loaded: boolean }>({ owner: '', worlds: [], loaded: false })
  const owned = player && ownership.owner === player.id ? ownership.worlds : []
  const ownedLoaded = !!player && ownership.owner === player.id && ownership.loaded
  const [category, setCategory] = useState('Все')
  const [unlockInfo, setUnlockInfo] = useState(false)
  const [error, setError] = useState('')
  const [ownedFailed, setOwnedFailed] = useState(false)
  const [pending, setPending] = useState<boolean | null>(null)
  useEffect(() => {
    let alive = true
    setOwnedFailed(false)
    if (player) api.ownedWorlds().then(found => {
      if (alive) { setOwnership({ owner: player.id, worlds: found, loaded: true }); setError('') }
    }).catch(() => { if (alive) { setOwnedFailed(true); setError('Не получилось загрузить твои миры. Показан прогресс текущего мира.') } })
    return () => { alive = false }
  }, [player?.id]) // eslint-disable-line react-hooks/exhaustive-deps

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
  const currentWorld = player ? { ...(owned.find(world => world.id === player.worldInstanceId) ?? worldDocumentOf(player.world)), id: player.worldInstanceId ?? player.world, name: player.name, avatar: player.handle, subscribers: player.subs } : undefined
  const bestWorld = player ? [currentWorld!, ...owned.filter(world => world.id !== currentWorld!.id)].reduce((best, world) => world.subscribers > best.subscribers ? world : best) : undefined
  const bestSubscribers = bestWorld?.subscribers
  const ownProgress = bestSubscribers === undefined ? undefined : Math.min(500, Math.max(0, bestSubscribers))
  const openProgress = async () => {
    if (!bestWorld || !player) return
    try {
      if (bestWorld.id !== player.worldInstanceId) setPlayer(await api.me(bestWorld.id))
      setUnlockInfo(false); nav.push('05-13_progress')
    } catch { setError('Не получилось открыть прогресс мира. Попробуй ещё раз.') }
  }
  const otherWorlds = owned.filter(world => world.id !== currentWorld?.id)
  const resume = async (world: WorldDocument) => { try { const next = await api.me(world.id); setPlayer(next); nav.push('03-5_stage') } catch { setError('Не получилось открыть мир.') } }
  const savedWorldCard = (world: WorldDocument) => {
    const art = worldArt[world.id] ?? worldArt[optionalWorldTemplate(world)?.id ?? '']
    return <button className="continue-world card" key={world.id} onClick={() => resume(world)} aria-current={world.id === currentWorld?.id ? 'true' : undefined}>{art ? <img className="saved-world-cover" src={art.smallCover} alt="" /> : <Avatar seed={world.avatar} avatarStyle={world.avatarStyle} size={44} />}<div><strong>{world.title}</strong><small>{world.name} · {world.role} · 👥 {world.subscribers}</small>{world.id === currentWorld?.id && <em>Сейчас здесь</em>}</div><span aria-hidden="true">→</span></button>
  }
  return <div className="screen no-tab"><header className="catalog-header"><span className="welcome-brand">реки.</span>{player && <button aria-label="Мой профиль" onClick={() => nav.push('05-5_collection')}><Avatar seed={player.handle} size={36} /></button>}</header>
    <div className="pad world-catalog-page worlds-home">
      {playerState === 'unauthorized' && <p className="small muted" role="status">Можно посмотреть миры. Для создания и сохранения открой Реки в Telegram.</p>}
      <button className="own-world-prototype" onClick={() => player?.hitOwnWorldMilestone ? startOwn() : setUnlockInfo(true)}>
        <span className="own-prototype-art" aria-hidden="true"><span className="own-prototype-photo"><img src={welcomeSki} alt="" /><span>свои люди · новые истории</span></span><span className="own-prototype-sticker">а что, если…</span></span>
        <strong>Воплоти свои<br />фантазии</strong>
        <span className="own-prototype-value"><span className="own-prototype-mic" aria-hidden="true"><svg viewBox="0 0 24 24"><path d="m4 16 12-12 4 4L8 20H4v-4Zm10-10 4 4" /></svg></span><span>Придумай свой мир.<br />Ты и твои собеседники.</span></span>
        {player?.hitOwnWorldMilestone ? <span className="own-prototype-cta">Создать свой мир ↗</span> : <><span className="own-prototype-locked"><b>Свой мир откроется</b><span>{ownProgress === undefined ? 'После первого мира' : <><strong>{bestSubscribers}</strong> / 500</>}</span></span>{ownProgress !== undefined && <span className="own-prototype-track" role="progressbar" aria-label="Подписчики для открытия своего мира" aria-valuemin={0} aria-valuemax={500} aria-valuenow={ownProgress}>{Array.from({ length: 10 }, (_, index) => <i key={index} style={{ background: `linear-gradient(to right, #17221b ${Math.max(0, Math.min(100, (ownProgress - index * 50) * 2))}%, #17221b22 0)` }} />)}</span>}<span className="own-prototype-alternative">или пригласи первого друга ↗</span></>}

      </button>
      {player?.hitOwnWorldMilestone && <button className="text-button" onClick={() => setUnlockInfo(true)}>О своём мире →</button>}
      {unlockInfo && <Sheet className="own-unlock-modal entry-own-sheet" label="Как открыть свой мир" onClose={() => setUnlockInfo(false)}>
        <div className="own-unlock-head"><h2>Свой мир — из твоей идеи</h2><button className="icon-button" aria-label="Закрыть" onClick={() => setUnlockInfo(false)}>×</button></div>
        <div className="own-unlock-sheet">
          <p className="own-unlock-lead">Расскажи идею — создай место, себя и собеседников. Проверь детали перед созданием мира. Сейчас описание вводится текстом; диктовка пока недоступна.</p>
          <div className="unlock-compact">
            <strong>{player?.hitOwnWorldMilestone ? '✓ Можно создавать' : 'Как открыть'}</strong>
            <div className="unlock-followers"><span>Набери 500 подписчиков<br />в одном мире</span><b>{bestSubscribers === undefined ? '—' : bestSubscribers}<small>/ 500</small></b></div>
            {ownProgress !== undefined ? <><progress max={500} value={ownProgress} aria-label={`${bestSubscribers} из 500 подписчиков`} /><small className="unlock-world">{ownedLoaded ? 'Лучший мир' : 'Текущий мир'}: {bestWorld?.title}{!ownedLoaded && (ownedFailed ? ' · остальные данные недоступны' : ' · загружаем остальные миры')}</small></> : <p className="unlock-no-data">{playerState === 'loading' ? 'Загружаем профиль…' : playerState === 'failed' ? 'Не удалось загрузить профиль. Счётчик пока недоступен.' : playerState === 'unauthorized' ? 'Для сохранения прогресса открой Реки в Telegram.' : 'Прогресс появится после создания первого мира.'}</p>}
            {player?.hitOwnWorldMilestone && <p className="unlock-confirmed">Доступ подтверждён сервером.</p>}
            <div className="unlock-friend"><span>или</span><p>Пригласи друга.<br />Доступ откроется после его первого поста.</p></div>
          </div>
          <Button disabled={playerState === 'loading'} onClick={() => {
            if (playerState === 'failed') { void refresh(); return }
            setUnlockInfo(false)
            if (player?.hitOwnWorldMilestone) startOwn()
            else if (player) nav.push('04-3_invite')
            else document.querySelector('.catalog-heading')?.scrollIntoView({ block: 'start' })
          }}>{player?.hitOwnWorldMilestone ? 'Создать свой мир' : player ? 'Пригласить друга' : playerState === 'failed' ? 'Повторить загрузку профиля' : playerState === 'loading' ? 'Загружаем…' : 'Выбрать первый мир'}</Button>
          <button className="magic-progress" onClick={() => player ? void openProgress() : setUnlockInfo(false)}>{player ? 'К прогрессу →' : 'Вернуться к мирам →'}</button>
          {error && <p role="alert">{error}</p>}
        </div>
      </Sheet>}


      {getWorldDraft() && <Button secondary onClick={() => nav.push(getWorldDraft()?.definition.templateId === null ? '05-7b_own_world' : '01-4_channel')}>Продолжить настройку</Button>}
      {currentWorld && <section className="saved-worlds"><h2>Твои миры</h2><div className="owned-stories">{[currentWorld, ...otherWorlds].slice(0,2).map(savedWorldCard)}</div>{otherWorlds.length > 1 && <details className="saved-worlds-more" key={player?.id}><summary><span className="show-more">Показать все миры · {otherWorlds.length + 1}</span><span className="show-less">Свернуть</span></summary><div className="owned-stories">{otherWorlds.slice(1).map(savedWorldCard)}</div></details>}</section>}
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
  return <Ready>{draft => draft.advanced ? <StarsForm draft={draft} /> : draft.definition.templateId ? <PresetEntryForm draft={draft} /> : <QuickChannelForm draft={draft} />}</Ready>
}
export function DescriptionSetup() { return <Ready>{draft => draft.advanced ? <StarsForm draft={draft} /> : <QuickWorldForm draft={draft} />}</Ready> }
export function VoiceSetup() { return <Ready>{draft => <StarsForm draft={draft} quick={!draft.advanced} />}</Ready> }
export function AdvancedSetup() { return <Ready>{draft => <StarsForm draft={draft} />}</Ready> }
export function StarsSetup() { return <Ready>{draft => <StarsForm draft={draft} />}</Ready> }
type EntryDraft = NonNullable<ReturnType<typeof useWorldDraft>> & { entryChoice?: string }

/** Keep the existing NPC-as-player payload: restore the previous NPC before changing roles. */
function PresetEntryForm({ draft }: { draft: EntryDraft }) {
  const nav = useNav(); const d = draft.definition
  const { save, saving, error, uncertain } = useWorldSave(draft)
  const choice = draft.entryChoice ?? draft.persona?.star.id
  const selected = draft.persona
  const heroes = [...d.stars, ...(selected ? [selected.star] : [])].sort((a, b) => (draft.heroOrder?.indexOf(a.id) ?? 0) - (draft.heroOrder?.indexOf(b.id) ?? 0))
  const choose = (star?: WorldDefinition['stars'][number]) => {
    const previous = draft.persona
    const restored = { ...d, stars: [...d.stars, ...(previous ? [previous.star] : [])], description: previous?.description ?? d.description, name: previous?.name ?? d.name, avatar: previous?.avatar ?? d.avatar, role: previous?.role ?? d.role }
    const next: EntryDraft = { ...draft, entryChoice: star?.id ?? 'custom', heroOrder: draft.heroOrder ?? heroes.map(hero => hero.id), persona: undefined, definition: restored }
    if (star) {
      next.persona = { star, description: restored.description, name: restored.name, avatar: restored.avatar, role: restored.role }
      next.definition = { ...restored, name: star.name, avatar: star.avatarSeed, role: star.role, description: `${restored.description}\nПерсона игрока: ${star.name}. ${star.about}`.slice(0, 2000), stars: restored.stars.filter(value => value.id !== star.id) }
    }
    setWorldDraft(next)
  }
  return <><ChooseWorld /><Sheet className="entry-parity entry-profile" title="Твой профиль в мире" onClose={saving ? undefined : nav.back}><form className="entry-content" onSubmit={event => { event.preventDefault(); if (choice && d.name.trim() && d.role.trim() && d.avatar.trim() && d.voice.trim() && d.stars.length) void save() }}>
    <div className="entry-world"><WorldCover world={d.templateId!} /><div><h3>{d.title}</h3><p>{d.summary}</p></div></div>
    <fieldset className="entry-characters" disabled={saving}><legend>Кем войдёшь в мир?</legend>
      {heroes.map(star => <label className="entry-character" key={star.id}><input type="radio" name="character" value={star.id} checked={choice === star.id} onChange={() => choose(star)} /><Avatar seed={star.avatarSeed} world={d.templateId!} avatarStyle={d.avatarStyle} mode="default" size={40} /><span><strong>{star.name}</strong><small>{star.role}</small></span></label>)}
      <label className="entry-character"><input type="radio" name="character" value="custom" checked={choice === 'custom'} onChange={() => choose()} /><span className="entry-anonymous" aria-hidden="true">✦</span><span><strong>Свой персонаж</strong><small>Выбрать имя и роль</small></span></label>
    </fieldset>
    {choice === 'custom' && <div className="entry-custom"><div className="entry-identity"><Avatar seed={d.avatar} avatarStyle={d.avatarStyle} size={40} /><div><strong>Кем ты будешь здесь?</strong><p>Выбери имя и роль. Портрет можно настроить позже.</p></div></div>
      <label>Как тебя зовут здесь?<input required aria-label="Имя персонажа" maxLength={80} value={d.name} onChange={event => updateWorldDraft({ name: event.target.value })} /></label>
      <label>Ник в ленте<input required aria-label="Ник в ленте" maxLength={80} value={d.avatar} onChange={event => updateWorldDraft({ avatar: event.target.value })} /></label>
      <details className="entry-details"><summary>Кто ты в этом мире · {d.role}</summary><fieldset className="entry-roles"><legend className="sr-only">Роль персонажа</legend>{d.roles.map(role => <label key={role}><input type="radio" name="role" checked={d.role === role} onChange={() => updateWorldDraft({ role })} />{role}</label>)}</fieldset><label>Своя роль<input required aria-label="Кто ты в этом мире" maxLength={80} value={d.role} onChange={event => updateWorldDraft({ role: event.target.value })} /></label></details>
    </div>}
    {choice && <details className="entry-details"><summary>Обращение и портрет</summary><Segmented value={draft.address} onChange={address => setWorldDraft({ ...draft, address })} options={[{ id: 'she', label: 'она' }, { id: 'he', label: 'он' }, { id: 'nick', label: 'по нику' }]} /><p className="small muted">{displayHandle(d.avatar)}</p><AvatarPersonalization enabled={draft.personalizeAvatar !== false} onChange={personalizeAvatar => setWorldDraft({ ...draft, personalizeAvatar })} /></details>}
    <details className="entry-details"><summary>Настроение и голос · необязательно</summary><EntryMood definition={d} /><label>Как ты пишешь<input aria-label="Голос персонажа" maxLength={200} value={d.voice} onChange={event => updateWorldDraft({ voice: event.target.value })} /></label></details>
    {error && <p role="alert">{error}</p>}{uncertain && <Button onClick={() => nav.replace('01-3_world')}>Проверить список миров</Button>}
    <footer><button className="btn" disabled={saving || uncertain || !choice || !d.name.trim() || !d.role.trim() || !d.avatar.trim() || !d.voice.trim() || !d.stars.length} type="submit">{saving ? 'Сохраняем…' : choice ? `Войти как ${d.name || '…'} →` : 'Выбери персонажа'}</button></footer>
  </form></Sheet></>
}

function EntryMood({ definition: d }: { definition: WorldDefinition }) {
  return <><p className="entry-lead">Какими будут разговоры и приключения? Эти настройки можно изменить позже.</p>
    <fieldset className="entry-characters"><legend>Градус драмы</legend>{dramaOptions.map(option => <label key={option.id} className="entry-character"><input type="radio" name="drama" checked={d.drama === option.id} onChange={() => updateWorldDraft({ drama: option.id })} /><span className="entry-anonymous" aria-hidden="true">{option.icon}</span><span><strong>{option.title}</strong><small>{option.description}</small></span></label>)}</fieldset>
    <fieldset className="entry-difficulty"><legend>Сложность</legend>{([{ id: 'easy', title: 'Легко' }, { id: 'normal', title: 'Средне' }, { id: 'hard', title: 'Сложно' }] as const).map(option => <label key={option.id}><input type="radio" name="difficulty" checked={d.difficulty === option.id} onChange={() => updateWorldDraft({ difficulty: option.id })} />{option.title}</label>)}</fieldset>
  </>
}

function useWorldSave(draft: NonNullable<ReturnType<typeof useWorldDraft>>) {
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
      setWorldDraft(null); setPlayer(player); nav.replace(!draft.editingId && d.templateId ? '01-6_home' : '03-5_stage')
    } catch (cause) {
      if (!draft.editingId && !(cause instanceof ApiError)) { setUncertain(true); setError('Не удалось подтвердить создание. Сначала проверь список миров: повтор может создать ещё один экземпляр.') }
      else setError(cause instanceof ApiError && cause.status === 409 ? 'Мир изменился или создание с нуля пока недоступно. Твои изменения сохранены в черновике.' : cause instanceof ApiError && cause.status === 503 ? 'Проверка истории временно недоступна. Настройки сохранены — попробуй начать ещё раз чуть позже.' : 'Не получилось сохранить мир. Проверь поля и попробуй снова.')
    } finally { setSaving(false) }
  }
  return { save, saving, error, uncertain }
}

function StarsForm({ draft, quick = false }: { draft: NonNullable<ReturnType<typeof useWorldDraft>>; quick?: boolean }) {
  const nav = useNav(); const d = draft.definition
  const { save, saving, error, uncertain } = useWorldSave(draft)
  if (quick) return <><ChooseWorld /><Sheet className="entry-parity entry-mood" title="Настроение истории" onClose={saving ? undefined : nav.back}><div className="entry-content"><EntryMood definition={d} />
    <div className="entry-summary"><Avatar seed={d.avatar} avatarStyle={d.avatarStyle} profile={{ address: draft.address, ageBand: draft.ageBand, personalizeAvatar: draft.personalizeAvatar }} size={40} /><div><b>{d.name}</b><p>{d.title} · {d.role}</p></div></div>
    <details className="entry-details"><summary>Изменить мир</summary><Button secondary onClick={() => nav.push('05-12_world_advanced')}>Расширенные настройки</Button><p className="small muted">Мир можно отредактировать и позже.</p></details>
    {draft.previewOnly && <p className="small prototype-note">Пример истории без создания мира.</p>}
    {error && <p role="alert">{error}</p>}{uncertain && <Button onClick={() => nav.replace('01-3_world')}>Проверить список миров</Button>}
    <footer><button className="btn" disabled={saving || uncertain || !d.voice.trim() || d.stars.length === 0} onClick={save}>{saving ? 'Сохраняем…' : draft.previewOnly ? 'Посмотреть свою историю' : draft.editingId ? 'Сохранить изменения' : 'Начать историю →'}</button></footer>
  </div></Sheet></>
  return <WorldAdvanced draft={draft} saving={saving} error={error} uncertain={uncertain} onSave={save} onCheck={() => nav.replace('01-3_world')} />
}
