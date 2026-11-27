import { useEffect, useRef, useState, type ReactNode } from 'react'
import { flushSync } from 'react-dom'
import { heroHandle } from '../app/handle'
import { useNav } from '../app/nav'
import { useStore } from '../app/store'
import { isOwnWorldSetup, setWorldDraft, updateWorldDraft, type WorldDraft } from '../app/worldDraft'
import type { Address, WorldDefinition } from '../api/types'
import { Avatar, Button, Segmented, TopBar, plural } from '../ui'
import { AvatarPersonalization } from '../ui/AvatarPersonalization'
import { DifficultyPicker, DramaPicker } from './worldQuick'

export function AgeRuleNote({ teen = true }: { teen?: boolean }) {
  return <div className="age-rule-note"><b>🔒 Ограничения контента</b><p>{teen ? 'Без взрослой сексуализированной подачи. Дружба и романтические симпатии допустимы.' : 'Чувствительные темы можно обсуждать без откровенных подробностей.'}</p><p className="small muted">Травля и откровенный контент запрещены для всех.</p></div>
}

function StringList({ title, help, values, maxLength = 2000, onChange }: { title: string; help: string; values: string[]; maxLength?: number; onChange: (values: string[]) => void }) {
  return <details className="editor-extra"><summary>{title} · {values.length}</summary><p className="small muted">{help}</p>{values.map((value, i) => <div className="editor-list-row" key={i}><input aria-label={`${title} ${i + 1}`} value={value} maxLength={maxLength} onChange={e => onChange(values.map((v, j) => j === i ? e.target.value : v))} /><button className="text-link" aria-label={`Удалить: ${title} ${i + 1}`} onClick={() => onChange(values.filter((_, j) => j !== i))}>Убрать</button></div>)}<button className="chip" onClick={() => onChange([...values, ''])}>＋ Добавить</button></details>
}

