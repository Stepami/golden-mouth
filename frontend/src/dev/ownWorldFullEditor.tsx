import { useEffect, useState, type ReactNode } from 'react'
import { flushSync } from 'react-dom'
import type { WorldDefinition } from '../api/types'
import { blankWorld } from '../app/worldDraft'
import { dramaOptions } from '../app/worldPrototype'
import { avatarUri } from '../ui/avatarRenderer'
import { VoiceCoverEditor, type VoiceCover } from './ownWorldCover'
import { exampleWorldDetails } from './ownWorldExample'
import './ownWorldFullEditor.css'

type VoiceSummary = {
  title: string
  opening: string
  people: string
  rules: string
}

type VoiceDraft = WorldDefinition & { people: string; portraitSeeds: Record<string, string> }
type Hero = WorldDefinition['stars'][number]
const traits = ['Смелость', 'Поддержка', 'Загадка', 'Воображение', 'Любопытство', 'Энергия', 'Ирония и расчёт', 'Сухой юмор', 'Самоирония', 'Азарт', 'Внимание']
const achievementCards = [
  ['star_card', '👑', 'Карточка звезды'],
  ['star_story', '💬', 'История звезды'],
  ['first_viral', '🌊', 'Первый залёт'],
  ['hate_survived', '🌩', 'Хейт пережит'],
  ['collaboration', '🎬', 'Коллаб'],
  ['ten_thousand', '⭐', '10k'],
  ['skill_mastery', '🎓', 'Мастерство'],
  ['profile_frame', '🖼', 'Рамка профиля'],
  ['secret', '❔', 'Тайная'],
  ['duet', '🤝', 'Дуэт'],
  ['challenge', '⚔️', 'Челлендж'],
  ['rematch', '🔁', 'Реванш'],
]

export function voiceWorldIssue(draft: WorldDefinition): string | null {
  if (!draft.name.trim() || !draft.avatar.trim() || !draft.role.trim()) return 'Укажи имя, ник и роль своего персонажа.'
  if (!draft.title.trim() || !draft.description.trim()) return 'Укажи название и описание мира.'
  if (!draft.voice.trim()) return 'Укажи стиль подсказок для постов.'
  if (!draft.stars.length) return 'Добавь хотя бы одного героя, который будет отвечать на посты.'
  if (draft.stars.some(hero => !hero.name.trim() || !hero.role.trim() || !hero.about.trim() || !hero.voice.trim())) {
    return 'Укажи имя, роль, характер и манеру общения каждого героя.'
  }
  if (draft.rules.some(rule => !rule.text.trim())) return 'Заполни или убери пустые особенности мира.'
  return null
}

export function voicePortraitSeed(draft: VoiceDraft, personId: string, fallback: string) {
  return draft.portraitSeeds[personId] || fallback || personId
}

function rulesText(rules: VoiceDraft['rules']) {
  return rules.filter(rule => !rule.locked).map(rule => rule.text).join('\n')
}

export function voiceDraftSummary(draft: VoiceDraft): VoiceSummary {
  return { title: draft.title, opening: draft.firstContext, people: draft.people, rules: rulesText(draft.rules) }
}

function applyQuickEdits(draft: VoiceDraft, summary: VoiceSummary): VoiceDraft {
  const rules = rulesText(draft.rules) === summary.rules ? draft.rules : [
    ...draft.rules.filter(rule => rule.locked),
    ...summary.rules.split('\n').filter(rule => rule.trim()).map(text => ({ text, locked: false })),
  ]
  return { ...draft, title: summary.title, firstContext: summary.opening, rules }
}

function exampleWorld(summary: VoiceSummary): VoiceDraft {
  return applyQuickEdits({
    ...blankWorld(),
    ...exampleWorldDetails,
    people: summary.people,
    portraitSeeds: {},
    achievements: achievementCards.map(([id, emoji, title]) => ({ id, emoji, title, open: false })),
  }, summary)
}

export function useVoiceFullDraft(summary: VoiceSummary) {
  const [draft, setDraft] = useState(() => exampleWorld(summary))
  return { draft, setDraft, previewDraft: applyQuickEdits(draft, summary), openEditor: () => setDraft(previous => applyQuickEdits(previous, summary)) }
}

