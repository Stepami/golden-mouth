import type { WorldDefinition } from '../api/types'
import { optionalWorldTemplate } from '../api/worldContent'

export const presetGoals: Record<string, { goal: string; firstStep: string }> = {
  evening: {
    goal: 'Раскрыть тайну исчезающих улиц и найти тех, кто помнит город таким же, как ты.',
    firstStep: 'Опубликуй находку на фотографии и спроси жителей, кто узнаёт исчезнувшую улицу.',
  },
  sorcerers: {
    goal: 'Разгадать тайну меняющейся школы и найти друзей, которым можно доверять.',
    firstStep: 'Расскажи об уроке, отменённом сто лет назад, и выясни, кому ещё пришло приглашение.',
  },
  houses: {
    goal: 'Стать голосом, к которому прислушиваются великие дома, и разобраться, кому верить в борьбе за престол.',
    firstStep: 'Расскажи о двух письмах с королевской печатью и спроси, почему подписи на них разные.',
  },
  it: {
    goal: 'Найти своих в айти и стать автором, чьим наблюдениям и идеям доверяют.',
    firstStep: 'Поделись случаем из проекта, эксперимента или работы и предложи героям обсудить твою версию.',
  },
}

/** Character/voice/drama choices keep the story; edited lore must not inherit its goal. */
export function presetGoalFor(world: WorldDefinition, template = optionalWorldTemplate(world.balanceKey)) {
  if (!template || !presetGoals[world.balanceKey]) return undefined
  const description = world.description.split('\nПерсона игрока: ')[0]
  if (description !== template.description || world.title !== template.title || world.where !== template.where
    || world.firstContext !== template.firstContext || world.firstPrompt !== template.firstPrompt) return undefined
  return presetGoals[world.balanceKey]
}
