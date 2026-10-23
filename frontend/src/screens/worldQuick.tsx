import { useState, type ReactNode } from 'react'
import { useStore } from '../app/store'
import { avatarPersonalization } from '../app/avatarPersonalization'
import { displayHandle } from '../app/handle'
import type { AgeBand } from '../app/ageBand'
import { worldDefinitionOf } from '../api/worldContent'
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

export function WorldOverview() {
  const draft = useWorldDraft(); const nav = useNav(); const { player } = useStore()
  const [confirmSwitch, setConfirmSwitch] = useState(false)
  const world = nav.params.world as WorldId | undefined ?? draft?.definition.templateId
  if (!world) return <div className="screen no-tab world-overview"><TopBar title="Обзор мира" onBack={nav.back} /><div className="pad"><Button onClick={() => nav.replace('01-3_world')}>Выбрать мир</Button></div></div>
  const d = worldDefinitionOf(world); const art = worldArt[d.templateId ?? '']; const goal = presetGoalFor(d)
  const begin = (confirmed = false) => {
    selectDraftOwner(player?.id ?? 'onboarding')
    const previous = getWorldDraft()
    if (!confirmed && previous && (previous.definition.templateId !== world || previous.editingId)) { setConfirmSwitch(true); return }
    const restored = savedWorldDraft(world)
    setWorldDraft(restored && !restored.previewOnly ? restored : {
      definition: d, heroOrder: d.stars.map(star => star.id), address: player?.address ?? 'nick',
      group: player?.ageBand ? (player.ageBand === '14-17' ? '14-17' : '18+') : (nav.params.group as '14-17' | '18+') ?? '14-17',
      ageBand: player?.ageBand ?? nav.params.ageBand as AgeBand | undefined,
      personalizeAvatar: player ? avatarPersonalization(player.id) : true,
    })
    setConfirmSwitch(false)
    nav.push('01-4_channel')
  }
  return <div className="screen no-tab world-overview"><TopBar title="Обзор мира" onBack={nav.back} />
    {art && <WorldCover world={world} className="world-hero" priority alt={`Обложка мира «${d.title}»`} />}
    <div className="pad"><div className="small eyebrow">{art?.genre}</div><h1>{d.emoji} {d.title}</h1><p className="muted">{art?.pitch ?? d.description}</p>
      {goal && <Card><div className="small eyebrow">Твой путь</div><p>{goal.goal}</p></Card>}
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
  const nav = useNav(); const d = draft.definition; const selected = draft.persona
  const choose = (star?: WorldDefinition['stars'][number]) => {
    const previous = draft.persona
    const heroOrder = draft.heroOrder ?? [...d.stars, ...(previous ? [previous.star] : [])].map(value => value.id)
    const restored = { ...d, stars: [...d.stars, ...(previous ? [previous.star] : [])], description: previous?.description ?? d.description, name: previous?.name ?? d.name, avatar: previous?.avatar ?? d.avatar, role: previous?.role ?? d.role }
    if (!star) { setWorldDraft({ ...draft, heroOrder, persona: undefined, definition: restored }); return }
    setWorldDraft({ ...draft, heroOrder, persona: { star, description: restored.description, name: restored.name, avatar: restored.avatar, role: restored.role }, definition: { ...restored, name: star.name, avatar: star.avatarSeed, role: star.role, description: `${restored.description}\nПерсона игрока: ${star.name}. ${star.about}`.slice(0, 2000), stars: restored.stars.filter(value => value.id !== star.id) } })
  }
  const heroes = [...d.stars, ...(selected ? [selected.star] : [])].sort((a, b) => (draft.heroOrder?.indexOf(a.id) ?? 0) - (draft.heroOrder?.indexOf(b.id) ?? 0))
  return <div className="screen no-tab"><TopBar title={`Персонаж · ${d.templateId === null ? '2 / 3' : '1 / 2'}`} onBack={nav.back} /><div className="pad"><h1>Кем ты будешь в этой истории?</h1><p className="muted">Свой персонаж или герой мира — история будет идти от твоего лица.</p>
    <button className={`persona-option ${!selected ? 'selected' : ''}`} aria-pressed={!selected} onClick={() => choose()}><span>✦</span><div><b>Свой персонаж</b><div className="small muted">Твоё имя, твоя роль в истории</div></div><span>{!selected ? '✓' : ''}</span></button>
    {heroes.length > 1 && heroes.map(star => <button className={`persona-option ${selected?.star.id === star.id ? 'selected' : ''}`} aria-pressed={selected?.star.id === star.id} key={star.id} onClick={() => choose(star)}><Avatar seed={star.avatarSeed} avatarStyle={d.avatarStyle} world={d.templateId ?? undefined} mode="default" /><div><b>{star.name}</b><div className="small muted">{star.role}</div></div><span>{selected?.star.id === star.id ? '✓' : ''}</span></button>)}
    {selected ? <Card><ExpandableText text={selected.star.about} /></Card> : <div className="quick-personal"><label className="label">Имя персонажа<input aria-label="Имя персонажа" value={d.name} maxLength={80} onChange={e => updateWorldDraft({ name: e.target.value })} /></label><label className="label">Кто ты в этом мире<input aria-label="Кто ты в этом мире" value={d.role} maxLength={80} onChange={e => updateWorldDraft({ role: e.target.value })} /></label></div>}
    <div className="label">Как к тебе обращаться</div><Segmented value={draft.address} onChange={(address: Address) => setWorldDraft({ ...draft, address })} options={[{ id: 'she', label: 'она' }, { id: 'he', label: 'он' }, { id: 'nick', label: 'по нику' }]} />
    <details className="card profile-preview"><summary><Avatar seed={d.avatar} avatarStyle={d.avatarStyle} profile={{ address: draft.address, ageBand: draft.ageBand, personalizeAvatar: draft.personalizeAvatar }} size={40} /><span><b>{d.name || "Твой персонаж"}</b><span className="small muted">{displayHandle(d.avatar)}</span></span><span className="small">Изменить</span></summary><label className="label">Ник<input aria-label="Ник" value={d.avatar} maxLength={80} onChange={e => updateWorldDraft({ avatar: e.target.value })} /></label><Avatar seed={d.avatar} avatarStyle={d.avatarStyle} profile={{ address: draft.address, ageBand: draft.ageBand, personalizeAvatar: draft.personalizeAvatar }} size={56} /><AvatarPersonalization enabled={draft.personalizeAvatar !== false} onChange={personalizeAvatar => setWorldDraft({ ...draft, personalizeAvatar })} /></details>
  </div><BottomCTA main="Выбрать градус драмы" disabled={!d.name.trim() || !d.role.trim() || !d.avatar.trim()} onMain={() => nav.push('01-4b_world_setup')} /></div>
}
export function DifficultyPicker({ definition }: { definition: WorldDefinition }) {
  return <section><h2>Сложность</h2><Segmented value={definition.difficulty} onChange={difficulty => updateWorldDraft({ difficulty })} options={[{ id: 'easy', label: 'Легко' }, { id: 'normal', label: 'Средне' }, { id: 'hard', label: 'Сложно' }]} /><p className="small muted">Чем сложнее, тем быстрее рост подписчиков и выше риск провала. Изменение действует на следующие действия.</p></section>
}

