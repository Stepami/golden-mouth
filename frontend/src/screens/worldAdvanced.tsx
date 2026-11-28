import { useEffect, useRef, useState, type ReactNode } from 'react'
import { flushSync } from 'react-dom'
import { heroHandle } from '../app/handle'
import { useNav } from '../app/nav'
import { useStore } from '../app/store'
import { isOwnWorldSetup, ownWorldRole, setWorldDraft, updateWorldDraft, type WorldDraft } from '../app/worldDraft'
import type { Address, WorldDefinition, WorldDocument } from '../api/types'
import { Avatar, Button, Segmented, TopBar, plural } from '../ui'
import { AvatarPersonalization } from '../ui/AvatarPersonalization'
import { DifficultyPicker, DramaPicker } from './worldQuick'
import { heroTraits, WorldCardPreview, WorldEditorHelp, type WorldHelpTarget } from './worldEditorHelp'
import { WorldCharacterPicker } from './worldCharacterPicker'

export function AgeRuleNote({ teen = true }: { teen?: boolean }) {
  return <div className="age-rule-note"><b>🔒 Ограничения контента</b><p>{teen ? 'Без взрослой сексуализированной подачи. Дружба и романтические симпатии допустимы.' : 'Чувствительные темы можно обсуждать без откровенных подробностей.'}</p><p className="small muted">Травля и откровенный контент запрещены для всех.</p></div>
}

function StringList({ title, help, values, maxLength = 2000, onChange, info }: {
  title: string; help: string; values: string[]; maxLength?: number; onChange: (values: string[]) => void; info?: (label: string, value?: string) => ReactNode
}) {
  return <details className="editor-extra">
    <summary>{title} · {values.length}</summary>
    <div className="world-field-label"><p className="small muted">{help}</p>{info?.(title, values.find(value => value.trim()))}</div>
    {values.map((value, i) => <div className="editor-list-row" key={i}>
      <input aria-label={`${title} ${i + 1}`} value={value} maxLength={maxLength} onChange={e => onChange(values.map((v, j) => j === i ? e.target.value : v))} />
      {info?.(`${title} ${i + 1}`, value)}
      <button className="text-link" aria-label={`Удалить: ${title} ${i + 1}`} onClick={() => onChange(values.filter((_, j) => j !== i))}>Убрать</button>
    </div>)}
    <button className="chip" onClick={() => onChange([...values, ''])}>＋ Добавить</button>
  </details>
}

