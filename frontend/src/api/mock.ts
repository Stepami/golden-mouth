// Тестовые данные = тексты и числа из макетов docs/screens. Пока бэкенд (#10) не готов, экраны живут на них.
import { WORLD_CONTENT } from './worldContent'
import type { Challenge, Dm, FeedItem, Friend, GameEvent, Player, Post, PostResult, ReplyResult, Star, World, WorldRules } from './types'

export const worlds: World[] = [
  { id: 'stage', title: 'Сцена', emoji: '🎤', description: 'Айдол-агентство: трейни, дебют, фансайты', difficulty: 1, summary: 'мало провалов · хейт с 5k · ценят смелость' },
  { id: 'academy', title: 'Академия', emoji: '🌸', description: 'Аниме-школа: клубы, соперницы, фестиваль', difficulty: 2, summary: 'рост ×1,1 · хейт с 3k · ценят «в тему»' },
  { id: 'it', title: 'Айти-тусовка', emoji: '💻', description: 'Блогеры, разоблачения, коалиции, курсы', difficulty: 3, summary: 'рост ×1,25 · хейт с 1k · больше риска' },
  { id: 'stream', title: 'Стрим', emoji: '🎮', description: 'Стримеры, донаты, рейды, турниры', difficulty: 2, summary: 'рост ×1,1 · ценят юмор', lock: { skill: 'humor', level: 4 } },
]
export const roles = ['🎤 Трейни', '📸 Фансайт', '💃 Хореограф', '🎧 Продюсер']

export const stars: Star[] = WORLD_CONTENT.stage.stars.map(profile => ({ ...profile, relation: profile.id === 'mira' ? 2 : 0 }))

export const player: Player = {
  id: '00000000000000000000000000000001', name: 'Лиза · трейни', handle: '@liza.trainee', role: 'трейни', address: 'she',
  world: 'stage', subs: 120, postsLeft: 10, postsCap: 10,
  streak: { days: 4, week: [true, true, true, true, false, false, false], nextReward: '7 дней: карточка «Неделя в эфире»' },
  skills: { brave: { level: 6, progress: .4 }, humor: { level: 5, progress: .7 }, topic: { level: 4, progress: .3 } },
  wants: [
    { id: 'w1', text: 'Пошути про хореографа', reward: 'опыт ×1,3', done: false },
    { id: 'w2', text: 'Ответь Еве в личке', reward: '+отношения', done: true },
    { id: 'w3', text: 'Пост про свою ошибку', reward: 'опыт ×1,3', done: false },
  ],
  collection: [
    { id: 'c1', emoji: '🌊', title: 'Первый залёт', open: true }, { id: 'c2', emoji: '👑', title: 'Мира', open: true },
    { id: 'c3', emoji: '💛', title: 'Соня', open: true }, { id: 'c4', emoji: '🌩', title: 'Хейт пережит', open: true },
    { id: 'c5', emoji: '🤝', title: 'Дуэт', open: true }, { id: 'c6', emoji: '', title: '', open: false },
  ],
  bio: 'первый год в агентстве. путаю право и лево, но очень стараюсь ✨', type: 'дерзкая с самоиронией',
  nextWorld: { id: 'stream', title: 'Стрим', need: '😂 юмор 4' },
  settings: { drama: 'mid', notifications: 'до 3 в день', quiet: '22:00–9:00', voice: '«коротко, с самоиронией»', plus: false },
}

export const firstPost: Post = {
  id: 'post1', text: 'первый день трейни: перепутала право и лево три раза. хореограф сказал «зато уверенно» 🫠', views: 312,
  reactions: [{ emoji: '😂', count: 48 }, { emoji: '🫶', count: 21 }, { emoji: '💀', count: 9 }], at: 'только что', canReply: true,
  comments: [
    { id: 'k1', author: 'Мира', avatar: 'mira', handle: 'лидер BLESK', text: 'я на первой репетиции упала со сцены. «уверенно» это комплимент, поверь', star: true },
    { id: 'k2', author: 'Лиза · трейни', avatar: '@liza.trainee', handle: '', text: 'серьёзно? со сцены?? 😳', me: true },
    { id: 'k3', author: 'Мира', avatar: 'mira', handle: 'лидер BLESK', text: 'прямо в оркестровую яму. потом расскажу 🙃', star: true },
    { id: 'k4', author: 'stan_critic', avatar: 'critic', handle: '', text: 'трейни пишет про ошибки? ну такое…' },
  ],
}

export const channelPosts = [{
  postId: firstPost.id,
  text: firstPost.text,
  createdAtUtc: '2026-10-07T06:09:02Z',
  views: firstPost.views,
  reactions: firstPost.reactions,
  commentCount: firstPost.comments.length,
}]
export const liveComment = { id: 'k0', author: 'kpop_dasha', avatar: 'dasha', handle: 'фанатка', text: 'ЗАТО УВЕРЕННО 😭 это теперь мой девиз' }

