import {eveningTemplateId} from './eveningAppearance';
export const customLooks=[
  {id:'custom-man-1',label:'Мужской образ 1'},
  {id:'custom-man-2',label:'Мужской образ 2'},
  {id:'custom-woman-1',label:'Женский образ 1'},
  {id:'custom-woman-2',label:'Женский образ 2'},
];
export const eveningLooks=[
  {id:'evening-man-1',label:'Парень в зелёной дождевке',description:'Светлокожий юноша с веснушками, короткими русыми волосами и серо-голубыми глазами. Зелёная дождевка поверх серой футболки.'},
  {id:'evening-man-2',label:'Парень в синей дождевке',description:'Юноша со светло-оливковой кожей, короткими чёрными волосами и карими глазами. Синяя дождевка поверх бордового джемпера.'},
  {id:'evening-woman-1',label:'Девушка в голубой дождевке',description:'Светлокожая девушка с длинными каштановыми волосами и серо-зелёными глазами. Серо-голубая дождевка поверх светлого вязаного свитера.'},
  {id:'evening-woman-2',label:'Девушка в сливовой дождевке',description:'Светлокожая девушка с веснушками, короткими волнистыми рыжими волосами и ореховыми глазами. Сливовая дождевка поверх тёмного свитера.'},
];
export const customLooksFor=(worldId?:string)=>worldId===eveningTemplateId?eveningLooks:customLooks;
export const customLook=(seed:string,worldId?:string)=>customLooksFor(worldId).find(look=>look.id===seed)??customLooksFor(worldId)[0];
export const customPortrait=(seed:string,worldId?:string)=>{
  const id=customLook(seed,worldId).id;
  return `/assets/avatars/3d/${id}-${id.startsWith('evening-')?'v10':'v9'}.png`;
};