type Field = {
  id: string; label: string; value: string; onChange: (value: string) => void
  help?: string; placeholder?: string; rows?: number; maxLength?: number; required?: boolean; error?: string
  info?: WorldHelpTarget
}
function WorldField({ id, label, value, onChange, help, placeholder, rows = 1, maxLength = 2000, required, error, infoButton }: Field & { infoButton?: ReactNode }) {
  const props = {
    id, value, maxLength, placeholder, required,
    'aria-invalid': error ? true : undefined,
    'aria-describedby': help || error ? `${id}-help` : undefined,
    onChange: (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => onChange(e.currentTarget.value),
  }
  return <div className="world-field">
    <div className="world-field-label"><label className="label" htmlFor={id}>{label}{required && <span className="down" aria-hidden="true"> *</span>}</label>{infoButton}</div>
    {rows === 1 ? <input {...props} /> : <textarea {...props} rows={rows} />}
    {(help || error) && <p id={`${id}-help`} className={`small ${error ? 'down' : 'muted'}`} role={error ? 'alert' : undefined}>{error || help}</p>}
  </div>
}

const starField = (id: string, field: string) => `hero-${id}-${field}`

function showEditorField(control: HTMLElement) {
  const field = control.closest('.world-field') ?? control
  const viewport = window.visualViewport
  if (!viewport) { field.scrollIntoView({ block: 'center' }); return }
  const { top, bottom } = field.getBoundingClientRect()
  const visibleTop = viewport.offsetTop + 56
  const visibleBottom = viewport.offsetTop + viewport.height - 16
  if (top < visibleTop || bottom > visibleBottom) window.scrollBy(0, (top + bottom - visibleTop - visibleBottom) / 2)
}

export function WorldAdvanced({ draft, saving, error, uncertain, onSave, onCheck, editRole }: {
  draft: WorldDraft; saving: boolean; error: string; uncertain: boolean; onSave: () => void; onCheck: () => void; editRole?: boolean
}) {
  const nav = useNav(); const { player } = useStore(); const d = draft.definition
  const custom = isOwnWorldSetup(draft)
  const ownWorld = custom || draft.customEditing
  const role = ownWorld ? ownWorldRole(draft) : d.role
  const [open, setOpen] = useState(editRole ? 'person' : custom ? 'world' : '')
  const [invalid, setInvalid] = useState('')
  const [guide, setGuide] = useState<WorldHelpTarget | null>(null)
  const [choosingCharacter, setChoosingCharacter] = useState(false)
  const guideButton = useRef<HTMLButtonElement>(null)
  const copyButton = useRef<HTMLButtonElement>(null)
  const focused = useRef<HTMLElement | null>(null)
  useEffect(() => {
    const reveal = () => { if (focused.current && document.activeElement === focused.current) showEditorField(focused.current) }
    window.visualViewport?.addEventListener('resize', reveal)
    return () => window.visualViewport?.removeEventListener('resize', reveal)
  }, [])

  // Temporary editor visits use their own history entry; the draft's mode stays intact.
  const quickSettings = () => {
    if (nav.current === '05-12_world_advanced' && nav.canBack) { nav.back(); return }
    setWorldDraft({ ...draft, advanced: false })
    nav.replace(d.templateId === null || draft.editingId ? '05-7b_own_world' : '01-4_channel')
  }
  const update = updateWorldDraft
  const setRole = (role: string) => setWorldDraft({ ...draft, ownRoleEdited: true, definition: { ...d, role } })
  const helpButton = (label: string, target: WorldHelpTarget) => ownWorld ? <button type="button" className="world-help-button" aria-label={`Справка о поле «${label}»`} aria-haspopup="dialog"
    onClick={event => { guideButton.current = event.currentTarget; setGuide(target) }}>?</button> : null
  const closeGuide = () => {
    setGuide(null)
    requestAnimationFrame(() => guideButton.current?.focus({ preventScroll: true }))
  }
  const closePicker = () => {
    setChoosingCharacter(false)
    requestAnimationFrame(() => copyButton.current?.focus({ preventScroll: true }))
  }
  const copyCharacter = (world: WorldDocument) => {
    setWorldDraft({ ...draft, ownRoleEdited: true, definition: { ...d, name: world.name, avatar: world.avatar, role: world.role } })
    closePicker()
  }
  const personRequired = !custom || d.stars.length < 2
  const requiredFields = [
    { section: 'world', id: 'title', value: d.title, message: 'Введи название мира.' },
    { section: 'world', id: 'description', value: d.description, message: 'Опиши, о чём этот мир.' },
    ...(personRequired ? [
      { section: 'person', id: 'name', value: d.name, message: 'Введи имя персонажа.' },
      { section: 'person', id: 'role', value: role, message: 'Укажи, кем будешь играть.' },
      { section: 'person', id: 'avatar', value: d.avatar, message: 'Задай ник персонажа.' },
    ] : []),
    { section: 'heroes', id: 'add-hero', value: d.stars.length ? 'герой добавлен' : '', message: 'Добавь хотя бы одного героя.' },
    ...d.stars.flatMap(star => [
      { section: 'heroes', id: starField(star.id, 'name'), value: star.name, message: 'Введи имя героя.' },
      { section: 'heroes', id: starField(star.id, 'role'), value: star.role, message: 'Укажи, чем занимается герой.' },
      { section: 'heroes', id: starField(star.id, 'about'), value: star.about, message: 'Добавь пару слов о характере или прошлом героя.' },
      { section: 'heroes', id: starField(star.id, 'voice'), value: star.voice, message: 'Опиши, как общается герой.' },
    ]),
    ...(ownWorld ? [] : [{ section: 'mood', id: 'voice', value: d.voice, message: 'Опиши стиль подсказок для постов.' }]),
    ...d.rules.flatMap((rule, index) => rule.locked ? [] : [
      { section: 'rules', id: `rule-${index}`, value: rule.text, message: 'Заполни правило мира или убери его.' },
    ]),
  ]
  const missingFields = requiredFields.filter(field => !field.value.trim())
  const missing = missingFields[0]
  const missingSections = new Set(missingFields.map(field => field.section)).size
  const issue = missingFields.find(field => field.id === invalid)
  const fieldsLeft = (count: number) => `Ещё ${count} ${plural(count, 'поле', 'поля', 'полей')}`
  const focusMissing = (field: (typeof requiredFields)[number]) => {
    // Open the section and focus during the click so iOS can keep the keyboard available.
    flushSync(() => { setOpen(field.section); setInvalid(field.id) })
    const control = document.getElementById(field.id)
    if (!control) return
    for (let parent = control.parentElement; parent; parent = parent.parentElement) if (parent instanceof HTMLDetailsElement) parent.open = true
    focused.current = control
    control.focus({ preventScroll: true })
    requestAnimationFrame(() => showEditorField(control))
  }
  const field = (props: Field) => <WorldField {...props} error={issue?.id === props.id ? issue.message : undefined}
    infoButton={props.info ? helpButton(props.label, props.info) : undefined} />
  const text = (key: keyof WorldDefinition, label: string, props: Partial<Field> = {}) =>
    field({ id: key, label, value: String(d[key]), onChange: value => update({ [key]: value }), ...props })

  const section = (id: string, title: string, subtitle: string, children: ReactNode) => {
    const count = missingFields.filter(field => field.section === id).length
    const required = requiredFields.some(field => field.section === id)
    const status = count ? (id === 'heroes' && !d.stars.length ? 'Добавь героя' : fieldsLeft(count)) : required ? 'Готово' : 'По желанию'
    return <section className="editor-section">
      <button className="editor-section-head" aria-expanded={open === id} aria-controls={`editor-${id}`} onClick={() => setOpen(open === id ? '' : id)}>
        <span><b>{title}</b><span className="small muted">{subtitle}</span>
          {custom ? <span className={`small ${count ? 'editor-incomplete' : required ? 'up' : 'muted'}`}>{status}</span>
            : issue?.section === id && open !== id && <span className="small down">{issue.message}</span>}
        </span><span aria-hidden>{open === id ? '−' : '＋'}</span>
      </button>
      {open === id && <div className="editor-section-body" id={`editor-${id}`}>{children}</div>}
    </section>
  }
  const changeStar = (id: string, patch: Partial<WorldDefinition['stars'][number]>) => update({ stars: d.stars.map(s => s.id === id ? { ...s, ...patch } : s) })
  const addHero = () => {
    const id = crypto.randomUUID().replaceAll('-', '')
    const avatarSeed = custom ? '' : crypto.randomUUID()
    flushSync(() => update({ stars: [...d.stars, { id, name: '', role: '', about: '', voice: '', avatarSeed, trait: 1 }] }))
    const control = document.getElementById(starField(id, 'name'))
    const detail = control?.closest('details')
    if (detail) detail.open = true
    control?.focus()
  }
  return <div className="screen no-tab advanced-world"><TopBar title="Настрой свой мир" onBack={saving ? undefined : nav.back} /><div className="pad">
    {!ownWorld && <button className="text-link" onClick={quickSettings}>← Быстрые настройки</button>}
    {custom && <div className="world-editor-progress">
      <div role="status">
        <b>{missingSections ? `Осталось заполнить ${missingSections} ${plural(missingSections, 'раздел', 'раздела', 'разделов')}` : 'Можно выбрать персонажа'}</b>
      </div>
      {missing && <button type="button" className="text-link small" onClick={() => focusMissing(missing)}>Что заполнить</button>}
    </div>}
    <WorldCardPreview title={d.title.trim() || 'Твой мир'} emoji={d.emoji.trim() || '✨'}
      description={(d.summary !== 'Твоя история' && d.summary.trim()) || d.description.trim() || 'Здесь появится описание мира.'} />
    {section('world', 'Мир и начало истории', 'Завязка и оформление карточки', <>
      <div className="world-editor-title">{text('emoji', 'Значок', { maxLength: 8, info: { topic: 'emoji' } })}{text('title', 'Название мира', { maxLength: 120, required: true, info: { topic: 'title' } })}</div>
      {text('description', 'О чём этот мир', { rows: 3, required: true, help: ownWorld ? 'Опиши место и что в нём происходит.' : 'Что здесь необычного и какие истории хочется прожить?', info: { topic: 'description' } })}
      {text('firstContext', 'С чего начинается история', { rows: 3, help: ownWorld ? 'Напиши, что случилось перед первым постом.' : 'Если оставить пустым, в канале появится описание мира.', info: { topic: 'firstContext' } })}
      <details className="editor-extra"><summary>Дополнить карточку и завязку</summary>
        {text('summary', 'Подпись на карточке', { maxLength: 200, info: { topic: 'summary' } })}
        {text('where', 'Где всё происходит', { maxLength: 200, info: { topic: 'where' } })}
        {text('firstPrompt', 'Подсказка для первого поста', { rows: 2, help: 'Задай вопрос, с которого можно начать пост.', info: { topic: 'firstPrompt' } })}
      </details>
    </>)}
    {section('person', 'Твой персонаж', d.name ? [d.name, role].filter(Boolean).join(' · ') : 'Имя, роль и портрет', <>
      {ownWorld && <p className="small muted">Характер твоего персонажа раскрывается в твоих постах и ответах.</p>}
      {custom && player && <div className="world-editor-action">
        <button ref={copyButton} type="button" className="btn secondary" aria-haspopup="dialog" onClick={() => setChoosingCharacter(true)}>Скопировать персонажа</button>
        {helpButton('Скопировать персонажа', { topic: 'copyPersona' })}
      </div>}
      {text('name', 'Имя персонажа', { maxLength: 80, required: personRequired, info: { topic: 'name' } })}
      {text('role', 'Кто ты в этой истории', { maxLength: 80, required: personRequired, placeholder: 'Например, репортёр', info: { topic: 'role' },
        ...(ownWorld ? { value: role === d.role.trim() ? d.role : role, onChange: setRole } : {}) })}
      {text('avatar', 'Ник персонажа', { maxLength: 80, required: personRequired, info: { topic: 'avatar' } })}
      <div className="editor-preview"><Avatar seed={d.avatar || 'new-player'} avatarStyle={d.avatarStyle} profile={{ address: draft.address, ageBand: draft.ageBand, personalizeAvatar: draft.personalizeAvatar }} size={48} /><div><b>{d.name || 'Твой персонаж'}</b><p className="small muted">{role}</p></div></div>
      <details className="editor-extra"><summary>Портрет и обращение</summary>
        <AvatarPersonalization enabled={draft.personalizeAvatar !== false} onChange={personalizeAvatar => setWorldDraft({ ...draft, personalizeAvatar })}
          info={helpButton('Персонализация аватарки', { topic: 'personalization' })} />
        {ownWorld && <div className="world-field-label"><span className="label">Стиль портретов</span>{helpButton('Стиль портретов', { topic: 'avatarStyle' })}</div>}
        <Segmented wrap value={d.avatarStyle} onChange={avatarStyle => update({ avatarStyle })} options={['lorelei', 'notionists', 'pixelArt', 'thumbs', 'shapes', 'adventurer'].map((id, i) => ({ id, label: ['портрет', 'скетч', 'пиксели', 'мини', 'формы', 'приключения'][i] }))} />
        <div className="world-field-label"><span className="label">Как к тебе обращаться</span>{helpButton('Как к тебе обращаться', { topic: 'address' })}</div>
        <Segmented value={draft.address} onChange={(address: Address) => setWorldDraft({ ...draft, address })} options={[{ id: 'she', label: 'она' }, { id: 'he', label: 'он' }, { id: 'nick', label: 'по нику' }]} />
      </details>
      {!ownWorld && <StringList title="Варианты роли" help="Добавь роли, из которых можно будет выбрать перед входом." values={d.roles} maxLength={80} onChange={roles => update({ roles })} />}
    </>)}
    {section('heroes', custom ? 'Герои вокруг тебя' : 'Герои вокруг тебя *', 'Пишут в ленте и отвечают на посты', <>
      <p className="small muted">Добавь хотя бы одного героя. Твой персонаж сюда не входит.</p>
      {d.stars.map(star => {
        const count = missingFields.filter(field => field.id.startsWith(starField(star.id, ''))).length
        return <details className="editor-extra hero-editor" key={star.id}>
          <summary><Avatar seed={star.avatarSeed || star.id} avatarStyle={d.avatarStyle} mode="default" size={34} /><span>{star.name || 'Новый герой'}<span className="small muted">{star.role || 'Добавь занятие героя'}</span>{custom && <span className={`small ${count ? 'editor-incomplete' : 'up'}`}>{count ? fieldsLeft(count) : 'Готово'}</span>}</span></summary>
          {field({ id: starField(star.id, 'name'), label: 'Имя героя', value: star.name, onChange: name => changeStar(star.id, { name }), maxLength: 80, required: true, info: { topic: 'heroName', star } })}
          {field({ id: starField(star.id, 'role'), label: 'Чем занимается герой', value: star.role, onChange: role => changeStar(star.id, { role }), maxLength: 80, required: true, info: { topic: 'heroRole', star } })}
          {field({ id: starField(star.id, 'about'), label: 'Характер и история', value: star.about, onChange: about => changeStar(star.id, { about }), rows: 3, required: true, placeholder: ownWorld ? 'Что важно герою и что уже с ним случилось?' : 'Что для героя важно? Что он скрывает?', info: { topic: 'heroAbout', star } })}
          {field({ id: starField(star.id, 'voice'), label: 'Как герой общается', value: star.voice, onChange: voice => changeStar(star.id, { voice }), rows: 2, required: true, placeholder: 'Например, сдержанно, с сухим юмором', info: { topic: 'heroVoice', star } })}
          <details className="editor-extra"><summary>Ник и главная черта</summary>
            {field({ id: starField(star.id, 'nickname'), label: 'Ник героя', value: heroHandle(star.avatarSeed) ? star.avatarSeed : '', onChange: avatarSeed => changeStar(star.id, { avatarSeed }), maxLength: 80, info: { topic: 'heroNickname', star } })}
            <div className="world-field-label"><label className="label" htmlFor={starField(star.id, 'trait')}>Главная черта</label>{helpButton('Главная черта', { topic: 'trait', star })}</div>
            <select id={starField(star.id, 'trait')} value={star.trait} onChange={e => changeStar(star.id, { trait: Number(e.target.value) })}>
              {heroTraits.map((label, i) => <option key={label} value={i}>{label}</option>)}
            </select>
          </details>
          <button className="text-link down" onClick={() => update({ stars: d.stars.filter(s => s.id !== star.id) })}>Убрать героя</button>
        </details>
      })}
      <div className="world-field">
        <div className="world-editor-action">
          <button id="add-hero" className="btn secondary" aria-describedby={issue?.id === 'add-hero' ? 'add-hero-help' : undefined} onClick={addHero}>＋ Добавить героя</button>
          {helpButton('Герои мира', { topic: 'heroes', star: d.stars[0] })}
        </div>
        {issue?.id === 'add-hero' && <p id="add-hero-help" className="small down" role="alert">{issue.message}</p>}
      </div>
    </>)}
    {section('mood', 'Настроение истории', ownWorld ? 'Сложность и драма' : 'Сложность, драма и стиль подсказок', <>
      {!ownWorld && text('voice', 'Стиль подсказок для постов', { rows: 2, maxLength: 200, required: true, placeholder: 'Например, с любопытством и добрым юмором' })}
      <DifficultyPicker definition={d} info={helpButton('Сложность', { topic: 'difficulty' })} />
      <DramaPicker definition={d} withDifficulty={false} info={helpButton('Градус драмы', { topic: 'drama' })} />
      {!ownWorld && <StringList title="Варианты стиля" help="Готовые варианты для быстрого выбора стиля подсказок." values={d.voices} onChange={voices => update({ voices })} />}
    </>)}
    {section('rules', 'Особенности мира', 'Что здесь возможно и как всё устроено', <>
      <div className="world-field-label"><p className="small muted">Например: «Магия работает только ночью».</p>
        {helpButton('Особенности мира', { topic: 'rule', value: d.rules.find(rule => !rule.locked && rule.text.trim())?.text })}
      </div>
      {d.rules.map((rule, i) => !rule.locked && <div className="editor-rule" key={i}>
        {field({ id: `rule-${i}`, label: `Особенность ${i + 1}`, value: rule.text, onChange: text => update({ rules: d.rules.map((r, j) => i === j ? { ...r, text } : r) }), rows: 2, required: true, info: { topic: 'rule', value: rule.text } })}
        <button className="text-link" onClick={() => update({ rules: d.rules.filter((_, j) => i !== j) })}>Убрать особенность</button>
      </div>)}
      <button className="chip" onClick={() => update({ rules: [...d.rules, { text: '', locked: false }] })}>＋ Добавить особенность</button>
    </>)}
    {section('extra', 'Дополнительные настройки игры', 'Идеи постов и оформление достижений', <>
      <StringList title="Идеи для постов" help="Начала фраз, которые игрок может дописать." values={d.postIdeas} onChange={postIdeas => update({ postIdeas })} info={(label, value) => helpButton(label, { topic: 'postIdea', value })} />
      <StringList title="Примеры постов" help={ownWorld ? 'Образцы текста для героев.' : 'Образцы текста для этого мира.'} values={d.demoPosts} onChange={demoPosts => update({ demoPosts })} info={(label, value) => helpButton(label, { topic: 'demoPost', value })} />
      <details className="editor-extra"><summary>Достижения коллекции · {d.achievements.length}</summary>
        <div className="world-field-label"><p className="small muted">Оформление карточек в коллекции.</p>{helpButton('Достижения коллекции', { topic: 'achievements', achievement: d.achievements[0] })}</div>
        {d.achievements.map((a, i) => <div className="editor-rule" key={a.id}>
          <div className="achievement-preview">{a.emoji} {a.title || 'Новое достижение'}</div>
          {field({ id: `achievement-title-${a.id}`, label: 'Название достижения', value: a.title, onChange: title => update({ achievements: d.achievements.map((v, j) => i === j ? { ...v, title } : v) }), info: { topic: 'achievementTitle', achievement: a } })}
          {field({ id: `achievement-emoji-${a.id}`, label: 'Значок достижения', value: a.emoji, onChange: emoji => update({ achievements: d.achievements.map((v, j) => i === j ? { ...v, emoji } : v) }), maxLength: 8, info: { topic: 'achievementEmoji', achievement: a } })}
          <button className="text-link" onClick={() => update({ achievements: d.achievements.filter((_, j) => i !== j) })}>Убрать достижение</button>
        </div>)}
        <button className="chip" onClick={() => update({ achievements: [...d.achievements, { id: crypto.randomUUID(), emoji: '⭐', title: '', open: false }] })}>＋ Добавить достижение</button>
      </details>
    </>)}
    {!ownWorld && <AgeRuleNote teen={draft.group !== '18+' || draft.ageBand === '14-17'} />}
    {error && <p className="down" role="alert">{error}</p>}
    {uncertain && <Button secondary onClick={onCheck}>Проверить список миров</Button>}
    <Button disabled={saving || uncertain} onClick={() => missing ? focusMissing(missing) : onSave()}>{saving ? 'Сохраняем…' : custom ? 'Выбрать персонажа' : draft.previewOnly ? 'Посмотреть демо' : draft.editingId ? 'Сохранить изменения' : 'Начать историю'}</Button>
  </div>{guide && <WorldEditorHelp target={guide} draft={draft} onClose={closeGuide} />}
    {choosingCharacter && <WorldCharacterPicker currentWorldId={player?.worldInstanceId} onChoose={copyCharacter} onClose={closePicker} />}
  </div>
}