function Portrait({ seed, style, name, size = 42 }: { seed: string; style: string; name: string; size?: number }) {
  const key = `${style}:${seed}`
  const [portrait, setPortrait] = useState<{ key: string; uri: string }>()
  useEffect(() => {
    let active = true
    void avatarUri(seed, undefined, undefined, style)
      .then(uri => { if (active) setPortrait({ key, uri }) })
      .catch(() => { /* The initial remains visible if a style asset fails to load. */ })
    return () => { active = false }
  }, [key, seed, style])

  if (portrait?.key === key) return <img className="voice-portrait" width={size} height={size} src={portrait.uri} alt="" />
  return <span className="voice-portrait voice-portrait-placeholder" style={{ width: size, height: size }} aria-hidden="true">{name.trim().slice(0, 1) || '✦'}</span>
}

function peopleOf(draft: VoiceDraft) {
  return [{ id: 'player', name: draft.name, role: draft.role, avatarSeed: draft.avatar }, ...draft.stars]
}

function rosterText(draft: VoiceDraft) {
  return peopleOf(draft)
    .filter(person => person.name.trim() || person.role.trim())
    .map(person => [person.name.trim(), person.role.trim()].filter(Boolean).join(' — '))
    .join('\n')
}

function rerollPortraitSeeds(draft: VoiceDraft, id: string) {
  return { ...draft.portraitSeeds, [id]: crypto.randomUUID() }
}

export function VoicePortraits({ draft }: { draft: VoiceDraft }) {
  return <ul className="voice-portraits" aria-label="Персонажи мира">
    {peopleOf(draft).map(person => <li key={person.id}>
      <Portrait seed={voicePortraitSeed(draft, person.id, person.avatarSeed)} style={draft.avatarStyle} name={person.name} size={36} />
      <span><b>{person.name || 'Без имени'}</b><small>{person.role}</small></span>
    </li>)}
  </ul>
}

export function VoiceQuickCharacters({ draft, onChange }: { draft: VoiceDraft; onChange: (draft: VoiceDraft) => void }) {
  const [selectedId, setSelectedId] = useState('')
  const people = peopleOf(draft)
  const selected = people.find(person => person.id === selectedId)
  function editPerson(change: { name: string } | { role: string }) {
    const next = selectedId === 'player'
      ? { ...draft, ...change }
      : { ...draft, stars: draft.stars.map(hero => hero.id === selectedId ? { ...hero, ...change } : hero) }
    onChange({ ...next, people: rosterText(next) })
  }
  return <section className="voice-quick-characters" aria-label="Персонажи мира">
    <ul className="voice-quick-character-list">
      {people.map(person => <li key={person.id}>
        <button type="button" className="voice-quick-character" aria-expanded={person.id === selectedId}
          aria-label={`Редактировать ${person.name.trim() || (person.id === 'player' ? 'своего персонажа' : 'героя')}`}
          onClick={() => setSelectedId(person.id === selectedId ? '' : person.id)}>
          <Portrait seed={voicePortraitSeed(draft, person.id, person.avatarSeed)} style={draft.avatarStyle} name={person.name} size={32} />
          <span className="voice-quick-character-copy"><b>{person.name.trim() || 'Без имени'}</b><small>{person.role}</small></span>
          <span className="voice-edit-mark" aria-hidden="true">✎</span>
        </button>
      </li>)}
    </ul>
    {selected && <div className="voice-quick-character-fields" key={selected.id}>
      <Field label="Имя" maxLength={80} value={selected.name} onChange={name => editPerson({ name })} />
      <Field label="Роль" maxLength={80} value={selected.role} onChange={role => editPerson({ role })} />
      <div className="voice-quick-character-actions">
        <button type="button" className="text-link" onClick={() => onChange({ ...draft, portraitSeeds: rerollPortraitSeeds(draft, selected.id) })}>Другой портрет</button>
        <button type="button" className="text-link" onClick={() => setSelectedId('')}>Готово</button>
      </div>
    </div>}
  </section>
}

function Field({ label, value, onChange, rows = 1, maxLength = 2000, autoFocus }: {
  label: string
  value: string
  onChange: (value: string) => void
  rows?: number
  maxLength?: number
  autoFocus?: boolean
}) {
  return <label className="voice-field">
    {label}
    {rows === 1
      ? <input value={value} onChange={event => onChange(event.target.value)} maxLength={maxLength} autoFocus={autoFocus} />
      : <textarea value={value} onChange={event => onChange(event.target.value)} rows={rows} maxLength={maxLength} />}
  </label>
}

