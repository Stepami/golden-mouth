import { trackLandingAuth } from '../analytics'
import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import { Avatar, Button } from '../../ui'
import { worldDefinitionOf, worlds as templates } from '../../api/worldContent'
import type { CommentT, Post, PostResult, WorldDefinition } from '../../api/types'
import { Home } from '../../screens/onboarding'
import { Compose, Live, Result, Thread } from '../../screens/turn'
import { AvatarWorld } from '../../ui/avatar'
import { NavContext, type Nav } from '../nav'
import { blankWorld } from '../worldDraft'
import { GuestSessionContext, type GuestPlayer } from './guestSession'
import { worldArt } from '../worldArt'
import './guestOnboarding.css'

type Hero = { id: string; name: string; seed: string; role: string }
type Speaker = { id: string; first: string; second: string }
type GuestWorld = {
  title: string
  emoji: string
  avatarStyle: string
  heroes: Hero[]
  speakers: [Speaker, Speaker]
  intro: { goal: string; context: string; firstStep: string }
  firstPostIdea: string
  replyIdea: string
}
const worlds: Record<string, GuestWorld> = {
  '5720946a-ea7c-4a10-9c50-51ce3deefc53': {
    title: 'Последние инфобизеры',
    emoji: '💻',
    avatarStyle: 'pixelArt',
    heroes: [
      { id: 'greedy_wolf', name: 'Меркантил', seed: 'greedy_wolf', role: 'Продавец курса «Вкат»' },
      { id: '2kotleti_news', name: 'Бургер в Декабре', seed: '2kotleti_news', role: 'Автор обзора спора о курсах' },
      { id: 'alexey_nl', name: 'Куляш AI', seed: 'alexey_nl', role: 'Критик обещаний «Вката»' },
      { id: 'nika', name: 'Ника Борщ', seed: 'nika.recruit', role: 'Рекрутер с завышенной планкой' },
    ],
    speakers: [
      { id: 'nika', first: 'Я открыла резюме таксиста. В графе «опыт» пусто, а на работу он хочет уже в понедельник. Какую первую задачу ты бы доверил ему показать на собеседовании?', second: 'Хорошо. Пусть покажет ход работы и объяснит, где сам принял решение. Я посмотрю на проект, даже если в резюме всё ещё будет пусто.' },
      { id: 'alexey_nl', first: 'На баннере «Вката» вижу обещание работы. В вакансии вижу требование опыта. Что положим рядом для проверки: программу курса или один проект выпускника?', second: 'Соберу всё в один пост: обещание, требования вакансии и что человек реально сделал. Тогда можно будет обсуждать факты.' },
    ],
    intro: {
      goal: 'Дать новичку шанс в IT',
      context: 'Таксист хочет сменить работу. «Вкат» обещает оффер, а рекрутер просит опыт. В комментах уже спорят 👀',
      firstStep: 'Спроси подписчиков, с какой задачи новичок может начать.',
    },
    firstPostIdea: 'В чат написал таксист: хочет уйти в IT, но первый проект требует опыта. «Вкат» обещает помочь. Давайте выберем одну задачу, с которой он может начать.',
    replyIdea: 'Предлагаю взять одну задачу из вакансии. Пусть решит её и покажет, как рассуждал.',
  },
  'b3a1a522-0263-4796-a197-57a8731fc291': {
    title: 'Вечерело',
    emoji: '🌲',
    avatarStyle: 'lorelei',
    heroes: [
      { id: 'isabella', name: 'Изабелла Лебедева', seed: 'isabella', role: 'Автор ночных фотографий' },
      { id: 'eduard', name: 'Эдуард Калинин', seed: 'ed_thekiller_skin', role: 'Хранитель кинотеатра' },
      { id: 'yakov', name: 'Яков Чернов', seed: 'nessssssie', role: 'Проводник' },
    ],
    speakers: [
      { id: 'eduard', first: 'Мяч улетел к лесу. Сразу после удара грянул гром. На твоём фото видно, кто стоял у деревьев?', second: 'У меня в кинотеатре есть съёмка поляны до грозы. Давай сравним кадры. Может, это вообще был блик в луже?' },
      { id: 'isabella', first: 'Мяч я нашла у ограды. На последнем кадре всё смазано, только зонт видно. Ты помнишь, кто остался на поляне?', second: 'Скину кадр целиком. Присмотрись к луже у ограды — там тоже что-то видно.' },
    ],
    intro: {
      goal: 'Дать Изабелле рассказать свою версию',
      context: 'Гроза сорвала бейсбол. Изабелла уехала, а в ленте уже три версии того, что случилось. Эдуард предлагает сначала спросить её саму.',
      firstStep: 'Спроси, кто остался на поляне после последнего удара.',
    },
    firstPostIdea: 'Вчера играли в бейсбол, пока не началась гроза. После последнего удара за оградой что-то вспыхнуло. Кто успел снять? 📸',
    replyIdea: 'Давай сравним наши кадры. Мой снят сразу после последнего удара.',
  },
  'aec9494b-4118-4525-8458-e85d759c8dd9': {
    title: 'Спасти Хогвартс',
    emoji: '🪄',
    avatarStyle: 'notionists',
    heroes: [
      { id: 'gosha', name: 'Гарри Поттер', seed: 'gosh', role: 'За возвращение школы' },
      { id: 'georgina', name: 'Гермиона Грейнджер', seed: '__curly_wit_', role: 'За устойчивую и этичную школу' },
      { id: 'roman', name: 'Рон Уизли', seed: 'roman', role: 'За доступность обучения' },
      { id: 'draco', name: 'Драко Малфой', seed: 'dracom', role: 'За коммерческий проект замка' },
    ],
    speakers: [
      { id: 'georgina', first: 'Гарри, я уже подставила ведро под дыру в крыше. Шляпа сухая, ученикам пока сидеть негде 🪄 С чего начнём ремонт?', second: 'Окей, запишу для совета. А мадам Помфри спрошу, что нужно, чтобы принять учеников в первую неделю.' },
      { id: 'draco', first: 'Свечи в Большом зале всё ещё летают. На крыше, похоже, держатся только свечи. Что будем чинить первым?', second: 'Окей, прикину цену для того же крыла. К пятнице у совета будут две сметы.' },
    ],
    intro: {
      goal: 'Вернуть Хогвартс ученикам',
      context: 'Хогвартс закрыли. Драко уже прикидывает, сколько брать за ночь в замке 👀 Гермиона хочет вернуть уроки, а Рон спрашивает, кто оплатит форму и учебники.',
      firstStep: 'Напиши, с чего начнём спасать школу.',
    },
    firstPostIdea: 'Зашли в Большой зал. Свечи горят, на Шляпу капает с потолка. Можно вернуть уроки хотя бы в одно крыло. С чего начнём? ✨',
    replyIdea: 'Давай начнём с крыши над одним классом. Сколько будет стоить ремонт?',
  },
  '8c02a7fb-3b55-4eb0-b957-1b4869ed9834': {
    title: 'Игра домов',
    emoji: '👑',
    avatarStyle: 'thumbs',
    heroes: [
      { id: 'diana', name: 'Диана ШтормоРождённая', seed: 'dracary', role: 'Наследница династии' },
      { id: 'jon', name: 'Джон Винтер', seed: '__ice__and__fire__', role: 'Защитник Стены' },
      { id: 'james', name: 'Джеймс Левлистер', seed: '_gold__lion', role: 'КороноУбийца' },
    ],
    speakers: [
      { id: 'jon', first: 'В Драконьем Логове мы посмотрели на карту и наконец заговорили об одной дороге. У Стены ждут припасы. Кто может отправить первый обоз до снегов?', second: 'Попрошу дозорных открыть ворота к рассвету. Если обоз пойдёт в назначенный день, у нас будет время распределить припасы.' },
      { id: 'diana', first: 'Перед советом я велела развернуть карту Северной дороги. Мои люди могут отправить обоз. Где лучше принять припасы, чтобы их хватило на неделю?', second: 'Попрошу распорядителя сверить это с запасами. До рассвета пришлю список, а после можно договориться с охраной дороги.' },
    ],
    intro: {
      goal: 'Отправить помощь к Стене',
      context: 'Совет закончился, а обоз на Север так и не собрали. Снег скоро перекроет дорогу.',
      firstStep: 'Спроси дома, кто готов снарядить первый обоз.',
    },
    firstPostIdea: 'Совет в Драконьем Логове закончился. На карте осталась одна дорога к Стене, пока её не занесло. Что отправим первым обозом?',
    replyIdea: 'Начнём с еды и тёплых одеял на неделю. Обоз выйдет на рассвете, если у ворот его встретит дозор.',
  },
}

