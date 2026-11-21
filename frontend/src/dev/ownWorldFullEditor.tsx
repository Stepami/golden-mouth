import { useEffect, useState, type ReactNode } from 'react'
import type { WorldDefinition } from '../api/types'
import { blankWorld } from '../app/worldDraft'
import { dramaOptions } from '../app/worldPrototype'
import { avatarUri } from '../ui/avatarRenderer'
import './ownWorldFullEditor.css'

type VoiceSummary = {
  title: string
  opening: string
  people: string
  rules: string
}

type VoiceDraft = WorldDefinition & { people: string }
type Hero = WorldDefinition['stars'][number]
const traits = ['Смелость', 'Поддержка', 'Загадка', 'Воображение', 'Любопытство', 'Энергия', 'Ирония и расчёт', 'Сухой юмор', 'Самоирония', 'Азарт', 'Внимание']

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
  return { ...draft, title: summary.title, firstContext: summary.opening, people: summary.people, rules }
}

function exampleWorld(summary: VoiceSummary): VoiceDraft {
  return applyQuickEdits({
    ...blankWorld(),
    emoji: '☁️',
    summary: 'Почта и тайны летающих островов',
    title: 'Город над облаками',
    description: 'На летающих островах новости разносят почтальоны. В старых картах остались маршруты, которыми давно никто не пользуется.',
    where: 'Архипелаг летающих островов',
    name: 'Рин',
    avatar: 'cloud-courier-rin',
    avatarStyle: 'adventurer',
    role: 'Воздушный почтальон',
    people: summary.people,
    voice: 'Живо, с любопытством и лёгким юмором',
    firstPrompt: 'Кто из подписчиков помнит путь к острову с посылки?',
    roles: ['Воздушный почтальон', 'Смотритель маяка', 'Собиратель старых карт'],
    voices: ['Живо, с лёгким юмором', 'Как в путевом дневнике'],
    postIdeas: ['У меня посылка для острова, которого нет на картах…', 'На рассвете попробую проверить старый маршрут…'],
    demoPosts: ['На посылке адрес острова, который исчез сто лет назад. Может, у кого-то осталась старая карта?'],
    achievements: [{ id: 'first-route', emoji: '✉️', title: 'Первый маршрут', open: false }],
    stars: [
      { id: 'lis', name: 'Лис', role: 'Картограф', about: 'Хранит старые карты и знает про исчезнувшие маршруты больше, чем говорит.', voice: 'Сдержанно, с сухим юмором', avatarSeed: 'cloud-cartographer-lis', trait: 2 },
      { id: 'mika', name: 'Мика', role: 'Смотритель маяка', about: 'Каждое утро отмечает в журнале острова, которые видны с башни.', voice: 'Тепло, короткими фразами', avatarSeed: 'cloud-lighthouse-mika', trait: 4 },
    ],
  }, summary)
}