function Choice<T extends string>({ label, value, options, onChange }: {
  label: string
  value: T
  options: readonly { id: T; title: string }[]
  onChange: (value: T) => void
}) {
  return <label className="voice-field">
    {label}
    <select value={value} onChange={event => onChange(event.target.value as T)}>
      {options.map(option => <option value={option.id} key={option.id}>{option.title}</option>)}
    </select>
  </label>
}

function Section({ id, open, onToggle, title, hint, children }: {
  id: string; open: boolean; onToggle: () => void; title: string; hint: string; children: ReactNode
}) {
  return <section className="editor-section">
    <button type="button" className="editor-section-head" aria-expanded={open} aria-controls={`voice-section-${id}`} onClick={event => {
      const header = event.currentTarget
      onToggle()
      if (!open) requestAnimationFrame(() => header.scrollIntoView({ block: 'nearest' }))
    }}>
      <span><b>{title}</b><span className="small muted">{hint}</span></span>
    </button>
    <div id={`voice-section-${id}`} className="editor-section-body voice-full-fields" hidden={!open}>{children}</div>
  </section>
}

function removeEditorRow(button: HTMLButtonElement, remove: () => void) {
  const row = button.closest('.voice-edit-row')
  const list = row?.parentElement
  if (!list || window.matchMedia('(prefers-reduced-motion: reduce)').matches || !list.animate) {
    remove()
    return
  }
  const startHeight = list.getBoundingClientRect().height
  const starts = Array.from(list.children).filter(item => item !== row).map(item => item.getBoundingClientRect().top)
  flushSync(remove)
  const endHeight = list.getBoundingClientRect().height
  const timing = { duration: 140, easing: 'ease-out' }
  Array.from(list.children).forEach((item, index) => {
    const distance = starts[index] - item.getBoundingClientRect().top
    if (distance) item.animate([{ transform: `translateY(${distance}px)` }, { transform: 'none' }], timing)
  })
  if (startHeight !== endHeight) list.animate([{ height: `${startHeight}px` }, { height: `${endHeight}px` }], timing)
}

function StringFields({ title, values, onChange }: { title: string; values: string[]; onChange: (values: string[]) => void }) {
  return <details className="editor-extra voice-list-fields">
    <summary>{title} · {values.length}</summary>
    {values.map((value, index) => <div className="voice-full-list-item voice-edit-row" key={index}>
      <Field label={`${title} ${index + 1}`} value={value} rows={2} onChange={text => onChange(values.map((item, i) => i === index ? text : item))} />
      <button type="button" className="text-link" onClick={event => removeEditorRow(event.currentTarget, () => onChange(values.filter((_, i) => i !== index)))}>Убрать</button>
    </div>)}
    <button type="button" className="chip" onClick={() => onChange([...values, ''])}>＋ Добавить</button>
  </details>
}

