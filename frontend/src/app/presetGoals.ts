import type { WorldDefinition, WorldDocument } from '../api/types'
import { optionalWorldTemplate } from '../api/worldContent'

type PresetGoal = { goal: string; firstStep: string; context?: string }

export const presetGoals: Record<string, PresetGoal> = {
  'aec9494b-4118-4525-8458-e85d759c8dd9': {
    goal: "Добиться решения о будущем Хогвартса и отвечать за свой выбор.",
    context: "В ленте Гарри обещает вернуть школу, а Драко пересказывает предложение Люциуса о бизнесе. Оба звучат так, будто ответ очевиден.",
    firstStep: "Ответь Гарри или Драко: что нравится в его идее и какой вопрос нужно прояснить? Начать можно с любого",
  },
  'b3a1a522-0263-4796-a197-57a8731fc291': {
    goal: 'Дать Изабелле и Эдуарду высказаться о случившемся после грозового бейсбола.',
    firstStep: 'Начни обсуждение сорванного бейсбола: спроси участников, что они готовы рассказать сами.',
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

const hogwartsRoleGoals: Record<string, PresetGoal> = {
  "Гарри Поттер": {
    "goal": "Снова открыть Хогвартс.",
    "firstStep": "Опубликуй пост о возвращении Хогвартса: зачем ему снова быть школой и за что берёшься лично?"
  },
  "Гермиона Грейнджер": {
    "goal": "Добиться устойчивой и этичной школы, учитывающей разные культуры.",
    "firstStep": "Объясни, кому единые правила помешают учиться и почему. Покажи, какие потребности разных учеников нужно учесть"
  },
  "Рон Уизли": {
    "goal": "Сохранить доступ к обучению для небогатых семей.",
    "firstStep": "Ответь Гарри: какие расходы помешают бедной семье отправить ребёнка в школу, даже если уроки бесплатные?"
  },
  "Драко Малфой": {
    "goal": "Закрыть школу и превратить замок в прибыльный бизнес.",
    "firstStep": "Заинтересуй будущих гостей постом: кто и за что будет платить в отеле? Признай, что он заменит школу и что спрос ещё нужно проверить"
  }
}

/** Character/voice/drama choices keep the story; edited lore must not inherit its goal. */
export function presetGoalFor(world: WorldDefinition | WorldDocument, personaName?: string) {
  const template = optionalWorldTemplate(world)
  if (template?.id === 'aec9494b-4118-4525-8458-e85d759c8dd9') {
    const role = personaName ?? Object.keys(hogwartsRoleGoals).find(name => world.description.includes(`\nПерсона игрока: ${name}.`))
    if (role) return hogwartsRoleGoals[role]
  }
  return template ? presetGoals[template.id] : undefined
}