/** Чужой пост того же мира (03-3 → 02-6): читается, но `canReply: false` — ответы только под своими постами. */
export const foreignPost: Post = {
  id: 'post-eva', text: 'иногда самый громкий пост это тот, который так и не выложен. 🖤', views: 14000,
  reactions: [{ emoji: '🖤', count: 14000 }, { emoji: '🌙', count: 3200 }], at: '2 ч назад', canReply: false,
  comments: [
    { id: 'fe1', author: 'Ева', avatar: 'eva', handle: 'солистка NOIR', text: 'набросала в черновик и оставила. пусть живёт', star: true },
    { id: 'fe2', author: 'stan_critic', avatar: 'critic', handle: '', text: 'загадочно. или просто лень?' },
  ],
}

const postsById: Record<string, Post> = { [firstPost.id]: firstPost, [foreignPost.id]: foreignPost }
/** Пост по стабильному id для GET /posts/:id в mock-режиме; неизвестный id = 404. */
export const post = (postId: string): Post => {
  const found = postsById[postId]
  if (!found) throw Object.assign(new Error(`post ${postId} not found`), { status: 404 })
  return structuredClone(found)
}

// Тред (02-6): +9 за первые 3 ответа из макета; в mock-режиме просто считаем ответы в сессии.
let mockReplies = 0
const mockThreadReplies = new Map<string, ReplyResult>()
export const threadReply = (replyId: string): ReplyResult => {
  const existing = mockThreadReplies.get(replyId)
  if (existing) return existing
  const subscriberBonus = mockReplies < 3 ? 9 : 0
  if (subscriberBonus > 0) mockReplies += 1
  const result = {
    replyId, commentId: crypto.randomUUID(), subscriberBonus,
    awardedReplies: mockReplies, replyLimit: 3, subscriberTotal: player.subs + mockReplies * 9, reaction: '🔥',
  }
  mockThreadReplies.set(replyId, result)
  return result
}

export const results: Record<'normal' | 'flop' | 'viral', PostResult> = {
  normal: { postId: 'post1', delta: 38, headline: 'Тебя заметили: первый мем уже есть', views: 312, viral: false, flop: false,
    whys: [{ icon: '😂', label: 'смешно', value: '+28' }, { icon: '🔥', label: 'смело', value: '+10' }],
    ribbons: [{ icon: '🌊', title: 'Пост почти залетел в Реки', sub: 'не хватило 12 репостов: смелее, и залетит' }, { icon: '👑', title: 'Мира: отношения +1' }],
    bestComment: { avatar: 'mira', text: '„уверенно“ это комплимент, поверь' }, hook: 'Соня пишет ответ…' },
  flop: { postId: 'post2', delta: -4, headline: 'Агентство сделало вид, что не видело этот пост', views: 640, viral: false, flop: true,
    whys: [{ icon: '🎤', label: 'мимо темы мира', value: '−7' }, { icon: '😂', label: 'смешно', value: '+3' }],
    ribbons: [{ icon: '💪', title: 'Провал тоже опыт: опыт смелости +1', sub: 'серия дней не сгорает' }],
    bestComment: { avatar: 'critic', text: 'а при чём тут Сцена?' } },
  viral: { postId: 'post1', delta: 620, headline: 'Мира сделала репост. Фанаты BLESK пошли смотреть, кто такая Лиза', views: 48000, viral: true, flop: false,
    whys: [{ icon: '👁', label: 'охват 48k', value: '×12' }],
    ribbons: [{ icon: '🃏', title: 'Новая карточка: Первый залёт', sub: 'нажми, чтобы открыть' }, { icon: '👑', title: 'Мира: отношения +1' }],
    bestComment: { avatar: 'mira', text: 'трейни, учитесь' } },
}

export const ideas = ['«Сегодня на репетиции до меня дошло, что…»', '«Никто не предупреждал, что трейни…»']

export const dm: Dm = {
  star: stars[0], status: 'заинтригована', memory: ['хореография', '«зато уверенно»'], repliesLeft: 3,
  messages: [{ from: 'star', text: 'твой пост про «уверенно» разлетелся по агентству 😏' }, { from: 'me', text: 'я сама в шоке' }, { from: 'star', text: 'снимем вместе разбор моей самой позорной репетиции? ты комментируешь' }],
  ideas: ['«а если я опозорюсь?»', '«только если покажешь падение»'],
}

