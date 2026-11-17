import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import { Avatar, Button, TopBar } from '../../ui'
import { TextField } from '../../ui/TextField'
import { worldArt } from '../worldArt'
import './guestOnboarding.css'

type Hero = { id: string; name: string; seed: string }
type Speaker = { id: string; first: string; second: string }
type GuestWorld = {
  title: string
  avatarStyle: string
  heroes: Hero[]
  speakers: [Speaker, Speaker]
  firstPostIdea: string
  replyIdea: string
}
const worlds: Record<string, GuestWorld> = {
  '5720946a-ea7c-4a10-9c50-51ce3deefc53': {
    title: 'Последние инфобизеры',
    avatarStyle: 'pixelArt',
    heroes: [
      { id: 'greedy_wolf', name: 'Меркантил', seed: 'greedy_wolf' },
      { id: '2kotleti_news', name: 'Бургер в Декабре', seed: '2kotleti_news' },
      { id: 'alexey_nl', name: 'Куляш AI', seed: 'alexey_nl' },
      { id: 'nika', name: 'Ника Борщ', seed: 'nika.recruit' },
    ],
    speakers: [
      { id: 'nika', first: 'Для начала можно посмотреть небольшой учебный проект. Какую задачу из него покажем новичкам?', second: 'Тогда попрошу показать проект целиком. Хочу увидеть, какую задачу выпускник решал сам.' },
      { id: 'alexey_nl', first: 'У меня открыты рекламный лендинг и три вакансии. По какой из них проверяем обещание курса?', second: 'Договорились, разберу одно объявление по пунктам. Начну с того, какой опыт там просят.' },
    ],
    firstPostIdea: 'Хочу собрать простой список для тех, кто ищет первую работу в IT. Какой навык стоит попробовать на маленьком проекте?',
    replyIdea: 'Давайте возьмём одну стартовую вакансию и сравним её требования с программой курса.',
  },
  'b3a1a522-0263-4796-a197-57a8731fc291': {
    title: 'Вечерело',
    avatarStyle: 'lorelei',
    heroes: [
      { id: 'isabella', name: 'Изабелла Лебедева', seed: 'isabella' },
      { id: 'eduard', name: 'Эдуард Калинин', seed: 'ed_thekiller_skin' },
      { id: 'yakov', name: 'Яков Чернов', seed: 'nessssssie' },
    ],
    speakers: [
      { id: 'eduard', first: 'На поляне зажёгся свет, хотя генератор уже заглох. Ты помнишь, откуда он шёл?', second: 'У меня в кинотеатре есть снимок поляны до грозы. Можно сверить, где стоял фонарь.' },
      { id: 'isabella', first: 'Я забрала фотоаппарат с поляны. Последний кадр смазало, но на краю виден свет. Кто стоял у старого фонаря?', second: 'Пришлю кадр целиком. По краю заметна ещё одна деталь: чей-то зонт остался у ограды.' },
    ],
    firstPostIdea: 'На поляне после дождя было необычно светло. У кого есть фотографии? Хочу собрать маленький альбом вечера.',
    replyIdea: 'Предлагаю сравнить снимки и восстановить порядок событий.',
  },
  'aec9494b-4118-4525-8458-e85d759c8dd9': {
    title: 'Спасти Хогвартс',
    avatarStyle: 'notionists',
    heroes: [
      { id: 'gosha', name: 'Гарри Поттер', seed: 'gosh' },
      { id: 'georgina', name: 'Гермиона Грейнджер', seed: '__curly_wit_' },
      { id: 'roman', name: 'Рон Уизли', seed: 'roman' },
      { id: 'draco', name: 'Драко Малфой', seed: 'dracom' },
    ],
    speakers: [
      { id: 'georgina', first: 'Совет просит смету к пятнице. С чего начнём: с ремонта комнат или с расходов будущих учеников?', second: 'Запишу это в план для совета. Попрошу мадам Помфри добавить то, что мы забыли.' },
      { id: 'draco', first: 'Совет ждёт смету к пятнице. Какой расход ты можешь посчитать прямо сейчас?', second: 'Я принесу свой расчёт на то же крыло замка. Посмотрим, что совет скажет, когда увидит обе сметы.' },
    ],
    firstPostIdea: 'Хочу составить первый план для замка. Давайте начнём с одного крыла: что там можно привести в порядок?',
    replyIdea: 'Начнём с одного крыла: сколько стоит подготовить там классы и комнаты для учеников?',
  },
  '8c02a7fb-3b55-4eb0-b957-1b4869ed9834': {
    title: 'Игра домов',
    avatarStyle: 'thumbs',
    heroes: [
      { id: 'diana', name: 'Диана ШтормоРождённая', seed: 'dracary' },
      { id: 'jon', name: 'Джон Винтер', seed: '__ice__and__fire__' },
      { id: 'james', name: 'Джеймс Левлистер', seed: '_gold__lion' },
    ],
    speakers: [
      { id: 'jon', first: 'На Севере ждут помощи. Кто назовёт день, когда у ворот появится первый обоз?', second: 'Передам дозорным. Пусть расчистят дорогу и приготовят место для припасов.' },
      { id: 'diana', first: 'Мои люди готовы собрать обоз. Сколько припасов нужно крепости на первую неделю?', second: 'Пусть распорядитель сверит это с запасами. Утром у нас будет список того, что можно отправить.' },
    ],
    firstPostIdea: 'Дорога на Север ещё открыта. Предлагаю собрать обоз с припасами. Что положим в него в первую очередь?',
    replyIdea: 'Соберём припасы на неделю и укажем в письме дату, когда обоз сможет выйти.',
  },
}

