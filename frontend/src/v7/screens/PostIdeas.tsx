import {useState} from 'react';
import {roleScenarios} from '../story/roles';
import type {WorldDocument} from '../../api/types';
import {Button,Chip,Dialog} from '../ui';

const ideas:Record<string,string[]>={
  'aec9494b-4118-4525-8458-e85d759c8dd9':[
    'Про Хогвартс спорят так, будто замок уже пустой. Спросим тех, кто ещё здесь живёт: что вам точно нельзя потерять?',
    'У каждого готов план для Хогвартса. Назовите одну вещь, за которую вы готовы отвечать сами. Я начну: ',
    'Представим: завтра на воротах Хогвартса повесят новую вывеску. Что на ней будет написано? Мой вариант: ',
  ],
  'b3a1a522-0263-4796-a197-57a8731fc291':[
    'На видео — ладонь на вмятине. Всё остальное мы уже додумали. Кто видел, что произошло за секунду до записи?',
    'На записи видно одно, в пересказах уже десять версий. Какой вопрос поможет отделить свидетеля от любителя слухов?',
    'Город уже раскрыл тайну, назначил виноватых и нарисовал мемы. А кто-нибудь поговорил с самой Изабеллой?',
  ],
  '8c02a7fb-3b55-4eb0-b957-1b4869ed9834':[
    'Все требуют клятву. А можно сначала услышать, что именно каждая сторона обещает взамен?',
    'Если союз держится на том, что никто не задаёт неудобных вопросов, у меня плохая новость. Мой вопрос: ',
    'Красиво назвать себя защитником Севера легко. Какое действие докажет это раньше следующего совета?',
  ],
  '5720946a-ea7c-4a10-9c50-51ce3deefc53':[
    '«Работа без опыта» — это обещание курса или название вакансии, которую никто не видел? Покажите условия.',
    'В рекламе — новая жизнь за три месяца. В комментариях — люди без работы. Предлагаю разобрать один конкретный случай.',
    'Пока вы меряетесь охватами, человек отправляет сотое резюме. Что ему нужно показать, чтобы попасть на собеседование?',
  ],
};
const ideaLabels:Record<string,string[]>={
 'aec9494b-4118-4525-8458-e85d759c8dd9':['Судьба Хогвартса','Личная ответственность','Новая вывеска'],
 'b3a1a522-0263-4796-a197-57a8731fc291':['Свидетели спасения','Проверить слухи','Спросить Изабеллу'],
 '8c02a7fb-3b55-4eb0-b957-1b4869ed9834':['Условия клятвы','Неудобный вопрос','Защита Севера'],
 '5720946a-ea7c-4a10-9c50-51ce3deefc53':['Обещания курса','Разобрать случай','Сотое резюме'],
};
function labelForIdea(text:string,index:number){
 for(const [world,values] of Object.entries(ideas)){
  const position=values.indexOf(text);
  if(position>=0)return ideaLabels[world][position];
 }
 return `Идея ${index+1}`;
}
export function ideasForWorld(world:WorldDocument){
  const scenario=roleScenarios.find(s=>s.title===world.title||world.firstContext.startsWith(s.opening));
  const legacy:Record<string,string>={'Айти-тусовка':'5720946a-ea7c-4a10-9c50-51ce3deefc53','Школа чародеев':'aec9494b-4118-4525-8458-e85d759c8dd9','Вечерело':'b3a1a522-0263-4796-a197-57a8731fc291','Игра домов':'8c02a7fb-3b55-4eb0-b957-1b4869ed9834'};
  return ideas[scenario?.templateId??legacy[world.title]??world.id]??world.postIdeas;
}
export function PostIdeas({ideas,draft,onDraft,disabled}:{ideas:string[];draft:string;onDraft:(text:string)=>void;disabled:boolean}){
  const [preview,setPreview]=useState<string>();
  const [replacement,setReplacement]=useState<string>();
  return <>{ideas.length>0&&<section className="post-ideas" aria-label="Подсказки для поста"><p className="caption muted">О чём написать</p><div className="post-ideas-grid">{ideas.slice(0,3).map((text,index)=><Chip key={text} disabled={disabled} onClick={()=>setPreview(text)}>{labelForIdea(text,index)}</Chip>)}</div></section>}
    {preview!==undefined&&<Dialog title="Идея для публикации" onClose={()=>setPreview(undefined)}><div className="stack"><p>{preview}</p><Button disabled={disabled} onClick={()=>{if(draft.trim())setReplacement(preview);else onDraft(preview);setPreview(undefined);}}>Использовать</Button><Button variant="secondary" onClick={()=>setPreview(undefined)}>Закрыть</Button></div></Dialog>}
    {replacement!==undefined&&<Dialog title="Заменить черновик?" onClose={()=>setReplacement(undefined)}><div className="stack"><p>Текущий текст заменится выбранной подсказкой.</p><Button disabled={disabled} onClick={()=>{onDraft(replacement);setReplacement(undefined);}}>Заменить</Button><Button variant="secondary" onClick={()=>setReplacement(undefined)}>Оставить мой текст</Button></div></Dialog>}
  </>;
}
