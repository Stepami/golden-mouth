import type { WorldDefinition, WorldDocument } from '../api/types'
import { optionalWorldTemplate } from '../api/worldContent'

export const presetGoals: Record<string, { goal: string; firstStep: string }> = {
  'b3a1a522-0263-4796-a197-57a8731fc291': {
    goal: 'Раскрыть тайну исчезающих улиц и найти тех, кто помнит город таким же, как ты.',
    firstStep: 'Опубликуй находку на фотографии и спроси жителей, кто узнаёт исчезнувшую улицу.',
  },
  'aec9494b-4118-4525-8458-e85d759c8dd9': {
    goal: 'Разгадать тайну меняющейся школы и найти друзей, которым можно доверять.',
    firstStep: 'Расскажи об уроке, отменённом сто лет назад, и выясни, кому ещё пришло приглашение.',
  },
  '8c02a7fb-3b55-4eb0-b957-1b4869ed9834': {
    goal: 'Стать голосом, к которому прислушиваются великие дома, и разобраться, кому верить в борьбе за престол.',
    firstStep: 'Расскажи о двух письмах с королевской печатью и спроси, почему подписи на них разные.',
  },
  '5720946a-ea7c-4a10-9c50-51ce3deefc53': {
    goal: 'Найти своих в айти и стать автором, чьим наблюдениям и идеям доверяют.',
    firstStep: 'Поделись случаем из проекта, эксперимента или работы и предложи героям обсудить твою версию.',
  },
}

/** Character/voice/drama choices keep the story; edited lore must not inherit its goal. */
export function presetGoalFor(world: WorldDefinition | WorldDocument) {
  const template = optionalWorldTemplate(world)
  return template ? presetGoals[template.id] : undefined
}
