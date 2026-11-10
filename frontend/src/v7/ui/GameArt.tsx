import './gameArt.css';

// Square source regions surround each complete silhouette with 12px of space.
// The illustrations are not centred on the atlas's nominal four-by-four grid.
const regions={energy:[42,53,266],crystal:[338.5,51,272],flame:[638.5,51,275],followers:[944,76.5,273],gift:[39.5,356,259],booster:[343,359,245],courage:[638,353,270],humor:[955,368,249],context:[32.5,656,267],diplomacy:[338,657,263],empathy:[641,657,269],argument:[947,651,274],precision:[31,958,267],knowledge:[344,960,259],voice:[646,952,273],direction:[946,955,271]} as const;
export type ArtKind=keyof typeof regions;

/** A shared atlas keeps material, light and scale consistent across the app. */
export function GameArt({kind,className=''}:{kind:ArtKind;className?:string}) {
 const [x,y,size]=regions[kind];
 return <svg className={`game-art game-art--${kind} ${className}`} width="96" height="96" viewBox={`${x} ${y} ${size} ${size}`} aria-hidden="true" focusable="false"><image href="/assets/game-art/icons-gloss.png" width="1254" height="1254"/></svg>;
}

const families:Record<ArtKind,string[]>={
 energy:[],crystal:[],flame:[],followers:[],gift:[],booster:[],
 courage:['Смелость','Публичная позиция','Независимость','Инициатива','Выдержка','Самообладание','Напор','Решительность','Настойчивость','Стойкость','Держать удар','Прямота'],
 humor:['Юмор','Фристайл','Самоирония','Сатира','Сухой панч','Находчивость','Смекалка'],
 context:['В тему','Сопоставление','Технический вопрос'],
 diplomacy:['Общение','Дипломатия','Согласование','Согласование условий','Переговоры об огласке','Переговоры','Проверка условий','Торг','Пробить фильтр'],
 empathy:['Эмпатия','Чуткость','Деликатность','Признание ошибок','Лучше слышать других','Солидарность','Прогрев'],
 argument:['Убеждение','Аргументация','Публичный спор','Прямой разговор','Публичный вопрос','Проверка гарантий','Доказательная база','Каверзный вопрос','Дожим'],
 precision:['Внимание','Точность','Проверка расходов','Наблюдательность','Проверка версий','Проверка источника','Слух к деталям','Сверка источников','Проверка подписи','Расчёт выгоды','Проверка квитанции','Редакторская точность','Воспроизводимость','Проницательность','Внимание к деталям','Редакторское чутьё','Чутьё на инфоповод','Нюх на буллшит','Глаз рекрутера','Второй вопрос'],
 knowledge:['Местная память','Эрудиция','Критическое мышление','Расчёт','Расчёт смены'],
 voice:['Самовыражение','Харизма','Собственный голос','Подача','Влияние','Изобретательность'],
 direction:['Сверка маршрутов','Позиция дома','Исполнимость','Лидерство','Стратегия','Хитрость','Чутьё на деньги'],
};
const skillKinds=Object.fromEntries(Object.entries(families).flatMap(([kind,names])=>names.map(name=>[name,kind as ArtKind])));
export function SkillArt({name}:{name:string}) { return <GameArt kind={skillKinds[name]??'direction'} className="skill-art"/>; }