const storageKey = 'reki:guest-demo:v1'
const screens = {
  '03-5_stage': Home,
  '02-1_compose': Compose,
  '02-2_live': Live,
  '02-3_result': Result,
  '02-6_thread': Thread,
}
type Screen = keyof typeof screens
const hints: Record<Screen, [number, string]> = {
  '03-5_stage': [1, 'Открой редактор и напиши первый пост в своём канале.'],
  '02-1_compose': [1, 'Дополни готовую идею или напиши свою.'],
  '02-2_live': [2, 'Герои вот-вот увидят твой пост.'],
  '02-3_result': [2, 'Открой комментарии и ответь герою.'],
  '02-6_thread': [3, 'Напиши герою ответ, чтобы завершить обучение.'],
}

function GuestHint({ screen, inline = true }: { screen: Screen; inline?: boolean }) {
  const [step, text] = hints[screen]
  return <aside className={`game-onboarding-progress guest-hint${inline ? ' is-inline' : ''}`} aria-label="Подсказка обучения">
    <div className="game-onboarding-meta"><span><span className="game-onboarding-segments" aria-hidden="true">{[1, 2, 3].map(n => <i key={n} className={n <= step ? 'on' : ''} />)}</span>Знакомство с игрой · {step} из 3</span><span className="game-onboarding-reward-hint">2 💎</span></div>
    <p>{text}</p>
  </aside>
}

