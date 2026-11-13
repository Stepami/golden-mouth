import type {WorldDefinition} from '../api/types';

export const eveningTemplateId='b3a1a522-0263-4796-a197-57a8731fc291';
export const eveningAppearance:Record<string,string>={
 isabella:'Изабелла Лебедева: девушка с тёмными волосами до плеч, карими глазами и тёплой кожей; сливовый вязаный свитер.',
 eduard:'Эдуард Калинин: юноша с бледной светлой кожей, короткими зачёсанными назад каштановыми волосами, янтарными глазами и выраженными мужскими чертами лица; тёмное пальто поверх синей рубашки. Выглядит молодым, но скрывает свою вампирскую природу.',
 yakov:'Яков Чернов: светлокожий юноша с прямыми тёмно-каштановыми волосами, карими глазами, без очков и бороды; красная клетчатая рубашка поверх тёмной футболки. Живёт с отцом у моря, возится с машинами. Обычный человек, без превращений и связи со стаей.',
 lida:'Лида: девушка с волнистыми рыжими волосами; синяя куртка.',
};

/** Appearance is explicit text for the existing API; image pixels are not sent to the LLM. */
export function withEveningAppearance(definition:WorldDefinition,roleId:string,customAppearance?:string):WorldDefinition {
 if(definition.templateId!==eveningTemplateId)return definition;
 const appearance=Object.values(eveningAppearance).join('\n');
 const playerAppearance=roleId==='custom' ? customAppearance : eveningAppearance[roleId];
 return {...definition,
  firstContext:`${definition.firstContext}\n\nВнешность персонажей:\n${appearance}${playerAppearance?`\nВнешность героя игрока: ${playerAppearance}`:''}`,
  rules:[...definition.rules,{text:`Сохраняй внешность персонажей при описании: ${appearance}${playerAppearance?`\nГерой игрока: ${playerAppearance}`:''} Одежда описывает начальный образ и может меняться по событиям истории. Внешность не определяет характер или согласие персонажа.`,locked:false}],
  stars:definition.stars.map(star=>eveningAppearance[star.id]?{...star,about:`${star.about}\n${eveningAppearance[star.id]}`}:star),
 };
}