export function useVoiceFullDraft(summary: VoiceSummary) {
  const [draft, setDraft] = useState(() => exampleWorld(summary))
  return { draft, setDraft, openEditor: () => setDraft(previous => applyQuickEdits(previous, summary)) }
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

export function VoicePortraits({ draft }: { draft: VoiceDraft }) {
  return <ul className="voice-portraits" aria-label="Персонажи мира">
    {peopleOf(draft).map(person => <li key={person.id}>
      <Portrait seed={person.avatarSeed || person.id} style={draft.avatarStyle} name={person.name} size={36} />
      <span><b>{person.name || 'Без имени'}</b><small>{person.role}</small></span>
    </li>)}
  </ul>
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

function Section({ title, hint, children }: { title: string; hint: string; children: ReactNode }) {
  return <details className="editor-section">
    <summary className="editor-section-head">
      <span><b>{title}</b><span className="small muted">{hint}</span></span>
    </summary>
    <div className="editor-section-body voice-full-fields">{children}</div>
  </details>
}

function StringFields({ title, values, onChange }: { title: string; values: string[]; onChange: (values: string[]) => void }) {
  return <details className="editor-extra voice-list-fields">
    <summary>{title} · {values.length}</summary>
    {values.map((value, index) => <div className="voice-full-list-item" key={index}>
      <Field label={`${title} ${index + 1}`} value={value} rows={2} onChange={text => onChange(values.map((item, i) => i === index ? text : item))} />
      <button type="button" className="text-link" onClick={() => onChange(values.filter((_, i) => i !== index))}>Убрать</button>
    </div>)}
    <button type="button" className="chip" onClick={() => onChange([...values, ''])}>＋ Добавить</button>
  </details>
}

export function VoiceFullEditor({ draft, onChange, onClose, onPreview }: {
  draft: VoiceDraft
  onChange: (draft: VoiceDraft) => void
  onClose: (summary: VoiceSummary) => void
  onPreview: (summary: VoiceSummary) => void
}) {
  const [newHero, setNewHero] = useState('')
  function patch(change: Partial<VoiceDraft>) {
    const next = { ...draft, ...change }
    // Preserve freeform quick notes until the player changes the names or roles in the full editor.
    const rosterText = (value: VoiceDraft) => peopleOf(value)
      .filter(person => person.name.trim() || person.role.trim())
      .map(person => [person.name.trim(), person.role.trim()].filter(Boolean).join(' — '))
      .join('\n')
    const roster = rosterText(next)
    onChange(roster !== rosterText(draft) ? { ...next, people: roster } : next)
  }
  function changeHero(id: string, change: Partial<Hero>) {
    patch({ stars: draft.stars.map(hero => hero.id === id ? { ...hero, ...change } : hero) })
  }
  function addHero() {
    const id = crypto.randomUUID()
    patch({ stars: [...draft.stars, { id, name: '', role: '', about: '', voice: '', avatarSeed: id, trait: 1 }] })
    setNewHero(id)
  }
  const summary = voiceDraftSummary(draft)
  const field = (label: string, key: {
    [K in keyof VoiceDraft]-?: VoiceDraft[K] extends string ? K : never
  }[keyof VoiceDraft], rows?: number, maxLength?: number) => (
    <Field label={label} value={draft[key]} onChange={value => patch({ [key]: value })} rows={rows} maxLength={maxLength} />
  )
  return <main className="pad voice-full">
    <div className="voice-intro">
      <h1 tabIndex={-1}>Полный редактор</h1>
      <p className="muted">Поля заполнены примером. Можно править.</p>
    </div>
    <Section title="Твой персонаж" hint={draft.name || 'Имя, занятие и портрет'}>
      <div className="row">
        <Portrait seed={draft.avatar || 'player'} style={draft.avatarStyle} name={draft.name} size={52} />
        <button type="button" className="text-link" onClick={() => patch({ avatar: crypto.randomUUID() })}>Другой портрет</button>
      </div>
      {field('Имя персонажа', 'name', 1, 80)}
      {field('Кто ты в этой истории', 'role', 1, 80)}
      {field('Ключ портрета', 'avatar', 1, 80)}
      <Choice label="Стиль портретов" value={draft.avatarStyle} onChange={avatarStyle => patch({ avatarStyle })} options={[
        { id: 'adventurer', title: 'Приключения' }, { id: 'lorelei', title: 'Портрет' }, { id: 'notionists', title: 'Скетч' },
        { id: 'pixelArt', title: 'Пиксели' }, { id: 'thumbs', title: 'Мини' }, { id: 'shapes', title: 'Формы' },
      ]} />
    </Section>
    <Section title="Мир и начало истории" hint={draft.title || 'Место, атмосфера и завязка'}>
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
    <Section title="Герои вокруг тебя" hint={draft.stars.map(hero => hero.name.trim()).filter(Boolean).join(', ') || 'Персонажи мира'}>
      {draft.stars.map(hero => <details className="editor-extra hero-editor voice-list-fields" key={hero.id} open={newHero === hero.id ? true : undefined}>
        <summary>
          <Portrait seed={hero.avatarSeed || hero.id} style={draft.avatarStyle} name={hero.name} />
          <span><b>{hero.name || 'Новый герой'}</b><span className="small muted">{hero.role || 'Добавь занятие героя'}</span></span>
        </summary>
        <Field label="Имя героя" value={hero.name} onChange={name => changeHero(hero.id, { name })} maxLength={80} autoFocus={newHero === hero.id} />
        <Field label="Чем занимается герой" value={hero.role} onChange={role => changeHero(hero.id, { role })} maxLength={80} />
        <Field label="Характер и история" value={hero.about} onChange={about => changeHero(hero.id, { about })} rows={3} />
        <Field label="Как герой общается" value={hero.voice} onChange={voice => changeHero(hero.id, { voice })} rows={2} />
        <Field label="Ключ портрета героя" value={hero.avatarSeed} onChange={avatarSeed => changeHero(hero.id, { avatarSeed })} maxLength={80} />
        <label className="voice-field">
          Главная черта
          <select value={hero.trait} onChange={event => changeHero(hero.id, { trait: Number(event.target.value) })}>
            {traits.map((title, index) => <option value={index} key={title}>{title}</option>)}
          </select>
        </label>
        <button type="button" className="text-link" onClick={() => changeHero(hero.id, { avatarSeed: crypto.randomUUID() })}>Другой портрет</button>
        <button type="button" className="text-link" onClick={() => patch({ stars: draft.stars.filter(item => item.id !== hero.id) })}>Убрать героя</button>
      </details>)}
      <button type="button" className="btn secondary" onClick={addHero}>＋ Добавить героя</button>
      {field('Кратко о персонажах', 'people', 3)}
    </Section>
    <Section title="Настроение истории" hint={dramaOptions.find(option => option.id === draft.drama)?.title ?? 'Драма и стиль подсказок'}>
      <Choice label="Градус драмы" value={draft.drama} options={dramaOptions} onChange={drama => patch({ drama })} />
      <Choice label="Сложность" value={draft.difficulty} options={[
        { id: 'easy', title: 'Легко' }, { id: 'normal', title: 'Средне' }, { id: 'hard', title: 'Сложно' },
      ]} onChange={difficulty => patch({ difficulty })} />
      {field('Стиль подсказок для постов', 'voice', 2, 200)}
    </Section>
    <Section title="Особенности мира" hint={draft.rules.find(rule => !rule.locked && rule.text)?.text || 'Как устроен мир'}>
      {draft.rules.map((rule, index) => rule.locked
        ? <div className="age-rule-note" key={index}><b>Ограничения контента</b><p>{rule.text}</p></div>
        : <div className="voice-full-list-item" key={index}>
          <Field label={`Особенность ${index + 1 - draft.rules.slice(0, index).filter(item => item.locked).length}`} value={rule.text} rows={2} onChange={text => patch({ rules: draft.rules.map((item, i) => i === index ? { ...item, text } : item) })} />
          <button type="button" className="text-link" onClick={() => patch({ rules: draft.rules.filter((_, i) => i !== index) })}>Убрать особенность</button>
        </div>)}
      <button type="button" className="chip" onClick={() => patch({ rules: [...draft.rules, { text: '', locked: false }] })}>＋ Добавить особенность</button>
    </Section>
    <Section title="Дополнительные настройки игры" hint="Идеи для постов и варианты настроек">
      <StringFields title="Идеи для постов" values={draft.postIdeas} onChange={postIdeas => patch({ postIdeas })} />
      <StringFields title="Примеры постов" values={draft.demoPosts} onChange={demoPosts => patch({ demoPosts })} />
      <StringFields title="Варианты занятия" values={draft.roles} onChange={roles => patch({ roles })} />
      <StringFields title="Варианты стиля" values={draft.voices} onChange={voices => patch({ voices })} />
      <details className="editor-extra voice-list-fields">
        <summary>Достижения коллекции</summary>
        {draft.achievements.map((achievement, index) => <div className="voice-full-list-item" key={achievement.id}>
          <Field label="Название достижения" value={achievement.title} onChange={title => patch({ achievements: draft.achievements.map((item, i) => i === index ? { ...item, title } : item) })} />
          <Field label="Значок достижения" value={achievement.emoji} maxLength={8} onChange={emoji => patch({ achievements: draft.achievements.map((item, i) => i === index ? { ...item, emoji } : item) })} />
          <button type="button" className="text-link" onClick={() => patch({ achievements: draft.achievements.filter((_, i) => i !== index) })}>Убрать достижение</button>
        </div>)}
        <button type="button" className="chip" onClick={() => patch({ achievements: [...draft.achievements, { id: crypto.randomUUID(), title: '', emoji: '⭐', open: false }] })}>＋ Добавить достижение</button>
      </details>
    </Section>
    <button type="button" className="btn" onClick={() => onPreview(summary)}>Посмотреть канал</button>
    <button type="button" className="text-link voice-full-back" onClick={() => onClose(summary)}>К быстрому черновику</button>
  </main>
}
