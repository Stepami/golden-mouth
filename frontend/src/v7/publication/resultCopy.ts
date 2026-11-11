export const growthPhrases=[
 'Твой канал становится заметнее.',
 'К разговору присоединяются новые читатели.',
 'У твоих слов появилась новая аудитория.',
 'Ещё один шаг к большему каналу.',
 'Твоя аудитория стала больше.',
 'Новый отклик — новые подписчики.',
 'Теперь с тобой больше читателей.',
 'Канал набирает аудиторию.',
 'Твой круг читателей расширился.',
 'Разговор привлёк новых подписчиков.',
];
export const declinePhrases=[
 'Состав аудитории изменился.',
 'Не каждый разговор подходит всем.',
 'Подписчиков стало меньше. История продолжается.',
 'Один ход не определяет весь канал.',
 'Следующий пост — возможность сменить угол.',
 'Этот ход изменил круг читателей.',
 'У следующего разговора будет свой отклик.',
 'Можно попробовать другой тон.',
 'Твой голос остаётся с тобой.',
 'Есть повод посмотреть на отклики.',
];
export function pickResultPhrase(delta:number){
 const phrases=delta>0?growthPhrases:delta<0?declinePhrases:undefined;
 return phrases?phrases[Math.floor(Math.random()*phrases.length)]:'Аудитория без изменений. Разговор продолжается.';
}
export function audiencePercent(delta:number,before?:number){
 if(before===undefined||before<=0)return undefined;
 const value=delta/before*100;
 if(value!==0&&Math.abs(value)<.1)return `${value>0?'+':'−'}<0,1%`;
 return `${value>0?'+':value<0?'−':''}${new Intl.NumberFormat('ru',{maximumFractionDigits:1}).format(Math.abs(value))}%`;
}
