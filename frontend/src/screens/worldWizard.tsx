import { useEffect, useState } from 'react'
import { api, ApiError } from '../api/client'
import { worldDefinitionOf, worlds } from '../api/worldContent'
import type { Address, WorldDefinition, WorldDocument, WorldId } from '../api/types'
import { useNav } from '../app/nav'
import { useStore } from '../app/store'
import { blankWorld, getWorldDraft, selectDraftOwner, setWorldDraft, updateWorldDraft, useWorldDraft } from '../app/worldDraft'
import { pendingReferral, clearReferral } from '../app/referral'
import { Avatar, BottomCTA, Button, Card, Dots, Segmented, TopBar } from '../ui'
import { TextField } from '../ui/TextField'
import type { AgeBand } from '../app/ageBand'

const field = (label: string, key: keyof WorldDefinition, definition: WorldDefinition, maxLength = 2000) => <TextField key={key} label={label} value={String(definition[key])} onChange={value => updateWorldDraft({ [key]: value })} maxLength={maxLength} rows={key === 'description' ? 5 : 2} />
function List({ label, values, onChange }: { label: string; values: string[]; onChange: (v: string[]) => void }) {
  return <details className="card" style={{ marginBottom: 12 }}><summary>{label} · {values.length}</summary>
    {values.map((value, index) => <div key={index}><TextField label={`${label} ${index + 1}`} value={value} onChange={text => onChange(values.map((v, i) => i === index ? text : v))} /><button className="chip" onClick={() => onChange(values.filter((_, i) => i !== index))}>Удалить</button></div>)}
    <button className="chip" onClick={() => onChange([...values, ''])}>＋ Добавить</button></details>
}
export function ChooseWorld() {
  const nav = useNav(); const { player, setPlayer } = useStore()
  const [owned, setOwned] = useState<WorldDocument[]>([])
  const [error, setError] = useState('')
  useEffect(() => { if (player) api.ownedWorlds().then(setOwned).catch(() => setError('Не получилось загрузить твои миры.')) }, [player?.id]) // eslint-disable-line react-hooks/exhaustive-deps
  function start(world?: WorldId) {
    selectDraftOwner(player?.id ?? 'onboarding')
    const definition = world ? worldDefinitionOf(world) : blankWorld()
    // Новый голос вводится явно; старый черновик сохраняется при возврате.
    definition.voice = ''
    setWorldDraft({ definition, address: player?.address ?? 'nick', group: (nav.params.group as '14-17' | '18+') ?? '14-17', ageBand: player?.ageBand ?? nav.params.ageBand as AgeBand | undefined })
    nav.push('01-4_channel', { world: definition.balanceKey })
  }
  return <div className="screen no-tab"><TopBar title="Выбери мир" onBack={nav.canBack ? nav.back : undefined} />
    <div className="pad"><h1>Где начнётся твоя история?</h1><p className="muted">Все готовые миры доступны сразу. У каждого — свои герои и темп роста.</p>
      {getWorldDraft() && <Button secondary onClick={() => nav.push('01-4_channel')}>Продолжить настройку</Button>}
      {worlds.map(world => <button key={world.id} className="card world-choice" onClick={() => start(world.id)}><span style={{ fontSize: 30 }}>{world.emoji}</span><div><b>{world.title}</b> <Dots n={world.difficulty} /><div className="small muted">{world.description}</div></div><span>›</span></button>)}
      <Card><b>✨ Создать с нуля</b><p className="small muted">{player?.hitOwnWorldMilestone ? 'Придумай место, правила и героев. Твои другие миры сохранятся.' : 'Набери 500 подписчиков в любом мире или пригласи друга по своей ссылке. Когда начислится бонус за друга, создание с нуля станет доступно.'}</p>
        <Button disabled={!player?.hitOwnWorldMilestone} onClick={() => start()}>Создать с нуля</Button>
        {player && !player.hitOwnWorldMilestone && <Button secondary onClick={() => nav.push('04-3_invite')}>Пригласить друга</Button>}
      </Card>
      {owned.length > 0 && <><h2>Твои миры</h2>{owned.map(world => <Button key={world.id} secondary onClick={async () => { try { const p = await api.me(world.id); setPlayer(p); nav.replace('03-5_stage') } catch { setError('Не получилось открыть мир.') } }}>{world.emoji} {world.title} · {world.subscribers}</Button>)}</>}
      {error && <p role="alert">{error}</p>}
    </div></div>
}
function Ready({ children }: { children: (draft: NonNullable<ReturnType<typeof useWorldDraft>>) => React.ReactNode }) {
  const draft = useWorldDraft(); const nav = useNav()
  if (!draft) return <div className="screen no-tab"><TopBar title="Настройка мира" onBack={nav.back} /><div className="pad"><p>Выбери шаблон, чтобы начать настройку.</p><Button onClick={() => nav.replace('01-3_world')}>Выбрать мир</Button></div></div>
  return <>{children(draft)}</>
}
export function ChannelSetup() {
  return <Ready>{draft => <ChannelForm draft={draft} />}</Ready>
}
function ChannelForm({ draft }: { draft: NonNullable<ReturnType<typeof useWorldDraft>> }) {
  const nav = useNav(); const d = draft.definition
  const update = updateWorldDraft
  return <div className="screen no-tab"><TopBar title="Твой канал · 1 / 4" onBack={nav.back} /><div className="pad">
    {draft.reason && <Card>{draft.reason}</Card>}
    <h1>Кем будешь в этом мире?</h1>
    {field('Название канала', 'name', d, 80)}{field('Ник и аватар', 'avatar', d, 80)}
    <div className="card row"><Avatar seed={d.avatar || 'new-player'} size={52} profile={{ address: draft.address, ageBand: draft.ageBand }} world={d.balanceKey} avatarStyle={d.avatarStyle} /><div>{d.name || 'Твой канал'}<div className="small muted">{d.avatar}</div></div></div>
    <div className="label">Как к тебе обращаться</div><Segmented value={draft.address} onChange={(address: Address) => setWorldDraft({ ...draft, address })} options={[{ id: 'she', label: 'она' }, { id: 'he', label: 'он' }, { id: 'nick', label: 'по нику' }]} />
    {field('Твоя роль', 'role', d, 80)}
    {d.roles.map(role => <button className="chip" key={role} onClick={() => update({ role })}>{role}</button>)}
    {(d.stars.length > 1 || draft.persona) && <details className="card"><summary>Играть за героя мира</summary><p className="small muted">Выбранный герой станет твоей персоной и не будет второй AI-копией в ленте.</p>
      {draft.persona && <><p>Сейчас: {draft.persona.star.name}</p><button className="opt" onClick={() => { const persona = draft.persona!; setWorldDraft({ ...draft, persona: undefined, definition: { ...d, name: persona.name, avatar: persona.avatar, role: persona.role, description: persona.description, stars: [...d.stars, persona.star] } }) }}>Вернуться к своему персонажу</button></>}
      {d.stars.map(star => <button className="opt" key={star.id} onClick={() => {
        const previous = draft.persona; const description = previous?.description ?? d.description
        setWorldDraft({ ...draft, persona: { star, description, name: previous?.name ?? d.name, avatar: previous?.avatar ?? d.avatar, role: previous?.role ?? d.role }, definition: { ...d, name: star.name, avatar: star.avatarSeed, role: star.role, description: `${description}\nПерсона игрока: ${star.name}. ${star.about}`.slice(0, 2000), stars: [...d.stars.filter(value => value.id !== star.id), ...(previous ? [previous.star] : [])] } })
      }}>{star.name} — {star.role}</button>)}
    </details>}
    <div className="label">Стиль аватара</div><Segmented value={d.avatarStyle} onChange={avatarStyle => update({ avatarStyle })} options={['lorelei', 'notionists', 'pixelArt', 'thumbs', 'shapes'].map((id, i) => ({ id, label: ['портрет', 'скетч', 'пиксели', 'мини', 'формы'][i] }))} />
    <List label="Роли мира" values={d.roles} onChange={roles => update({ roles })} />
  </div><BottomCTA main="Описание и правила" disabled={!d.name.trim() || !d.avatar.trim() || !d.role.trim()} onMain={() => nav.push('05-7b_own_world', { world: d.balanceKey })} /></div>
}
export function DescriptionSetup() { return <Ready>{draft => <DescriptionForm definition={draft.definition} />}</Ready> }
function DescriptionForm({ definition: d }: { definition: WorldDefinition }) {
  const nav = useNav()
  return <div className="screen no-tab"><TopBar title="Мир · 2 / 4" onBack={nav.back} /><div className="pad">
    {field('Название мира', 'title', d, 120)}{field('Описание мира', 'description', d)}{field('Где происходит история', 'where', d, 200)}
    {field('Эмодзи мира', 'emoji', d, 8)}{field('Краткое описание', 'summary', d, 200)}
    <div className="label">Темп истории</div><Segmented value={String(d.difficulty)} onChange={value => updateWorldDraft({ difficulty: Number(value) as 1 | 2 | 3 })} options={[{ id: '1', label: 'спокойно' }, { id: '2', label: 'обычно' }, { id: '3', label: 'интенсивно' }]} />
    <p className="small muted">Этот выбор меняет обозначение мира. Экономика сохраняется от выбранного пресета.</p>
    <h2>Правила мира</h2>{d.rules.map((rule, index) => rule.locked ? <Card key={index}>🔒 {rule.text}</Card> : <div key={index}>{fieldRule(rule.text, index, d)}<button className="chip" onClick={() => updateWorldDraft({ rules: d.rules.filter((_, i) => i !== index) })}>Удалить правило</button></div>)}
    <button className="chip" onClick={() => updateWorldDraft({ rules: [...d.rules, { text: '', locked: false }] })}>＋ Правило</button>
    {field('Первая завязка', 'firstContext', d)}{field('Первый вопрос', 'firstPrompt', d)}
    <List label="Идеи постов" values={d.postIdeas} onChange={postIdeas => updateWorldDraft({ postIdeas })} />
    <List label="Примеры постов" values={d.demoPosts} onChange={demoPosts => updateWorldDraft({ demoPosts })} />
    <details className="card"><summary>Желания и коллекция</summary><p className="small muted">Новые отметки начинаются закрытыми. При редактировании прогресс сохраняется сервером; подпись награды не меняет баланс.</p>
      {d.wants.map((want, i) => <div key={want.id}><TextField label="Желание" value={want.text} onChange={text => updateWorldDraft({ wants: d.wants.map((v, j) => i === j ? { ...v, text } : v) })} /><TextField label="Подпись награды" value={want.reward} onChange={reward => updateWorldDraft({ wants: d.wants.map((v, j) => i === j ? { ...v, reward } : v) })} /><button className="chip" onClick={() => updateWorldDraft({ wants: d.wants.filter((_, j) => i !== j) })}>Удалить</button></div>)}
      <button className="chip" onClick={() => updateWorldDraft({ wants: [...d.wants, { id: crypto.randomUUID(), text: '', reward: 'опыт ×1,3', done: false }] })}>＋ Желание</button>
      {d.achievements.map((item, i) => <div key={item.id}><TextField label="Название карточки" value={item.title} onChange={title => updateWorldDraft({ achievements: d.achievements.map((v, j) => i === j ? { ...v, title } : v) })} /><TextField label="Эмодзи карточки" value={item.emoji} onChange={emoji => updateWorldDraft({ achievements: d.achievements.map((v, j) => i === j ? { ...v, emoji } : v) })} maxLength={8} /><button className="chip" onClick={() => updateWorldDraft({ achievements: d.achievements.filter((_, j) => i !== j) })}>Удалить</button></div>)}
      <button className="chip" onClick={() => updateWorldDraft({ achievements: [...d.achievements, { id: crypto.randomUUID(), emoji: '⭐', title: '', open: false }] })}>＋ Карточка</button>
    </details>
  </div><BottomCTA main="Голос и драма" disabled={!d.title.trim() || !d.description.trim() || d.rules.some(rule => !rule.text.trim())} onMain={() => nav.push('01-4b_world_setup', { world: d.balanceKey })} /></div>
}
function fieldRule(text: string, index: number, d: WorldDefinition) { return <TextField label={`Правило ${index + 1}`} value={text} onChange={value => updateWorldDraft({ rules: d.rules.map((rule, i) => i === index ? { ...rule, text: value } : rule) })} /> }
export function VoiceSetup() { return <Ready>{draft => <VoiceForm definition={draft.definition} />}</Ready> }
function VoiceForm({ definition: d }: { definition: WorldDefinition }) {
  const nav = useNav()
  return <div className="screen no-tab"><TopBar title="Голос канала · 3 / 4" onBack={nav.back} /><div className="pad"><h1>Как звучит твой канал?</h1><p className="muted">Опиши, как хочешь писать: коротко и с юмором, спокойно или дерзко. «Подскажи» использует этот стиль для черновиков. Свой текст можно писать как угодно. Стиль можно изменить позже.</p>
    <TextField label="Голос канала" value={d.voice} onChange={voice => updateWorldDraft({ voice })} ideas={d.voices.length ? d.voices : ['Коротко, с добрым юмором и без пафоса', 'Спокойно, с точными деталями']} maxLength={200} placeholder="Например: коротко, с добрым юмором и без пафоса" />
    <List label="Примеры голоса" values={d.voices} onChange={voices => updateWorldDraft({ voices })} />
    <h2>Градус драмы</h2><Segmented value={d.drama} onChange={drama => updateWorldDraft({ drama })} options={[{ id: 'soft', label: 'мягко' }, { id: 'mid', label: 'средне' }, { id: 'hot', label: 'остро' }, { id: 'off', label: 'без хейта' }]} /><p className="small muted">Безопасность 14+ работает всегда.</p>
  </div><BottomCTA main="Настроить героев" disabled={!d.voice.trim()} onMain={() => nav.push('01-5_first_star', { world: d.balanceKey })} /></div>
}
export function StarsSetup() { return <Ready>{draft => <StarsForm draft={draft} />}</Ready> }
function StarsForm({ draft }: { draft: NonNullable<ReturnType<typeof useWorldDraft>> }) {
  const nav = useNav(); const { setPlayer } = useStore(); const d = draft.definition
  const [saving, setSaving] = useState(false); const [error, setError] = useState(''); const [uncertain, setUncertain] = useState(false)
  const change = (id: string, patch: Partial<WorldDefinition['stars'][number]>) => updateWorldDraft({ stars: d.stars.map(star => star.id === id ? { ...star, ...patch } : star) })
  const save = async () => {
    if (saving || uncertain) return
    setSaving(true); setError('')
    try {
      if (!api.hasPlayer()) { const identity = await api.onboarding({ birthGroup: draft.group, ageBand: draft.ageBand, address: draft.address, referralCode: pendingReferral() }); clearReferral(); setWorldDraft(null); selectDraftOwner(identity.id); setWorldDraft(draft) }
      await api.settings({ address: draft.address })
      const player = draft.editingId ? await api.saveWorld(draft.editingId, d) : await api.createWorld(d)
      setWorldDraft(null); setPlayer(player); nav.replace('03-5_stage')
    } catch (cause) {
      if (!draft.editingId && !(cause instanceof ApiError)) { setUncertain(true); setError('Не удалось подтвердить создание. Сначала проверь список миров: повтор может создать ещё один экземпляр.') }
      else setError(cause instanceof ApiError && cause.status === 409 ? 'Мир изменился или создание с нуля пока недоступно. Твои изменения сохранены в черновике.' : 'Не получилось сохранить мир. Проверь поля и попробуй снова.')
    } finally { setSaving(false) }
  }
  return <div className="screen no-tab"><TopBar title="Герои · 4 / 4" onBack={saving ? undefined : nav.back} /><div className="pad"><h1>Кто живёт в твоём мире?</h1><p className="muted">Настрой состав героев, их характеры и голоса. Это не выбор первой подписки.</p>
    {d.stars.map(star => <details className="card" key={star.id} open><summary>{star.name || 'Новый герой'}</summary><div className="row"><Avatar seed={star.avatarSeed || star.id} size={48} world={d.balanceKey} /><b>{star.name}</b></div>
      <TextField label="Имя героя" value={star.name} onChange={name => change(star.id, { name })} maxLength={80} />
      <TextField label="Роль героя" value={star.role} onChange={role => change(star.id, { role })} maxLength={80} />
      <TextField label="Характер и история" value={star.about} onChange={about => change(star.id, { about })} />
      <TextField label="Голос героя" value={star.voice} onChange={voice => change(star.id, { voice })} />
      <TextField label="Образ аватара" value={star.avatarSeed} onChange={avatarSeed => change(star.id, { avatarSeed })} maxLength={80} />
      <label className="label">Главная черта<select value={star.trait} onChange={event => change(star.id, { trait: Number(event.target.value) })}>{['Смелость', 'Поддержка', 'Загадка', 'Воображение', 'Любопытство', 'Энергия', 'Ирония и расчёт', 'Сухой юмор', 'Самоирония', 'Азарт', 'Внимание'].map((label, i) => <option key={label} value={i}>{label}</option>)}</select></label>
      <button className="chip" disabled={saving} onClick={() => updateWorldDraft({ stars: d.stars.filter(value => value.id !== star.id) })}>Убрать героя</button>
    </details>)}
    <Button secondary disabled={saving} onClick={() => updateWorldDraft({ stars: [...d.stars, { id: crypto.randomUUID().replaceAll('-', ''), name: '', role: '', about: '', voice: '', avatarSeed: crypto.randomUUID(), trait: 1, relation: 0 }] })}>＋ Добавить героя</Button>
    {error && <p role="alert">{error}</p>}{uncertain && <Button onClick={() => nav.replace('01-3_world')}>Проверить список миров</Button>}
    <p className="small muted">Сохраняем отдельный мир. {draft.editingId ? 'Подписчики и прогресс останутся на месте.' : 'Другие миры останутся на месте.'}</p>
  </div><BottomCTA main={saving ? 'Сохраняем…' : draft.editingId ? 'Сохранить изменения' : 'Создать мир'} disabled={saving || uncertain || d.stars.length === 0 || d.stars.some(star => !star.name.trim() || !star.role.trim() || !star.about.trim() || !star.voice.trim())} onMain={save} /></div>
}