type GuestDraft = {
  worldId: string
  screen: Screen
  hero: string
  text?: string
  reply?: string
  published?: boolean
  replySubmitted?: boolean
  replyDone?: boolean
  collected?: boolean
}

function readGuestDemo(): GuestDraft | undefined {
  try {
    const draft = JSON.parse(localStorage.getItem(storageKey) ?? 'null')
    if (!draft || !Object.hasOwn(worlds, draft.worldId) || !worlds[draft.worldId].heroes.some(hero => hero.id === draft.hero)) return
    if (draft.text !== undefined && typeof draft.text !== 'string' || draft.reply !== undefined && typeof draft.reply !== 'string') return
    if (['01-3b_world_overview', '01-4_channel', '01-4b_world_setup'].includes(draft.screen)) return { ...draft, screen: '03-5_stage' }
    if (Object.hasOwn(screens, draft.screen)) return draft
    // Preserve previews started before the guest journey adopted the game screens.
    switch (draft.step) {
      case 'hero': return { ...draft, screen: '03-5_stage', text: undefined }
      case 'post': return { ...draft, screen: '02-1_compose' }
      case 'publish': return { ...draft, screen: '02-2_live' }
      case 'reply': return { ...draft, screen: '02-6_thread', published: true }
      case 'respond': return { ...draft, screen: '02-6_thread', published: true, replySubmitted: true }
      case 'finish': return { ...draft, screen: '02-6_thread', published: true, replySubmitted: true, replyDone: true }
    }
  } catch { /* The current demo still works in memory. */ }
}

export function readGuestWorld(): string | undefined {
  return readGuestDemo()?.worldId
}

export function clearGuestDemo(): void {
  try { localStorage.removeItem(storageKey) } catch { /* Storage may be unavailable. */ }
}

function GuestFinish({ worldId, collected, selected, avatarStyle, onClaim, onCollect, onSave }: {
  worldId: string; collected: boolean; selected: WorldDefinition['stars'][number]; avatarStyle: string
  onClaim: () => void; onCollect: () => void; onSave: () => void
}) {
  const dialog = useRef<HTMLDialogElement>(null)
  const collectButton = useRef<HTMLButtonElement>(null)
  const flight = useRef<Animation[]>([])
  const inFlight = useRef(false)
  const mounted = useRef(true)
  const [collecting, setCollecting] = useState(false)
  useLayoutEffect(() => {
    if (collected && dialog.current && !dialog.current.open) dialog.current.showModal()
  }, [collected])
  useEffect(() => {
    mounted.current = true
    return () => { mounted.current = false; flight.current.forEach(animation => animation.cancel()) }
  }, [])

  const collect = async () => {
    if (collected || inFlight.current) return
    inFlight.current = true
    onClaim()
    setCollecting(true)
    const start = collectButton.current?.getBoundingClientRect()
    const end = document.querySelector('.guest-finish .thread-crystal-count')?.getBoundingClientRect()
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
    onCollect(); setCollecting(false)
  }

  return <>
    <aside className="game-onboarding-progress guest-finish__reward">
      <div className="game-onboarding-meta"><span><span className="game-onboarding-segments" aria-hidden="true"><i className="on" /><i className="on" /><i className="on" /></span>Знакомство с игрой · 3 из 3</span><span className="game-onboarding-reward-hint">2 💎</span></div>
      <h2>Обучение завершено</h2>
      <p>Первый пост написан. Герой уже ответил тебе.</p>
      <button ref={collectButton} disabled={collecting || collected} className="game-onboarding-collect" onClick={() => void collect()}>Забрать 2 💎</button>
    </aside>
    <dialog ref={dialog} className="sheet guest-finish__popup" aria-labelledby="guest-save-title" onCancel={event => event.preventDefault()}>
      <div className="guest-finish__art" aria-hidden="true">
        <img src={worldArt[worldId].smallCover} alt="" />
        <div className="guest-finish__channel">
          <div className="guest-finish__channel-avatar"><Avatar seed={selected.avatarSeed} world={worldId} avatarStyle={avatarStyle} mode="default" size={54} /></div>
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
          <li><span aria-hidden="true">📈</span><div><b>Канал до миллиона</b><p>Веди блог от лица героя. Смотри, какие посты залетают, и расти до миллиона.</p></div></li>
          <li><span aria-hidden="true">💬</span><div><b>Отношения с героями</b><p>Герои отвечают на посты. Заводи союзников и спорь с соперниками в комментах.</p></div></li>
          <li><span aria-hidden="true">🌍</span><div><b>Свой мир</b><p>Придумай своих героев, место и правила. Редактор откроется по ходу игры.</p></div></li>
          <li><span aria-hidden="true">🌶️</span><div><b>Тон «Остро»</b><p>Горячие споры, драмы и риск отмены. Решай, что ответить.</p></div></li>
        </ul>
        <div className="guest-finish__actions">
          <Button onClick={onSave}>Создать аккаунт и продолжить</Button>
          <p className="small muted">2 💎 в аккаунте за первый пост и ответ герою</p>
        </div>
      </div>
    </dialog>
  </>
}

