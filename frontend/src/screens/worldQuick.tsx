import { useGuestSession } from '../app/landing/guestSession'
import { readGuestHandoff } from '../app/landing/guestHandoff'
import { tap } from '../app/analytics'
import { recordEntryStep } from '../app/landing/openobserve'
import { clearLandingWorld } from '../app/landing/experiment'
import { useState, type ReactNode } from 'react'
import { useStore } from '../app/store'
import { avatarPersonalization } from '../app/avatarPersonalization'
import { displayHandle } from '../app/handle'
import type { AgeBand } from '../app/ageBand'
import { worldDefinitionOf, worlds } from '../api/worldContent'
import { api } from '../api/client'
import { WorldSwitchDialog } from '../ui/WorldSwitchDialog'
import { presetGoalFor } from '../app/presetGoals'
import { WorldCover } from '../ui/WorldCover'
import { useNav } from '../app/nav'
import { getWorldDraft, savedWorldDraft, selectDraftOwner, setWorldDraft, updateWorldDraft, useWorldDraft, type WorldDraft } from '../app/worldDraft'
import { dramaOptions, prototypeWorld, worldArt } from '../app/worldPrototype'
import { Avatar, BottomCTA, Button, Card, ExpandableText, Segmented, TopBar } from '../ui'
import { TextField } from '../ui/TextField'
import { AvatarPersonalization } from '../ui/AvatarPersonalization'
import type { Address, WorldDefinition, WorldId } from '../api/types'


export function withPersona(draft: WorldDraft, star?: WorldDefinition['stars'][number]): WorldDraft {
  const d = draft.definition; const previous = draft.persona
  const heroOrder = draft.heroOrder ?? [...d.stars, ...(previous ? [previous.star] : [])].map(value => value.id)
  const restored = { ...d, stars: [...d.stars, ...(previous ? [previous.star] : [])], description: previous?.description ?? d.description, name: previous?.name ?? d.name, avatar: previous?.avatar ?? d.avatar, role: previous?.role ?? d.role }
  if (!star) return { ...draft, heroOrder, personaChosen: true, persona: undefined, definition: restored }
  return { ...draft, heroOrder, personaChosen: true, persona: { star, description: restored.description, name: restored.name, avatar: restored.avatar, role: restored.role }, definition: { ...restored, name: star.name, avatar: star.avatarSeed, role: star.role, description: `${restored.description}\nПерсона игрока: ${star.name}. ${star.about}`.slice(0, 2000), stars: restored.stars.filter(value => value.id !== star.id) } }
}

function refreshHogwartsDraft(draft: WorldDraft, current: WorldDefinition): WorldDraft {
  if (draft.editingId || current.templateId !== 'aec9494b-4118-4525-8458-e85d759c8dd9') return draft
  const base = withPersona(draft).definition
  const aliases = current.description.replaceAll('Гарри', 'Гоша').replaceAll('Гермиона', 'Георгина').replaceAll('Рон', 'Роман')
  if (base.description !== aliases && base.description !== current.description) return draft
  const rename = (text: string) => text.replaceAll('Гоша Гончаров', 'Гарри Поттер').replaceAll('Георгина Гречихина', 'Гермиона Грейнджер').replaceAll('Роман Ласкин', 'Рон Уизли').replaceAll('Гоша', 'Гарри').replaceAll('Гоши', 'Гарри').replaceAll('Гоше', 'Гарри').replaceAll('Георгина', 'Гермиона').replaceAll('Георгины', 'Гермионы').replaceAll('Георгину', 'Гермиону').replaceAll('Роман', 'Рон')
  const refreshed: WorldDraft = { ...draft, persona: undefined, definition: { ...base, role: base.role === 'Автор хроники' ? current.role : base.role, description: rename(base.description), firstContext: rename(base.firstContext), rules: base.rules.map(rule => ({ ...rule, text: rename(rule.text) })), demoPosts: base.demoPosts.map(rename), stars: base.stars.map(star => { const latest = current.stars.find(value => value.id === star.id); return latest ? { ...star, name: latest.name, role: latest.role, about: latest.about, voice: latest.voice } : star }) } }
  if (!draft.persona) return refreshed
  const star = refreshed.definition.stars.find(value => value.id === draft.persona!.star.id)
  if (!star) return refreshed
  const selected = withPersona(refreshed, star)
  return { ...selected, definition: { ...selected.definition, avatar: draft.definition.avatar, name: draft.definition.name === draft.persona.star.name ? star.name : draft.definition.name } }
}

