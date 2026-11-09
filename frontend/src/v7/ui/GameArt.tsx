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
 courage:['Смелость','Публичная позиция','Независимость','Инициатива','Выдержка'],
 humor:['Юмор','Фристайл','Самоирония','Сатира'],
 context:['В тему','Сопоставление','Технический вопрос'],
 diplomacy:['Дипломатия','Согласование','Согласование условий','Переговоры об огласке','Переговоры','Проверка условий'],
 empathy:['Эмпатия','Чуткость','Деликатность','Признание ошибок','Лучше слышать других'],
 argument:['Аргументация','Публичный спор','Прямой разговор','Публичный вопрос','Проверка гарантий'],
 precision:['Точность','Проверка расходов','Наблюдательность','Проверка версий','Проверка источника','Слух к деталям','Сверка источников','Проверка подписи','Расчёт выгоды','Проверка квитанции','Редакторская точность','Воспроизводимость'],
 knowledge:['Местная память'],voice:['Харизма','Собственный голос','Подача'],direction:['Сверка маршрутов','Позиция дома','Исполнимость'],
};
const skillKinds=Object.fromEntries(Object.entries(families).flatMap(([kind,names])=>names.map(name=>[name,kind as ArtKind])));
export function SkillArt({name}:{name:string}) { return <GameArt kind={skillKinds[name]??'direction'} className="skill-art"/>; }
