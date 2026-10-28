import type { WorldDefinition, WorldDocument } from '../api/types'
import { optionalWorldTemplate } from '../api/worldContent'

export const presetGoals: Record<string, { goal: string; firstStep: string }> = {
  'b3a1a522-0263-4796-a197-57a8731fc291': {
    goal: 'Дать Изабелле и Эдуарду высказаться о случившемся после грозового бейсбола.',
    firstStep: 'Начни обсуждение сорванного бейсбола: спроси участников, что они готовы рассказать сами.',
  },
  'aec9494b-4118-4525-8458-e85d759c8dd9': {
    goal: 'Услышать вклад каждого в спасение от тролля и найти друзей в школе магии.',
    firstStep: 'Расскажи, что тебе известно о тролле, и предложи участникам дополнить историю.',
  },
  '8c02a7fb-3b55-4eb0-b957-1b4869ed9834': {
    goal: 'Разобраться, кто готов действовать против мёртвых и за какие обещания отвечает лично.',
    firstStep: 'После встречи в Драконьем Логове обсуди, какие личные обязательства участники готовы принять.',
  },
  '5720946a-ea7c-4a10-9c50-51ce3deefc53': {
    goal: 'Заслужить доверие к своему разбору «Смены»: сравнить цену обещаний и результат проверки.',
    firstStep: 'Опубликуй свою позицию о «Смене»: что нужно проверить, прежде чем верить обещанию заменить команду.',
  },
}

/** Character/voice/drama choices keep the story; edited lore must not inherit its goal. */
export function presetGoalFor(world: WorldDefinition | WorldDocument) {
  const template = optionalWorldTemplate(world)
  return template ? presetGoals[template.id] : undefined
}