export function WorldOverview() {
  const draft = useWorldDraft(); const nav = useNav(); const player = useStore()?.player
  const guest = useGuestSession()
  const [confirmSwitch, setConfirmSwitch] = useState(false)
  const world = guest?.world.templateId ?? (nav.params.world as WorldId | undefined) ?? draft?.definition.templateId
  const catalog = () => { clearLandingWorld(); nav.replace('01-3_world', { group: nav.params.group, ageBand: nav.params.ageBand, birthYear: nav.params.birthYear }) }
  const back = guest ? undefined : () => nav.params.fromLanding ? catalog() : nav.back()
  if (!world || !guest && !worlds.some(template => template.id === world)) return <div className="screen no-tab world-overview"><TopBar title="Обзор мира" onBack={back} /><div className="pad"><p>Этот мир сейчас недоступен.</p><Button onClick={catalog}>Выбрать мир</Button></div></div>
  const d = guest?.world ?? worldDefinitionOf(world); const art = worldArt[d.templateId ?? '']; const selectedRole = draft?.definition.templateId === world && (draft.personaChosen || draft.persona) ? d.stars.find(star => star.id === draft.persona?.star.id)?.name : d.stars[0]?.name; const goal = guest?.goal ?? presetGoalFor(d, selectedRole)
  const begin = (confirmed = false) => {
    if (guest) { nav.push('01-4_channel'); return }
    selectDraftOwner(player?.id ?? api.playerId() ?? 'onboarding')
    const previous = getWorldDraft()
    if (!confirmed && previous && (previous.definition.templateId !== world || previous.editingId)) { setConfirmSwitch(true); return }
    const restored = savedWorldDraft(world)
    const profile = player ?? api.onboardingProfile()
    let next: WorldDraft = restored && !restored.previewOnly ? { ...restored, ...(nav.params.ageBand ? { group: nav.params.group as '14-17' | '18+', ageBand: nav.params.ageBand as AgeBand, birthYear: nav.params.birthYear as number } : {}) } : {
      definition: d, heroOrder: d.stars.map(star => star.id), address: profile?.address ?? 'nick',
      group: profile ? (profile.ageBand === '14-17' ? '14-17' : '18+') : (nav.params.group as '14-17' | '18+') ?? '14-17',
      ageBand: profile?.ageBand ?? nav.params.ageBand as AgeBand | undefined,
      birthYear: nav.params.birthYear as number | undefined,
      personalizeAvatar: player ? avatarPersonalization(player.id) : true,
    }
    next = refreshHogwartsDraft(next, d)
    if (!next.persona && !next.personaChosen && next.definition.stars.length > 1) {
      const customProfile = restored && (next.definition.name !== d.name || next.definition.avatar !== d.avatar)
      const guest = readGuestHandoff()
      const guestHero = guest?.worldId === world ? next.definition.stars.find(star => star.id === guest.hero) : undefined
      next = customProfile ? { ...next, personaChosen: true } : withPersona(next, guestHero ?? next.definition.stars[0])
    }
    setWorldDraft(next)
    setConfirmSwitch(false)
    recordEntryStep('landing_world_selected')
    nav.push('01-4_channel')
  }
  return <div className="screen no-tab world-overview"><TopBar title="Обзор мира" onBack={back} />
    {art && <WorldCover world={world} className="world-hero" priority alt={`Обложка мира «${d.title}»`} />}
    <div className="pad"><div className="small eyebrow">{art?.genre}</div><h1>{d.emoji} {d.title}</h1><p className="muted">{art?.pitch ?? d.description}</p>
      {goal && <Card><div className="small eyebrow">Главная цель</div><p>{goal.goal}</p></Card>}
      <h2>Кого встретишь</h2><div className="world-people">{d.stars.map(star => <div key={star.id}><Avatar seed={star.avatarSeed} world={d.templateId ?? undefined} avatarStyle={d.avatarStyle} mode="default" size={44} /><b>{star.name}</b><span className="small muted">{star.role}</span></div>)}</div>

    </div><BottomCTA main="Выбрать персонажа" onMain={() => begin()} />
    {confirmSwitch && <WorldSwitchDialog title={draft?.definition.title ?? ''} onConfirm={() => begin(true)} onCancel={() => setConfirmSwitch(false)} />}</div>
}
export function QuickWorldForm({ draft }: { draft: WorldDraft }) {
  const nav = useNav(); const d = draft.definition
  return <div className="screen no-tab"><TopBar title={draft.editingId ? 'Твой мир' : 'Создай свой мир · 1 / 3'} onBack={nav.back} /><div className="pad"><h1>Какая здесь история?</h1><p className="muted">Хватит названия и пары предложений. Где всё происходит, кто здесь живёт и что необычного в этом мире?</p>
    {draft.reason && <Card>{draft.reason}</Card>}
    <label className="label">Название мира<input aria-label="Название мира" value={d.title} maxLength={120} placeholder="Например: Город над облаками" onChange={e => updateWorldDraft({ title: e.target.value })} /></label>
    <TextField label="Расскажи о своём мире" value={d.description} onChange={description => updateWorldDraft({ description })} rows={5} placeholder="Город парит над морем. Здесь письма доставляют птицы, а жители ищут пропавший остров…" />
    <details className="card"><summary>Нужна идея?</summary><p className="small muted">{d.description.length ? 'Очисти описание, чтобы выбрать готовую идею.' : 'Выбери основу и допиши свои детали.'}</p>{['Город над облаками, где жители ищут пропавший остров и оставляют находки в общей ленте.', 'Космическая станция, на которой каждое утро появляется сообщение из будущего.'].map(idea => <button className="opt" key={idea} disabled={d.description.length > 0} onClick={() => { if (!d.description.length) updateWorldDraft({ description: idea }) }}>{idea}</button>)}</details>
    {!draft.editingId && <p className="small prototype-note">DEV: пока показываем пример наполнения вместо AI-генерации.</p>}
    <button className="text-link" onClick={() => nav.push('05-12_world_advanced')}>Расширенные настройки <span>→</span></button><p className="small muted">Правила, герои и завязка — если хочется настроить всё самому.</p>
  </div><BottomCTA main="Мой персонаж" disabled={!d.title.trim() || !d.description.trim()} onMain={() => { const current = getWorldDraft()!; setWorldDraft({ ...current, definition: draft.editingId ? d : prototypeWorld(d), previewOnly: !draft.editingId }); nav.push('01-4_channel') }} /></div>
}
export function QuickChannelForm({ draft }: { draft: WorldDraft }) {
  const guest = useGuestSession()
  const nav = useNav(); const d = draft.definition; const selected = draft.persona
  const choose = (star?: WorldDefinition['stars'][number]) => { if (guest && star) { guest.chooseHero(star.id); return } tap('world_choose_character', { kind: star ? 'preset' : 'custom' }); setWorldDraft(withPersona(draft, star)) }
  const goal = guest?.goal ?? presetGoalFor(d)
  const heroes = [...d.stars, ...(selected ? [selected.star] : [])].sort((a, b) => (draft.heroOrder?.indexOf(a.id) ?? 0) - (draft.heroOrder?.indexOf(b.id) ?? 0))
  return <div className="screen no-tab"><TopBar title={`Персонаж · ${d.templateId === null ? '2 / 3' : '1 / 2'}`} onBack={nav.back} /><div className="pad"><h1>Кем ты будешь в этой истории?</h1><p className="muted">{guest ? 'Выбери героя мира. История будет идти от твоего лица.' : 'Свой персонаж или герой мира — история будет идти от твоего лица.'}</p>

    {heroes.length > 1 && heroes.map(star => <button className={`persona-option ${selected?.star.id === star.id ? 'selected' : ''}`} aria-pressed={selected?.star.id === star.id} key={star.id} onClick={() => choose(star)}><Avatar seed={star.avatarSeed} avatarStyle={d.avatarStyle} world={d.templateId ?? undefined} mode="default" /><div><b>{star.name}</b><div className="small muted">{star.role}</div></div><span>{selected?.star.id === star.id ? '✓' : ''}</span></button>)}
    {!guest && <button className={`persona-option ${!selected ? 'selected' : ''}`} aria-pressed={!selected} onClick={() => choose()}><span>✦</span><div><b>Свой персонаж</b><div className="small muted">Твоё имя, твоя роль в истории</div></div><span>{!selected ? '✓' : ''}</span></button>}
    {goal && <Card><b>Твоя цель</b><p>{goal.goal}</p>{!selected && goal.context && <p className="small muted">{goal.context}</p>}<p className="small muted">{goal.firstStep}</p></Card>}
    {selected ? selected.star.about && <Card><ExpandableText text={selected.star.about} /></Card> : <div className="quick-personal"><label className="label">Имя персонажа<input aria-label="Имя персонажа" value={d.name} maxLength={80} onChange={e => updateWorldDraft({ name: e.target.value })} /></label><label className="label">Кто ты в этом мире<input aria-label="Кто ты в этом мире" value={d.role} maxLength={80} onChange={e => updateWorldDraft({ role: e.target.value })} /></label></div>}
    {!guest && <><div className="label">Как к тебе обращаться</div><Segmented value={draft.address} onChange={(address: Address) => setWorldDraft({ ...draft, address })} options={[{ id: 'she', label: 'она' }, { id: 'he', label: 'он' }, { id: 'nick', label: 'по нику' }]} />
    <details className="card profile-preview"><summary><Avatar seed={d.avatar} avatarStyle={d.avatarStyle} profile={{ address: draft.address, ageBand: draft.ageBand, personalizeAvatar: draft.personalizeAvatar }} size={40} /><span><b>{d.name || "Твой персонаж"}</b><span className="small muted">{displayHandle(d.avatar)}</span></span><span className="small">Изменить</span></summary><label className="label">Ник<input aria-label="Ник" value={d.avatar} maxLength={80} onChange={e => updateWorldDraft({ avatar: e.target.value })} /></label><Avatar seed={d.avatar} avatarStyle={d.avatarStyle} profile={{ address: draft.address, ageBand: draft.ageBand, personalizeAvatar: draft.personalizeAvatar }} size={56} /><AvatarPersonalization enabled={draft.personalizeAvatar !== false} onChange={personalizeAvatar => setWorldDraft({ ...draft, personalizeAvatar })} /></details></>}
  </div><BottomCTA main="Выбрать градус драмы" disabled={!d.name.trim() || !d.role.trim() || !d.avatar.trim()} onMain={() => { if (!guest) recordEntryStep('landing_character_selected'); nav.push('01-4b_world_setup') }} /></div>
}
export function DifficultyPicker({ definition }: { definition: WorldDefinition }) {
  return <section><h2>Сложность</h2><Segmented value={definition.difficulty} onChange={difficulty => updateWorldDraft({ difficulty })} options={[{ id: 'easy', label: 'Легко' }, { id: 'normal', label: 'Средне' }, { id: 'hard', label: 'Сложно' }]} /><p className="small muted">Чем сложнее, тем быстрее рост подписчиков и выше риск провала. Изменение действует на следующие действия.</p></section>
}