export function GuestOnboarding({ worldId, onSave }: {
  worldId: string
  onSave: (draft: { text: string; reply: string; hero?: string }) => void
}) {
  const fixture = worlds[worldId]
  const [world] = useState<WorldDefinition>(() => {
    if (templates.some(template => template.id === worldId)) {
      const template = worldDefinitionOf(worldId)
      if (fixture.heroes.every(hero => template.stars.some(star => star.id === hero.id))) {
        return { ...template, firstContext: fixture.intro.context, drama: 'mid', difficulty: 'normal', stars: fixture.heroes.map(hero => template.stars.find(star => star.id === hero.id)!) }
      }
    }
    // Returning guests can finish without requiring the template endpoint.
    const art = worldArt[worldId]
    return {
      ...blankWorld(), templateId: worldId, drama: 'mid', difficulty: 'normal', title: fixture.title, emoji: fixture.emoji, avatarStyle: fixture.avatarStyle,
      description: art.pitch, summary: art.pitch, firstContext: fixture.intro.context,
      stars: fixture.heroes.map(hero => ({ id: hero.id, name: hero.name, avatarSeed: hero.seed, role: hero.role, about: '', voice: '', trait: 0 })),
    }
  })
  const [draft, setDraft] = useState<GuestDraft>(() => {
    const saved = readGuestDemo()
    if (saved?.worldId === worldId) return saved
    return { worldId, screen: '03-5_stage', hero: fixture.heroes[0].id }
  })
  const { screen, hero, published, replySubmitted, replyDone, collected } = draft
  const [storageAvailable, setStorageAvailable] = useState(true)
  const selected = world.stars.find(star => star.id === hero)!
  const speaker = fixture.speakers.find(speaker => speaker.id !== hero)!
  const other = world.stars.find(star => star.id === speaker.id)!
  const reply = draft.reply ?? fixture.replyIdea
  const text = draft.text ?? fixture.firstPostIdea
  const goal = fixture.intro
  const player: GuestPlayer = {
    id: 'guest', world: worldId, name: selected.name, handle: selected.avatarSeed,
    subs: published ? 4 : 0, postsLeft: replySubmitted ? 8 : published ? 9 : 10,
    streak: { days: 0, week: [], nextReward: '' },
  }
  const firstComment: CommentT = {
    id: 'guest-first', author: other.name, avatar: other.avatarSeed, authorId: `Star_${other.id}`, handle: '', star: true, text: speaker.first,
  }
  const post: Post = { id: 'guest-post', text, views: 27, likes: 4, likedBy: [], authorId: 'Player_guest', comments: [firstComment] }
  if (replySubmitted) post.comments.push({
    id: 'guest-reply', parentCommentId: firstComment.id, depth: 1, author: selected.name, avatar: selected.avatarSeed,
    authorId: post.authorId, handle: '', me: true, text: reply,
  })
  if (replyDone) post.comments.push({ ...firstComment, id: 'guest-second', parentCommentId: 'guest-reply', depth: 2, text: speaker.second })
  const result: PostResult = {
    postId: post.id, delta: 4, views: post.views, headline: 'Персонаж заметил твой первый пост.', viral: false, flop: false,
    whys: [{ icon: '⭐', label: 'Герой заметил пост', value: '+4' }], ribbons: [],
  }
  useLayoutEffect(() => {
    try { localStorage.setItem(storageKey, JSON.stringify(draft)) }
    catch { setStorageAvailable(false) }
  }, [draft])

  useEffect(() => {
    if (!replySubmitted || replyDone) return
    const timeout = setTimeout(() => setDraft(current => ({ ...current, replyDone: true })),
      window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : 1050)
    return () => clearTimeout(timeout)
  }, [replySubmitted, replyDone])

  const lastGuestScreen = useRef<string | undefined>(undefined)
  const guestScreen = collected ? 'save_prompt' : replyDone ? 'reward' : replySubmitted ? 'reply_pending' : screen
  useEffect(() => {
    if (lastGuestScreen.current === guestScreen) return
    lastGuestScreen.current = guestScreen
    trackLandingAuth('landing_guest_screen', { guest_screen: guestScreen, guest_world: worldId, guest_hero: hero })
  }, [guestScreen, worldId, hero])

  const guestAction = (action: string) => trackLandingAuth('landing_guest_action', {
    guest_action: action, guest_world: worldId, guest_hero: hero,
  })

  function go(id: string, params?: Record<string, unknown>) {
    if (!Object.hasOwn(screens, id)) return
    if (id !== screen) {
      const action = ({
        '03-5_stage:02-1_compose': 'open_editor',
        '02-1_compose:02-2_live': 'publish_demo',
        '02-3_result:02-6_thread': 'open_conversation',
      } as Record<string, string>)[`${screen}:${id}`]
      if (action) guestAction(action)
    }

    setDraft(current => ({
      ...current, screen: id as Screen,
      ...(id === '02-2_live' && typeof params?.text === 'string' ? { text: params.text } : {}),
      ...(id === '02-3_result' ? { published: true } : {}),
    }))
    window.scrollTo(0, 0)
  }
  const backTo: Partial<Record<Screen, Screen>> = {
    '02-1_compose': '03-5_stage',
  }
  const nav: Nav = {
    current: screen, params: { world: worldId, firstPostConfirmed: true }, canBack: !!backTo[screen],
    push: go, replace: go, reset: go, tab: () => {},
    back: () => { const target = backTo[screen]; if (target) go(target) },
  }

  const game = useRef<HTMLElement>(null)
  useLayoutEffect(() => {
    const root = game.current
    const footer = root?.querySelector<HTMLElement>('.bottom-cta')
    if (!footer || !root || replyDone) return
    const size = () => root.style.setProperty('--guest-footer-space', `${footer.offsetHeight + 12}px`)
    const observer = new ResizeObserver(size)
    observer.observe(footer)
    size()
    return () => { observer.disconnect(); root.style.removeProperty('--guest-footer-space') }
  }, [screen, replySubmitted, replyDone])

  const View = screens[screen]
  return <NavContext.Provider value={nav}><AvatarWorld.Provider value={worldId}><GuestSessionContext.Provider value={{
    world, player, goal, post, result, published: !!published, speaker: other.name,
    postIdea: fixture.firstPostIdea, text: draft.text,
    editPost: value => setDraft(current => ({ ...current, text: value })), reply, replySubmitted: !!replySubmitted, replyDone: !!replyDone,
    editReply: value => setDraft(current => ({ ...current, reply: value })),
    sendReply: () => { guestAction('send_demo_reply'); setDraft(current => ({ ...current, reply, replySubmitted: true })) },
    crystals: collected ? 2 : 0, hint: !replyDone ? <GuestHint screen={screen} /> : undefined,
  }}>
    <main ref={game} className={replyDone ? 'app guest-finish' : 'app guest-game'}>
      <div key={screen}><View /></div>
      {!replyDone && (screen === '02-2_live' || (screen === '02-6_thread' && replySubmitted)) && <GuestHint screen={screen} inline={false} />}
      {replyDone && <GuestFinish worldId={worldId} selected={selected} avatarStyle={world.avatarStyle} collected={!!collected} onClaim={() => guestAction('claim_demo_reward')}
        onCollect={() => setDraft(current => ({ ...current, collected: true }))} onSave={() => onSave({ text, reply, hero })} />}
      {!storageAvailable && <p className="small muted pad" role="status">Хранилище недоступно. Черновик останется в этой вкладке до обновления страницы.</p>}
    </main>
  </GuestSessionContext.Provider></AvatarWorld.Provider></NavContext.Provider>
}
