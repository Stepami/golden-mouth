// Демо-посты и фикстуры экранов. Миры и звёзды загружаются из API.
import type { Challenge, Dm, Friend, GameEvent, Player, Post, PostResult } from './types'

export const player: Player = {
  id: '00000000000000000000000000000001', name: 'Лиза · трейни', handle: '@liza.trainee', role: 'трейни', address: 'she',
  world: 'stage', subs: 120, postsLeft: 10, postsCap: 10, firstPostInviteBonus: 0,
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
  drama: 'mid', voice: '«коротко, с самоиронией»',
  settings: { notifications: 'до 3 в день', quiet: '22:00–9:00', plus: false },
}

export const firstPost: Post = {
  id: 'post1', text: 'первый день трейни: перепутала право и лево три раза. хореограф сказал «зато уверенно» 🫠', views: 312,
  reactions: [{ emoji: '😂', count: 48 }, { emoji: '🫶', count: 21 }, { emoji: '💀', count: 9 }], at: 'только что', canReply: true,
  comments: [
    { id: 'k1', authorId: 'mira', depth: 0, parentCommentId: null, author: 'Мира', avatar: 'mira', handle: 'лидер BLESK', text: 'я на первой репетиции упала со сцены. «уверенно» это комплимент, поверь', star: true },
    { id: 'k2', parentCommentId: 'k1', authorId: player.id, depth: 1, reactions: [{ authorId: 'mira', author: 'Мира', avatar: 'mira', star: true, me: false, emoji: '💛' }], author: 'Лиза · трейни', avatar: '@liza.trainee', handle: '', text: 'серьёзно? со сцены?? 😳', me: true },
    { id: 'k3', authorId: 'mira', parentCommentId: 'k2', depth: 2, author: 'Мира', avatar: 'mira', handle: 'лидер BLESK', text: 'прямо в оркестровую яму. потом расскажу 🙃', star: true },
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

const postsById: Record<string, Post> = { [firstPost.id]: firstPost }
/** Пост по стабильному id для GET /posts/:id в mock-режиме; неизвестный id = 404. */
export const post = (postId: string): Post => {
  const found = postsById[postId]
  if (!found) throw Object.assign(new Error(`post ${postId} not found`), { status: 404 })
  return structuredClone(found)
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

export const dm: Omit<Dm, 'star'> = { status: 'заинтригована', memory: ['хореография', '«зато уверенно»'], repliesLeft: 3,
  messages: [{ from: 'star', text: 'твой пост про «уверенно» разлетелся по агентству 😏' }, { from: 'me', text: 'я сама в шоке' }, { from: 'star', text: 'снимем вместе разбор моей самой позорной репетиции? ты комментируешь' }],
  ideas: ['«а если я опозорюсь?»', '«только если покажешь падение»'],
}

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