export function DramaPicker({ definition: d, beforeOptions, withDifficulty = true }: { definition: WorldDefinition; beforeOptions?: ReactNode; withDifficulty?: boolean }) {
  return <>{withDifficulty && <DifficultyPicker definition={d} />}<h1>Сколько драмы добавим?</h1><p className="muted">Выбери настроение истории. Его можно изменить позже.</p>{beforeOptions}<div className="drama-options">{dramaOptions.map(option => <button className={`drama-option ${d.drama === option.id ? 'selected' : ''}`} aria-pressed={d.drama === option.id} onClick={() => updateWorldDraft({ drama: option.id })} key={option.id}><span className="drama-icon">{option.icon}</span><div><b>{option.title}</b><p>{option.description}</p>{d.drama === option.id && <div className="drama-example">{option.example}</div>}</div><span>{d.drama === option.id ? '✓' : ''}</span></button>)}</div></>
}
export function WorldPrototypePreview() {
  const draft = useWorldDraft(); const nav = useNav()
  if (!draft) return <div className="screen no-tab"><TopBar title="Демо мира" onBack={nav.back} /><div className="pad"><Button onClick={() => nav.replace('01-3_world')}>Выбрать мир</Button></div></div>
  const d = draft.definition; const [first, second] = d.stars
  return <div className="screen no-tab"><TopBar title="Так начнётся твоя история" onBack={nav.back} /><div className="pad"><div className="small prototype-note">Демо · мир ещё не создан</div><h1>{d.emoji} {d.title}</h1><p className="muted">{d.description}</p><div className="pill">{dramaOptions.find(value => value.id === d.drama)?.title}</div>
    <h2>Первая запись в ленте</h2><Card><div className="row"><Avatar seed={first?.avatarSeed} avatarStyle={d.avatarStyle} mode="default" /><div><b>{first?.name}</b><div className="small muted">{first?.role}</div></div></div><p>{d.firstContext}</p><div className="small muted">♡ 12　💬 2</div></Card>
    {second && <Card><b>{second.name}</b><p>У меня есть другая версия. Давай сравним находки в ленте?</p></Card>}
    <p className="small muted">Это пример наполнения для проверки интерфейса. После согласования AI сможет подбирать героев и завязку под твоё описание.</p>
    <button className="text-link" onClick={() => nav.push('05-12_world_advanced')}>Посмотреть расширенные настройки →</button>
  </div><BottomCTA main="Вернуться к настройке" onMain={() => nav.back()} /></div>
}