export function VoiceFullEditor({ draft, cover, onCoverChange, onChange, onClose, onPreview }: {
  cover: VoiceCover | null
  onCoverChange: (cover: VoiceCover | null) => void
  draft: VoiceDraft
  onChange: (draft: VoiceDraft) => void
  onClose: (summary: VoiceSummary) => void
  onPreview: (summary: VoiceSummary) => void
}) {
  const [openSection, setOpenSection] = useState('')
  const [newHero, setNewHero] = useState('')
  const [selectedAchievementId, setSelectedAchievementId] = useState('')
  const selectedAchievement = draft.achievements.find(item => item.id === selectedAchievementId)
  const authorRules = draft.rules.flatMap((rule, index) => rule.locked ? [] : [{ rule, index }])
  const section = (id: string) => ({ id, open: openSection === id, onToggle: () => setOpenSection(openSection === id ? '' : id) })
  function patch(change: Partial<VoiceDraft>) {
    const next = { ...draft, ...change }
    // Preserve freeform quick notes until the player changes the names or roles in the full editor.
    const roster = rosterText(next)
    onChange(roster !== rosterText(draft) ? { ...next, people: roster } : next)
  }
  function changeHero(id: string, change: Partial<Hero>) {
    patch({ stars: draft.stars.map(hero => hero.id === id ? { ...hero, ...change } : hero) })
  }
  function changePortrait(id: string) {
    patch({ portraitSeeds: rerollPortraitSeeds(draft, id) })
  }
  function changeAchievement(change: { title: string } | { emoji: string }) {
    patch({ achievements: draft.achievements.map(item => item.id === selectedAchievementId ? { ...item, ...change } : item) })
  }
  function addHero() {
    const id = crypto.randomUUID()
    patch({ stars: [...draft.stars, { id, name: '', role: '', about: '', voice: '', avatarSeed: id, trait: 1 }] })
    setNewHero(id)
  }
  const summary = voiceDraftSummary(draft)
  const issue = voiceWorldIssue(draft)
  const field = (label: string, key: {
    [K in keyof VoiceDraft]-?: VoiceDraft[K] extends string ? K : never
  }[keyof VoiceDraft], rows?: number, maxLength?: number) => (
    <Field label={label} value={draft[key]} onChange={value => patch({ [key]: value })} rows={rows} maxLength={maxLength} />
  )
  return <main className="pad voice-full">
    <div className="voice-intro">
      <h1 tabIndex={-1}>Полный редактор</h1>
      <p className="muted">Открой раздел, который хочешь изменить.</p>
    </div>
    <Section {...section('player')} title="Твой персонаж" hint={draft.name || 'Имя, роль и портрет'}>
      <div className="row">
        <Portrait seed={voicePortraitSeed(draft, 'player', draft.avatar)} style={draft.avatarStyle} name={draft.name} size={52} />
        <button type="button" className="text-link" onClick={() => changePortrait('player')}>Другой портрет</button>
      </div>
      {field('Имя персонажа', 'name', 1, 80)}
      {field('Кто ты в этой истории', 'role', 1, 80)}
      {field('Ник персонажа', 'avatar', 1, 80)}
      <Choice label="Стиль портретов" value={draft.avatarStyle} onChange={avatarStyle => patch({ avatarStyle })} options={[
        { id: 'adventurer', title: 'Приключения' }, { id: 'lorelei', title: 'Портрет' }, { id: 'notionists', title: 'Скетч' },
        { id: 'pixelArt', title: 'Пиксели' }, { id: 'thumbs', title: 'Мини' }, { id: 'shapes', title: 'Формы' },
      ]} />
    </Section>
    <Section {...section('world')} title="Мир и завязка" hint={draft.title || 'Название, место и начало истории'}>
      {field('Название мира', 'title', 1, 120)}
      {field('Где всё происходит', 'where', 1, 200)}
      {field('О чём этот мир', 'description', 3)}
      {field('С чего начинается история', 'firstContext', 3)}
      {field('Подсказка для первого поста', 'firstPrompt', 2)}
      <details className="editor-extra voice-list-fields">
        <summary>Карточка мира</summary>
        {field('Значок', 'emoji', 1, 8)}
        {field('Короткое описание', 'summary', 2, 200)}
      </details>
    </Section>
    <Section {...section('cover')} title="Обложка мира" hint={cover ? 'Загружена' : 'Карточка в каталоге'}>
      <VoiceCoverEditor cover={cover} title={draft.title} description={draft.summary} onChange={onCoverChange} />
    </Section>
    <Section {...section('heroes')} title="Герои вокруг тебя" hint={draft.stars.length ? `${draft.stars.length} в мире` : 'Добавь героя'}>
      {draft.stars.map(hero => <details className="editor-extra hero-editor voice-list-fields voice-edit-row" key={hero.id} open={newHero === hero.id ? true : undefined}>
        <summary>
          <Portrait seed={voicePortraitSeed(draft, hero.id, hero.avatarSeed)} style={draft.avatarStyle} name={hero.name} />
          <span><b>{hero.name || 'Новый герой'}</b><span className="small muted">{hero.role || 'Добавь занятие героя'}</span></span>
        </summary>
        <Field label="Имя героя" value={hero.name} onChange={name => changeHero(hero.id, { name })} maxLength={80} autoFocus={newHero === hero.id} />
        <Field label="Чем занимается герой" value={hero.role} onChange={role => changeHero(hero.id, { role })} maxLength={80} />
        <Field label="Характер и история" value={hero.about} onChange={about => changeHero(hero.id, { about })} rows={3} />
        <Field label="Как герой общается" value={hero.voice} onChange={voice => changeHero(hero.id, { voice })} rows={2} />
        <Field label="Ник героя" value={hero.avatarSeed} onChange={avatarSeed => changeHero(hero.id, { avatarSeed })} maxLength={80} />
        <label className="voice-field">
          Главная черта
          <select value={hero.trait} onChange={event => changeHero(hero.id, { trait: Number(event.target.value) })}>
            {traits.map((title, index) => <option value={index} key={title}>{title}</option>)}
          </select>
        </label>
        <button type="button" className="text-link" onClick={() => changePortrait(hero.id)}>Другой портрет</button>
        <button type="button" className="text-link" onClick={event => removeEditorRow(event.currentTarget, () => patch({ stars: draft.stars.filter(item => item.id !== hero.id) }))}>Убрать героя</button>
      </details>)}
      <button type="button" className="btn secondary" onClick={addHero}>＋ Добавить героя</button>
    </Section>
    <Section {...section('tone')} title="Настроение истории" hint="Драма, сложность и стиль подсказок">
      <Choice label="Градус драмы" value={draft.drama} options={dramaOptions} onChange={drama => patch({ drama })} />
      <Choice label="Сложность" value={draft.difficulty} options={[
        { id: 'easy', title: 'Легко' }, { id: 'normal', title: 'Средне' }, { id: 'hard', title: 'Сложно' },
      ]} onChange={difficulty => patch({ difficulty })} />
      {field('Стиль подсказок для постов', 'voice', 2, 200)}
    </Section>
    <Section {...section('rules')} title="Правила мира" hint="Как устроен твой мир">
      {authorRules.map(({ rule, index }, position) => <div className="voice-full-list-item voice-edit-row" key={index}>
        <Field label={`Особенность ${position + 1}`} value={rule.text} rows={2} onChange={text => patch({ rules: draft.rules.map((item, i) => i === index ? { ...item, text } : item) })} />
        <button type="button" className="text-link" onClick={event => removeEditorRow(event.currentTarget, () => patch({ rules: draft.rules.filter((_, i) => i !== index) }))}>Убрать особенность</button>
      </div>)}
      <button type="button" className="chip" onClick={() => patch({ rules: [...draft.rules, { text: '', locked: false }] })}>＋ Добавить особенность</button>
    </Section>
    <Section {...section('extras')} title="Идеи и варианты" hint="Посты, занятия и стили">
      <StringFields title="Идеи для постов" values={draft.postIdeas} onChange={postIdeas => patch({ postIdeas })} />
      <StringFields title="Примеры постов" values={draft.demoPosts} onChange={demoPosts => patch({ demoPosts })} />
      <StringFields title="Варианты занятия" values={draft.roles} onChange={roles => patch({ roles })} />
      <StringFields title="Варианты стиля" values={draft.voices} onChange={voices => patch({ voices })} />
    </Section>
    <Section {...section('achievements')} title="Достижения" hint="Названия и значки коллекции">
      <div className="voice-achievements">
        {draft.achievements.map(achievement => <button type="button" className="voice-achievement" key={achievement.id}
          aria-pressed={achievement.id === selectedAchievementId} onClick={() => setSelectedAchievementId(achievement.id === selectedAchievementId ? '' : achievement.id)}>
          <span aria-hidden="true">{achievement.emoji}</span><span>{achievement.title.trim() || 'Без названия'}</span>
        </button>)}
      </div>
      {selectedAchievement && <div className="voice-achievement-fields" key={selectedAchievement.id}>
        <Field label="Название достижения" value={selectedAchievement.title} onChange={title => changeAchievement({ title })} />
        <Field label="Значок достижения" value={selectedAchievement.emoji} maxLength={8} onChange={emoji => changeAchievement({ emoji })} />
        <button type="button" className="text-link" onClick={() => setSelectedAchievementId('')}>Готово</button>
      </div>}
    </Section>
    {issue && <p className="small muted" id="voice-world-issue" role="status">{issue}</p>}
    <button type="button" className="btn" disabled={issue !== null} aria-describedby={issue ? 'voice-world-issue' : undefined} onClick={() => onPreview(summary)}>Начать историю</button>
    <button type="button" className="text-link voice-full-back" onClick={() => onClose(summary)}>К быстрому черновику</button>
  </main>
}
