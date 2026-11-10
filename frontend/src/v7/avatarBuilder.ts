import {eveningTemplateId} from './eveningAppearance';
export const customLooks=[
  {id:'custom-man-1',label:'Мужской образ 1',eyes:[.431,.567,.289]},
  {id:'custom-man-2',label:'Мужской образ 2',eyes:[.431,.566,.285]},
  {id:'custom-woman-1',label:'Женский образ 1',eyes:[.426,.566,.308]},
  {id:'custom-woman-2',label:'Женский образ 2',eyes:[.438,.574,.317]},
];
export const eveningLooks=[
  {id:'evening-man-1',label:'Парень в зелёной дождевке',eyes:[.426,.574,.331],description:'Светлокожий юноша с веснушками, короткими русыми волосами и серо-голубыми глазами. Зелёная дождевка поверх серой футболки.'},
  {id:'evening-man-2',label:'Парень в синей дождевке',eyes:[.428,.572,.329],description:'Юноша со светло-оливковой кожей, короткими чёрными волосами и карими глазами. Синяя дождевка поверх бордового джемпера.'},
  {id:'evening-woman-1',label:'Девушка в голубой дождевке',eyes:[.424,.573,.35],description:'Светлокожая девушка с длинными каштановыми волосами и серо-зелёными глазами. Серо-голубая дождевка поверх светлого вязаного свитера.'},
  {id:'evening-woman-2',label:'Девушка в сливовой дождевке',eyes:[.426,.571,.325],description:'Светлокожая девушка с веснушками, короткими волнистыми рыжими волосами и ореховыми глазами. Сливовая дождевка поверх тёмного свитера.'},
];
export const customLooksFor=(worldId?:string)=>worldId===eveningTemplateId?eveningLooks:customLooks;
export const customLook=(seed:string,worldId?:string)=>customLooksFor(worldId).find(look=>look.id===seed)??customLooksFor(worldId)[0];
export const customPortrait=(seed:string,worldId?:string)=>{
  const id=customLook(seed,worldId).id;
  return `/assets/avatars/3d/${id}-${id.startsWith('evening-')?'v10':'v9'}.png`;
};
export type AvatarGlasses='none'|'round'|'square';
const loadImage=async(src:string)=>{const image=new Image();image.src=src;await image.decode();return image;};

/** Compose reusable cutouts and accessory geometry; this does not generate a new face. */
export async function composeAvatar(seed:string,glasses:AvatarGlasses,worldId?:string){
  const canvas=document.createElement('canvas');canvas.width=640;canvas.height=640;
  const ctx=canvas.getContext('2d')!;
  ctx.drawImage(await loadImage(customPortrait(seed,worldId)),0,0,640,640);
  if(glasses!=='none'){
    const [left,right,y]=customLook(seed,worldId).eyes.map(n=>n*640),width=(right-left)*.83,height=width*.65;
    ctx.lineWidth=3;ctx.strokeStyle=glasses==='round'?'#b8a487':'#25272b';ctx.fillStyle='rgba(187,217,228,.09)';
    for(const x of [left,right]){
      ctx.beginPath();if(glasses==='round')ctx.ellipse(x,y+3,width/2,height/2,0,0,Math.PI*2);else ctx.roundRect(x-width/2,y+3-height/2,width,height,8);ctx.fill();ctx.stroke();
    }
    ctx.beginPath();ctx.moveTo(left+width/2,y);ctx.quadraticCurveTo((left+right)/2,y-5,right-width/2,y);ctx.stroke();
    ctx.beginPath();ctx.moveTo(left-width/2,y-4);ctx.lineTo(left-width*.7,y-8);ctx.moveTo(right+width/2,y-4);ctx.lineTo(right+width*.7,y-8);ctx.stroke();
  }
  return canvas.toDataURL('image/webp',.9);
}
