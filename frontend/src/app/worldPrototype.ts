import type { Drama, WorldDefinition } from '../api/types'
import { worldDefinitionOf } from '../api/worldContent'
import { blankWorld } from './worldDraft'

export { worldArt } from './worldArt'

export const dramaOptions: { id: Drama; icon: string; title: string; description: string; example: string }[] = [
  { id: 'soft', icon: '🌤', title: 'Мягко', description: 'Тёплые знакомства, поддержка и небольшие приключения.', example: 'Неожиданная находка становится поводом сблизиться.' },
  { id: 'mid', icon: '🌊', title: 'Средне', description: 'Споры, неожиданные повороты и непростые решения.', example: 'У друзей разные версии событий. Чью сторону выберешь?' },
  { id: 'hot', icon: '⚡', title: 'Остро', description: 'Соперничество, интриги и напряжённые отношения.', example: 'Твой пост меняет расстановку сил — и нравится не всем.' },
  { id: 'off', icon: '🌿', title: 'Без хейта', description: 'Приключения и разногласия без нападок на твой канал.', example: 'Герои могут спорить, но не устраивают травлю.' },
]

/** Explicit demo content, not an AI generator. Never sent by the quick prototype. */
export function prototypeWorld(source: WorldDefinition): WorldDefinition {
  const common = worldDefinitionOf('b3a1a522-0263-4796-a197-57a8731fc291')
  return { ...blankWorld(), ...source, where: source.where || 'твоём мире', name: source.name || 'Мой дневник', avatar: source.avatar || 'my-story', role: source.role || 'Автор своей истории', voice: source.voice || 'Живо, с точными наблюдениями и добрым юмором',
    firstContext: source.firstContext || `В мире «${source.title || 'Моя история'}» появляется запись о неожиданной находке. В комментариях уже спорят, что она означает.`, firstPrompt: source.firstPrompt || 'Какую версию событий ты расскажешь в первом посте?',
    roles: source.roles.length ? source.roles : ['Исследователь', 'Автор хроники', 'Собиратель историй'],
    postIdeas: source.postIdeas.length ? source.postIdeas : ['Сегодня я заметил кое-что необычное…', 'Кажется, у этой истории есть другая сторона…', 'Мне нужна ваша помощь, чтобы разобраться…'],
    demoPosts: source.demoPosts.length ? source.demoPosts : ['Нашёл необычную вещь. Кто-нибудь знает, откуда она?', 'Вчера все были уверены в одной версии. Сегодня — уже нет.', 'Мы решили проверить слух вместе. Записываю всё в ленту.'],
    stars: source.stars.length ? source.stars : [
      { id: 'guide', name: 'Саша', role: 'Проводник по миру', about: 'Замечает необычные детали и помогает проверить первую версию событий.', voice: 'Тепло, с конкретными наблюдениями', avatarSeed: 'world-guide', trait: 1, relation: 0 },
      { id: 'seeker', name: 'Женя', role: 'Искатель историй', about: 'Собирает находки и охотно спорит о том, что они означают.', voice: 'С любопытством и самоиронией', avatarSeed: 'world-seeker', trait: 4, relation: 0 },
    ], wants: source.wants.length ? source.wants : common.wants.map(value => ({ ...value, done: false })), achievements: source.achievements.length ? source.achievements : common.achievements.map(value => ({ ...value, open: false })),
  }
}
export const isDevWorldPrototype = import.meta.env.DEV && ['localhost', '127.0.0.1'].includes(location.hostname)