type Field = {
  id: string; label: string; value: string; onChange: (value: string) => void
  help?: string; placeholder?: string; rows?: number; maxLength?: number; required?: boolean; error?: string
}
function WorldField({ id, label, value, onChange, help, placeholder, rows = 1, maxLength = 2000, required, error }: Field) {
  const props = {
    id, value, maxLength, placeholder, required,
    'aria-invalid': error ? true : undefined,
    'aria-describedby': help || error ? `${id}-help` : undefined,
    onChange: (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => onChange(e.currentTarget.value),
  }
  return <div className="world-field">
    <label className="label" htmlFor={id}>{label}{required && <span className="down" aria-hidden="true"> *</span>}</label>
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
  const [open, setOpen] = useState(editRole ? 'person' : custom ? 'world' : '')
  const [invalid, setInvalid] = useState('')
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
  const personRequired = !custom || d.stars.length < 2
  const roleRequired = personRequired && (!custom || !d.roles.some(role => role.trim()))
  const requiredFields = [
    { section: 'world', id: 'title', value: d.title, message: 'Введи название мира.' },
    { section: 'world', id: 'description', value: d.description, message: 'Опиши, о чём этот мир.' },
    ...(personRequired ? [
      { section: 'person', id: 'name', value: d.name, message: 'Введи имя персонажа.' },
      ...(roleRequired ? [{ section: 'person', id: 'role', value: d.role, message: 'Укажи, кем будешь играть.' }] : []),
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
  const field = (props: Field) => <WorldField {...props} error={issue?.id === props.id ? issue.message : undefined} />
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
    {!custom && <button className="text-link" onClick={quickSettings}>← Быстрые настройки</button>}
    {custom && <div className="world-editor-progress">
      <div role="status">
        <b>{missingSections ? `Осталось заполнить ${missingSections} ${plural(missingSections, 'раздел', 'раздела', 'разделов')}` : 'Можно выбрать персонажа'}</b>
      </div>
      {missing && <button type="button" className="text-link small" onClick={() => focusMissing(missing)}>Что заполнить</button>}
    </div>}
    <div className="card world-editor-preview" aria-label="Карточка мира">
      <span aria-hidden="true">{d.emoji.trim() || '✨'}</span>
      <div><b>{d.title.trim() || 'Твой мир'}</b><p className="small muted">{(d.summary !== 'Твоя история' && d.summary.trim()) || d.description.trim() || 'Здесь появится описание мира.'}</p></div>
    </div>
    {section('world', 'Мир и начало истории', 'Завязка и оформление карточки', <>
      <div className="world-editor-title">{text('emoji', 'Значок', { maxLength: 8 })}{text('title', 'Название мира', { maxLength: 120, required: true })}</div>
      {text('description', 'О чём этот мир', { rows: 3, required: true, help: 'Что здесь необычного и какие истории хочется прожить?' })}
      {text('firstContext', 'С чего начинается история', { rows: 3, help: 'Если оставить пустым, в канале появится описание мира.' })}
      <details className="editor-extra"><summary>Дополнить карточку и завязку</summary>
        {text('summary', 'Подпись на карточке', { maxLength: 200 })}
        {text('where', 'Где всё происходит', { maxLength: 200 })}
        {text('firstPrompt', 'Подсказка для первого поста', { rows: 2, help: 'Вопрос, который поможет начать писать.' })}
      </details>
    </>)}
    {section('person', 'Твой персонаж', d.name ? [d.name, d.role].filter(Boolean).join(' · ') : 'Имя, роль и портрет', <>
      {custom && player && <Button secondary onClick={() => update({ name: player.name, avatar: player.handle, role: player.role })}>Взять персонажа из текущего мира</Button>}
      {text('name', 'Имя персонажа', { maxLength: 80, required: personRequired })}
      {text('role', 'Кто ты в этой истории', { maxLength: 80, required: roleRequired, placeholder: 'Например, репортёр' })}
      {text('avatar', 'Ник персонажа', { maxLength: 80, required: personRequired })}
      <div className="editor-preview"><Avatar seed={d.avatar || 'new-player'} avatarStyle={d.avatarStyle} profile={{ address: draft.address, ageBand: draft.ageBand, personalizeAvatar: draft.personalizeAvatar }} size={48} /><div><b>{d.name || 'Твой персонаж'}</b><p className="small muted">{d.role}</p></div></div>
      <details className="editor-extra"><summary>Портрет и обращение</summary>
        <AvatarPersonalization enabled={draft.personalizeAvatar !== false} onChange={personalizeAvatar => setWorldDraft({ ...draft, personalizeAvatar })} />
        <Segmented wrap value={d.avatarStyle} onChange={avatarStyle => update({ avatarStyle })} options={['lorelei', 'notionists', 'pixelArt', 'thumbs', 'shapes', 'adventurer'].map((id, i) => ({ id, label: ['портрет', 'скетч', 'пиксели', 'мини', 'формы', 'приключения'][i] }))} />
        <p className="label">Как к тебе обращаться</p>
        <Segmented value={draft.address} onChange={(address: Address) => setWorldDraft({ ...draft, address })} options={[{ id: 'she', label: 'она' }, { id: 'he', label: 'он' }, { id: 'nick', label: 'по нику' }]} />
      </details>
      <StringList title="Варианты роли" help="Добавь роли, из которых можно будет выбрать перед входом." values={d.roles} maxLength={80} onChange={roles => update({ roles })} />
    </>)}
    {section('heroes', custom ? 'Герои вокруг тебя' : 'Герои вокруг тебя *', 'Пишут в ленте и отвечают на посты', <>
      <p className="small muted">Добавь хотя бы одного героя. Твой персонаж сюда не входит.</p>
      {d.stars.map(star => {
        const count = missingFields.filter(field => field.id.startsWith(starField(star.id, ''))).length
        return <details className="editor-extra hero-editor" key={star.id}>
          <summary><Avatar seed={star.avatarSeed || star.id} avatarStyle={d.avatarStyle} mode="default" size={34} /><span>{star.name || 'Новый герой'}<span className="small muted">{star.role || 'Добавь занятие героя'}</span>{custom && <span className={`small ${count ? 'editor-incomplete' : 'up'}`}>{count ? fieldsLeft(count) : 'Готово'}</span>}</span></summary>
          {field({ id: starField(star.id, 'name'), label: 'Имя героя', value: star.name, onChange: name => changeStar(star.id, { name }), maxLength: 80, required: true })}
          {field({ id: starField(star.id, 'role'), label: 'Чем занимается герой', value: star.role, onChange: role => changeStar(star.id, { role }), maxLength: 80, required: true })}
          {field({ id: starField(star.id, 'about'), label: 'Характер и история', value: star.about, onChange: about => changeStar(star.id, { about }), rows: 3, required: true, placeholder: 'Что для героя важно? Что он скрывает?' })}
          {field({ id: starField(star.id, 'voice'), label: 'Как герой общается', value: star.voice, onChange: voice => changeStar(star.id, { voice }), rows: 2, required: true, placeholder: 'Например, сдержанно, с сухим юмором' })}
          <details className="editor-extra"><summary>Ник и главная черта</summary>
            {field({ id: starField(star.id, 'nickname'), label: 'Ник героя', value: heroHandle(star.avatarSeed) ? star.avatarSeed : '', onChange: avatarSeed => changeStar(star.id, { avatarSeed }), maxLength: 80 })}
            <label className="label">Главная черта<select value={star.trait} onChange={e => changeStar(star.id, { trait: Number(e.target.value) })}>{['Смелость', 'Поддержка', 'Загадка', 'Воображение', 'Любопытство', 'Энергия', 'Ирония и расчёт', 'Сухой юмор', 'Самоирония', 'Азарт', 'Внимание'].map((label, i) => <option key={label} value={i}>{label}</option>)}</select></label>
          </details>
          <button className="text-link down" onClick={() => update({ stars: d.stars.filter(s => s.id !== star.id) })}>Убрать героя</button>
        </details>
      })}
      <div className="world-field">
        <button id="add-hero" className="btn secondary" aria-describedby={issue?.id === 'add-hero' ? 'add-hero-help' : undefined} onClick={addHero}>＋ Добавить героя</button>
        {issue?.id === 'add-hero' && <p id="add-hero-help" className="small down" role="alert">{issue.message}</p>}
      </div>
    </>)}
    {section('mood', 'Настроение истории', ownWorld ? 'Сложность и драма' : 'Сложность, драма и стиль подсказок', <>
      {!ownWorld && text('voice', 'Стиль подсказок для постов', { rows: 2, maxLength: 200, required: true, placeholder: 'Например, с любопытством и добрым юмором' })}
      <DifficultyPicker definition={d} />
      <DramaPicker definition={d} withDifficulty={false} />
      {!ownWorld && <StringList title="Варианты стиля" help="Готовые варианты для быстрого выбора стиля подсказок." values={d.voices} onChange={voices => update({ voices })} />}
    </>)}
    {section('rules', 'Особенности мира', 'Что здесь возможно и как всё устроено', <>
      <p className="small muted">Например: «Магия работает только ночью». Эти правила влияют на поведение героев.</p>
      {d.rules.map((rule, i) => !rule.locked && <div className="editor-rule" key={i}>
        {field({ id: `rule-${i}`, label: `Особенность ${i + 1}`, value: rule.text, onChange: text => update({ rules: d.rules.map((r, j) => i === j ? { ...r, text } : r) }), rows: 2, required: true })}
        <button className="text-link" onClick={() => update({ rules: d.rules.filter((_, j) => i !== j) })}>Убрать особенность</button>
      </div>)}
      <button className="chip" onClick={() => update({ rules: [...d.rules, { text: '', locked: false }] })}>＋ Добавить особенность</button>
    </>)}
    {section('extra', 'Дополнительные настройки игры', 'Идеи постов и оформление достижений', <>
      <StringList title="Идеи для постов" help="Начала фраз, которые игрок может дописать." values={d.postIdeas} onChange={postIdeas => update({ postIdeas })} />
      <StringList title="Примеры постов" help="Образцы текста для этого мира." values={d.demoPosts} onChange={demoPosts => update({ demoPosts })} />
      <details className="editor-extra"><summary>Достижения коллекции · {d.achievements.length}</summary><p className="small muted">Оформление наград в коллекции. Условия получения и прогресс сохраняются.</p>
        {d.achievements.map((a, i) => <div className="editor-rule" key={a.id}>
          <div className="achievement-preview">{a.emoji} {a.title || 'Новое достижение'}</div>
          {field({ id: `achievement-title-${a.id}`, label: 'Название достижения', value: a.title, onChange: title => update({ achievements: d.achievements.map((v, j) => i === j ? { ...v, title } : v) }) })}
          {field({ id: `achievement-emoji-${a.id}`, label: 'Значок достижения', value: a.emoji, onChange: emoji => update({ achievements: d.achievements.map((v, j) => i === j ? { ...v, emoji } : v) }), maxLength: 8 })}
          <button className="text-link" onClick={() => update({ achievements: d.achievements.filter((_, j) => i !== j) })}>Убрать достижение</button>
        </div>)}
        <button className="chip" onClick={() => update({ achievements: [...d.achievements, { id: crypto.randomUUID(), emoji: '⭐', title: '', open: false }] })}>＋ Добавить достижение</button>
      </details>
    </>)}
    {!ownWorld && <AgeRuleNote teen={draft.group !== '18+' || draft.ageBand === '14-17'} />}
    {error && <p className="down" role="alert">{error}</p>}
    {uncertain && <Button secondary onClick={onCheck}>Проверить список миров</Button>}
    <Button disabled={saving || uncertain} onClick={() => missing ? focusMissing(missing) : onSave()}>{saving ? 'Сохраняем…' : custom ? 'Выбрать персонажа' : draft.previewOnly ? 'Посмотреть демо' : draft.editingId ? 'Сохранить изменения' : 'Начать историю'}</Button>
  </div></div>
}