export const feed: FeedItem[] = [
  { kind: 'news', text: 'BLESK объявили камбэк. Трейни, у кого лучший пост недели, позовут на съёмки тизера 👀' },
  { kind: 'post', postId: foreignPost.id, canReply: false, author: 'Ева', avatar: 'eva', role: 'солистка NOIR', ago: '2026-10-07T04:09:02Z', text: foreignPost.text, reactions: foreignPost.reactions, views: foreignPost.views, commentCount: foreignPost.comments.length },
  { kind: 'post', postId: firstPost.id, canReply: true, author: 'Мира', avatar: 'mira', role: 'лидер BLESK', ago: channelPosts[0].createdAtUtc, text: 'репостнула к себе. трейни, учитесь 😏', repost: 'первый день трейни: перепутала право и лево…', reactions: [{ emoji: '🔥', count: 2100 }, { emoji: '😂', count: 840 }], views: firstPost.views, commentCount: firstPost.comments.length },
]

export const friends: Friend[] = [
  { id: 'katya', name: 'Катя · ахаха', avatar: 'katya', world: '🌸 Академия', subs: 5120, lastPost: 'на фестивале мой клуб занял последнее место. зато лучший баннер', pronoun: 'её' },
  { id: 'dima', name: 'Дима пишет', avatar: 'dima', world: '🎮 Стрим', subs: 1940, lastPost: 'первый стрим: 3 зрителя, двое из них мама и бот', pronoun: 'его' },
]
export const challenge: Challenge = { id: 'ch1', me: 'Лиза', rival: friends[0], day: 2, days: 3, myScore: 3.4, rivalScore: 2.8, bestPost: 'Лиза, «зато уверенно» · ×6,1' }

export const events: Record<'collab' | 'hate', GameEvent> = {
  collab: { id: 'e1', kind: 'collab', badge: 'СОБЫТИЕ ДНЯ · ДЕНЬ 2', title: 'Бренд зовёт на коллаб', text: '«Блеск-тинт» хочет пост с тобой: «образ на первый день в агентстве». Как сделаешь?',
    ideas: ['честно: «я даже не умею красить губы»', 'дерзко: «лучший тинт тот, что выжил на репетиции»'], reward: 'Справишься: карточка момента и +смелость', note: 'Это пост в твоём канале · лимит не тратит. Выбери идею или напиши свой' },
  hate: { id: 'e2', kind: 'hate', badge: 'ЭТАП: УДАР · с 5k', title: 'Событие дня: хейт-волна', text: 'Фансайт BLESK: «трейни пиарится на Мире». Хейт только про посты, никогда про тебя. Как ответишь?',
    ideas: ['«я тут ради сцены, а не ради Миры»', '«спасибо за охват 💅»', 'промолчать'], reward: 'Пережить волну: карточка момента и бонус к подписчикам', note: 'Ответ уйдёт постом в твой канал · лимит не тратит' },
}

export const ownWorld: WorldRules = {
  title: 'Свой мир', presets: ['🏫 моя школа', '💻 IT-драма', '🏰 фэнтези-академия', '⚽ футбольный клуб'],
  description: 'школа как наша, но все учителя ведут блоги, а директор тайно стример. я новенькая в 9Б',
  rules: [{ text: 'Звёзды: завуч-блогерша, директор-стример, отличница-сплетница' }, { text: 'Фанаты: весь чат параллели, обожают драму' }, { text: 'Тон: смешно и по-доброму' },
    { text: 'События: контрольная, дискотека, слив переписки' }, { text: 'Без тем 18+ и травли по внешности', locked: true }],
}

/** Правила готового мира: видны игроку, правится только градус драмы (подход Кумена, #9). */
export const presetRules: WorldRules = {
  title: 'Сцена', presets: [], description: 'Айдол-агентство: трейни, дебют, камбэки, фансайты',
  rules: [{ text: 'Звёзды: Мира (дерзкая, ценит смелость), Соня (добрая, ценит юмор), Ева (загадочная, ценит «в тему»)', locked: true },
    { text: 'Фанаты: фансайты BLESK и NOIR, любят камбэки и честность', locked: true }, { text: 'Тон: поддержка до 1k, честная критика с 1k, хейт-волны с 5k', locked: true },
    { text: 'События: коллабы с брендами, слухи фансайтов, прослушивания, хейт-волны', locked: true }, { text: 'Без тем 18+ и травли по внешности', locked: true }],
}

/** Мир игрока с падежами для заголовков: «Лента Сцены», «В Академию». */
export const WORLD_FORMS: Record<string, { gen: string; acc: string }> = {
  stage: { gen: 'Сцены', acc: 'Сцену' }, academy: { gen: 'Академии', acc: 'Академию' }, it: { gen: 'Айти-тусовки', acc: 'Айти-тусовку' },
  stream: { gen: 'Стрима', acc: 'Стрим' }, own: { gen: 'своего мира', acc: 'свой мир' },
}
export const worldOf = (id: string) => { const w = worlds.find(x => x.id === id) ?? (id === 'own' ? { ...worlds[0], id: 'own', title: 'Свой мир', emoji: '✨' } : worlds[0]); return { ...w, ...(WORLD_FORMS[w.id] ?? WORLD_FORMS.stage) } }