const storageKey = 'reki:guest-demo:v1'
const titles = {
  hero: 'Выбери героя',
  post: 'Твой первый пост',
  publish: 'Пост в ленте',
  reply: 'Ответь персонажу',
  respond: 'Ответ отправлен',
  finish: 'Начало истории',
}
type Step = keyof typeof titles
type GuestDraft = { worldId: string; step: Step; hero: string; text: string; reply: string; collected?: boolean }

function readGuestDemo(): GuestDraft | undefined {
  try {
    const draft = JSON.parse(localStorage.getItem(storageKey) ?? 'null')
    if (draft && Object.hasOwn(worlds, draft.worldId) && Object.hasOwn(titles, draft.step)
      && worlds[draft.worldId].heroes.some(hero => hero.id === draft.hero)
      && typeof draft.text === 'string' && typeof draft.reply === 'string') return draft
  } catch { /* The current demo still works in memory. */ }
}

export function readGuestWorld(): string | undefined {
  return readGuestDemo()?.worldId
}

export function clearGuestDemo(): void {
  try { localStorage.removeItem(storageKey) } catch { /* Storage may be unavailable. */ }
}

function GuestFinish({ world, worldId, draft, selected, guest, speaker, onCollect, onSave }: {
  world: GuestWorld; worldId: string; draft: GuestDraft; selected: Hero; guest: Hero; speaker: Speaker
  onCollect: () => void; onSave: () => void
}) {
  const counter = useRef<HTMLSpanElement>(null)
  const dialog = useRef<HTMLDialogElement>(null)
  const collectButton = useRef<HTMLButtonElement>(null)
  const flight = useRef<Animation[]>([])
  const inFlight = useRef(false)
  const mounted = useRef(true)
  const [collecting, setCollecting] = useState(false)
  const openSave = () => { if (dialog.current && !dialog.current.open) dialog.current.showModal() }
  useEffect(() => {
    mounted.current = true
    return () => { mounted.current = false; flight.current.forEach(animation => animation.cancel()) }
  }, [])

  const collect = async () => {
    if (draft.collected) { openSave(); return }
    if (inFlight.current) return
    inFlight.current = true
    setCollecting(true)
    const start = collectButton.current?.getBoundingClientRect()
    const end = counter.current?.getBoundingClientRect()
    if (start && end && !window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      const gems = [0, 1].map(index => {
        const gem = document.createElement('span')
        gem.className = 'game-onboarding-crystal-flight'
        gem.textContent = '💎'
        gem.setAttribute('aria-hidden', 'true')
        gem.style.left = `${start.left + start.width / 2 - 14}px`
        gem.style.top = `${start.top + start.height / 2 - 14}px`
        document.body.append(gem)
        const x = end.left + end.width / 2 - start.left - start.width / 2
        const y = end.top + end.height / 2 - start.top - start.height / 2
        const animation = gem.animate([
          { transform: 'translate(0, 0) scale(.75)', opacity: 1 },
          { transform: `translate(${x / 2 + (index ? 27 : -27)}px, ${y / 3}px) scale(1.1)`, opacity: 1, offset: .4 },
          { transform: `translate(${x}px, ${y}px) scale(.35)`, opacity: 0 },
        ], { duration: 620, delay: index * 80, easing: 'ease-out', fill: 'forwards' })
        flight.current.push(animation)
        return animation.finished.catch(() => undefined).finally(() => gem.remove())
      })
      await Promise.all(gems)
    }
    if (!mounted.current) return
    onCollect(); setCollecting(false); openSave()
  }

  const message = (hero: Hero, text: string, mine = false) => <div className="guest-finish__reply">
    <Avatar seed={hero.seed} world={worldId} avatarStyle={world.avatarStyle} mode="default" size={34} />
    <div><b>{hero.name}</b> <span>{mine ? 'ты' : '★ звезда'}</span><p>{text}</p></div>
  </div>

  return <main className="app guest-finish">
    <div className="screen no-tab thread-screen">
      <TopBar title="Реакции" rightPlain right={<div className="thread-header-actions">
        <span className="small muted">Пробная история · Мягко</span>
        <span ref={counter} className="energy-badge thread-crystal-count" aria-label={`В демо: ${draft.collected ? 2 : 0} кристаллов`}>💎 {draft.collected ? 2 : 0}</span>
      </div>} />
      <div className="pad">
        <div className="card"><div className="guest-onboarding__byline"><Avatar seed={selected.seed} world={worldId} avatarStyle={world.avatarStyle} mode="default" size={28} /><b>{selected.name}</b></div><p>{draft.text}</p></div>
        {message(guest, speaker.first)}
        {message(selected, draft.reply, true)}
        {message(guest, speaker.second)}
      </div>
    </div>
    <aside className="game-onboarding-progress guest-finish__reward">
      <div className="game-onboarding-meta"><span><span className="game-onboarding-segments" aria-hidden="true"><i className="on" /><i className="on" /><i className="on" /></span>Знакомство с игрой · 3 из 3</span><span className="game-onboarding-reward-hint">2 💎</span></div>
      <h2>Обучение завершено</h2>
      <p>Первый пост написан. Герой уже ответил тебе.</p>
      <button ref={collectButton} disabled={collecting} className="game-onboarding-collect" onClick={() => void collect()}>{draft.collected ? 'Сохранить историю' : 'Забрать 2 💎'}</button>
    </aside>
    <dialog ref={dialog} className="sheet guest-finish__popup" aria-labelledby="guest-save-title">
      <button type="button" className="guest-finish__close" onClick={() => dialog.current?.close()} aria-label="Закрыть">×</button>
      <div className="guest-finish__art" aria-hidden="true">
        <img src={worldArt[worldId].smallCover} alt="" />
        <div className="guest-finish__channel">
          <div className="guest-finish__channel-avatar"><Avatar seed={selected.seed} world={worldId} avatarStyle={world.avatarStyle} mode="default" size={54} /></div>
          <div><strong>{selected.name}</strong><span>Первый разговор начался</span></div>
          <div className="guest-finish__gem">💎<b>+2</b></div>
        </div>
      </div>
      <div className="guest-finish__popup-body">
        <div className="guest-finish__pitch">
          <h2 id="guest-save-title">Твоя история<br />только началась</h2>
          <p>Сохрани черновик и продолжи игру за своего героя.</p>
        </div>
        <ul className="guest-finish__benefits">
          <li><span aria-hidden="true">📈</span><div><b>Канал до миллиона</b><p>Пиши посты и набирай подписчиков</p></div></li>
          <li><span aria-hidden="true">💬</span><div><b>Отношения с героями</b><p>Спорь, дружи и находи союзников</p></div></li>
          <li><span aria-hidden="true">🌍</span><div><b>Свой мир</b><p>Открой редактор по ходу игры</p></div></li>
          <li><span aria-hidden="true">⚡</span><div><b>Тон «Остро»</b><p>Интриги, драмы и риск отмены</p></div></li>
        </ul>
        <div className="guest-finish__actions">
          <Button onClick={onSave}>Создать аккаунт и продолжить</Button>
          <p className="small muted">2 💎 в аккаунте за первый пост и ответ герою</p>
        </div>
      </div>
    </dialog>
  </main>
}