export function DramaPicker({ definition: d, beforeOptions, withDifficulty = true, fixed = false }: { definition: WorldDefinition; beforeOptions?: ReactNode; withDifficulty?: boolean; fixed?: boolean }) {
  return <>{withDifficulty && <DifficultyPicker definition={d} />}<h1>Сколько драмы добавим?</h1><p className="muted">{fixed ? 'В пробной истории доступно «Мягко». Остальные варианты откроются после входа.' : 'Выбери настроение истории. Его можно изменить позже.'}</p>{beforeOptions}<div className="drama-options">{dramaOptions.map(option => <button className={`drama-option ${d.drama === option.id ? 'selected' : ''}`} aria-pressed={d.drama === option.id} disabled={fixed && d.drama !== option.id} onClick={fixed ? undefined : () => { tap('world_choose_mood', { kind: option.id }); updateWorldDraft({ drama: option.id }) }} key={option.id}><span className="drama-icon">{option.icon}</span><div><b>{option.title}</b>{(!fixed || d.drama === option.id) && <p>{option.description}</p>}{d.drama === option.id && <div className="drama-example">{option.example}</div>}</div><span>{d.drama === option.id ? '✓' : fixed ? '🔒' : ''}</span></button>)}</div></>
}
export function WorldPrototypePreview() {
  const draft = useWorldDraft(); const nav = useNav()
  if (!draft) return <div className="screen no-tab"><TopBar title="Демо мира" onBack={nav.back} /><div className="pad"><Button onClick={() => nav.replace('01-3_world')}>Выбрать мир</Button></div></div>
  const d = draft.definition; const first = d.stars.find(star => star.id === (draft.persona?.star.id === 'georgina' ? 'roman' : 'georgina')) ?? d.stars[0]
  return <div className="screen no-tab"><TopBar title="Предпросмотр истории" onBack={nav.back} /><div className="pad"><div className="small prototype-note">Предпросмотр · без сохранения</div><h1>{d.emoji} {d.title}</h1><p className="muted">{d.description}</p><div className="pill">{dramaOptions.find(value => value.id === d.drama)?.title}</div>
    <h2>Завязка истории</h2><Card><div className="row"><Avatar seed={first?.avatarSeed} avatarStyle={d.avatarStyle} mode="default" /><div><b>{first?.name}</b><div className="small muted">{first?.role}</div></div></div><p>{d.firstContext}</p><div className="small muted">♡ 12　💬 2</div></Card>
    <p className="small muted">Это пример начала истории. Мир не сохраняется, посты не публикуются.</p>
    <button className="text-link" onClick={() => nav.push('05-12_world_advanced')}>Посмотреть расширенные настройки →</button>
  </div><BottomCTA main="Вернуться к настройке" onMain={() => nav.back()} /></div>
}
