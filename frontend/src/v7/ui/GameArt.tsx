import './gameArt.css';

const cells={energy:[0,0],crystal:[1,0],flame:[2,0],followers:[3,0],gift:[0,1],booster:[1,1],courage:[2,1],humor:[3,1],context:[0,2],diplomacy:[1,2],empathy:[2,2],argument:[3,2],precision:[0,3],knowledge:[1,3],voice:[2,3],direction:[3,3]} as const;
export type ArtKind=keyof typeof cells;

/** A shared atlas keeps material, light and scale consistent across the app. */
export function GameArt({kind,className=''}:{kind:ArtKind;className?:string}) {
 const [x,y]=cells[kind];
 return <svg className={`game-art ${className}`} width="96" height="96" viewBox="8 8 84 84" aria-hidden="true" focusable="false"><image href="/assets/game-art/icons-gloss.png" x={-x*100} y={-y*100} width="400" height="400"/></svg>;
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