export function GuestOnboarding({ worldId, onSave }: {
  worldId: string
  onSave: (draft: { text: string; reply: string; hero?: string }) => void
}) {
  const world = worlds[worldId]
  const [draft, setDraft] = useState<GuestDraft>(() => {
    const saved = readGuestDemo()
    if (saved?.worldId === worldId) return saved
    return { worldId, step: 'hero', hero: world.heroes[0].id, text: '', reply: '' }
  })
  const [visibleLength, setVisibleLength] = useState<number>()
  const [storageAvailable, setStorageAvailable] = useState(true)
  const heading = useRef<HTMLHeadingElement>(null)
  const { step, text, reply } = draft
  const speaker = world.speakers.find(item => item.id !== draft.hero)!
  const guest = world.heroes.find(item => item.id === speaker.id)!
  const selected = world.heroes.find(item => item.id === draft.hero)!
  const revealing = visibleLength !== undefined
  const editorText = step === 'post' ? text : reply
  const waiting = step === 'publish' || step === 'respond'

  useLayoutEffect(() => {
    try {
      localStorage.setItem(storageKey, JSON.stringify(draft))
    } catch {
      setStorageAvailable(false)
    }
  }, [draft])

  useEffect(() => { heading.current?.focus() }, [step])

  useEffect(() => {
    if (!waiting) return
    const motion = window.matchMedia('(prefers-reduced-motion: reduce)')
    const next = () => {
      if (step === 'publish') setVisibleLength(motion.matches ? undefined : 0)
      setDraft(current => ({
        ...current, step: step === 'publish' ? 'reply' : 'finish',
        reply: step === 'publish' ? world.replyIdea : current.reply,
      }))
    }
    if (motion.matches) { next(); return }
    const timer = window.setTimeout(next, 900)
    return () => window.clearTimeout(timer)
  }, [step, waiting, world.replyIdea])

  useEffect(() => {
    if (!revealing) return
    const motion = window.matchMedia('(prefers-reduced-motion: reduce)')
    const finish = () => setVisibleLength(undefined)
    if (motion.matches) { finish(); return }
    const started = performance.now()
    const timer = window.setInterval(() => {
      const count = Math.floor((performance.now() - started) / 12)
      setVisibleLength(count >= editorText.length ? undefined : count)
    }, 30)
    motion.addEventListener('change', finish)
    return () => { window.clearInterval(timer); motion.removeEventListener('change', finish) }
  }, [editorText, revealing])

  const startPost = () => {
    if (!window.matchMedia('(prefers-reduced-motion: reduce)').matches) setVisibleLength(0)
    setDraft({ ...draft, step: 'post', text: world.firstPostIdea })
  }
  const changeStep = (next: Step) => {
    setVisibleLength(undefined)
    setDraft({ ...draft, step: next })
  }
  const edit = (value: string) => {
    setVisibleLength(undefined)
    setDraft(step === 'post' ? { ...draft, text: value } : { ...draft, reply: value })
  }
  const art = worldArt[worldId]

  if (step === 'finish') return <GuestFinish
    world={world} worldId={worldId} draft={draft} selected={selected} guest={guest} speaker={speaker}
    onCollect={() => setDraft(current => ({ ...current, collected: true }))}
    onSave={() => onSave({ text, reply, hero: draft.hero })}
  />

  return <main className="guest-onboarding" aria-labelledby="guest-title">
    <div className="guest-onboarding__cover" aria-hidden="true">
      <img src={art.smallCover} alt="" />
    </div>
    <header className="guest-onboarding__world">
      <img src="/android-chrome-192x192.png?v=wave-2" width="32" height="32" alt="" />
      <div><b>{world.title}</b><p>Пробная история · Мягко</p></div>
    </header>
    <section className="guest-onboarding__sheet">
      <h1 ref={heading} id="guest-title" tabIndex={-1}>{titles[step]}</h1>
      {step === 'hero' ? <>
        <div className="guest-onboarding__heroes" role="group" aria-label="Герои мира">
          {world.heroes.map(hero => <button key={hero.id} type="button" aria-pressed={draft.hero === hero.id} onClick={() => setDraft({ ...draft, hero: hero.id })}>
            <Avatar seed={hero.seed} label={hero.name} world={worldId} avatarStyle={world.avatarStyle} mode="default" size={36} />
            <span>{hero.name}</span>
          </button>)}
        </div>
        <Button onClick={startPost}>Написать пост</Button>
      </> : <>
        <div className="guest-onboarding__byline">
          <Avatar seed={selected.seed} label={selected.name} world={worldId} avatarStyle={world.avatarStyle} mode="default" size={28} />
          <b>{selected.name}</b>
        </div>
        {step === 'post' ? <>
          <TextField label="Текст поста" value={text} visibleLength={visibleLength} onInteract={() => setVisibleLength(undefined)} onChange={edit} maxLength={500} rows={4} />
          <Button disabled={!text.trim()} onClick={() => changeStep('publish')}>Опубликовать</Button>
        </> : <>
          <details className="guest-onboarding__details"><summary>Твой пост</summary><p>{text}</p></details>
          <div className="guest-onboarding__thread" aria-live="polite" aria-relevant="additions text">
            {step !== 'publish' && <div className="guest-onboarding__message">
              <Avatar seed={guest.seed} label={guest.name} world={worldId} avatarStyle={world.avatarStyle} mode="default" size={31} />
              <div><b>{guest.name}</b><p>{speaker.first}</p></div>
            </div>}
            {step === 'respond' && <details className="guest-onboarding__details"><summary>Твой ответ</summary><p>{reply}</p></details>}
            {waiting && <div className="guest-onboarding__typing">{guest.name} печатает<span aria-hidden="true"><i /><i /><i /></span></div>}
          </div>
          {step === 'reply' && <>
            <TextField label="Текст ответа" value={reply} visibleLength={visibleLength} onInteract={() => setVisibleLength(undefined)} onChange={edit} maxLength={1000} rows={3} />
            <Button disabled={!reply.trim()} onClick={() => changeStep('respond')}>Ответить</Button>
          </>}
        </>}
      </>}
      {!storageAvailable && <p className="guest-onboarding__intro" role="status">Хранилище недоступно. Черновик останется в этой вкладке до обновления страницы.</p>}
    </section>
  </main>
}
