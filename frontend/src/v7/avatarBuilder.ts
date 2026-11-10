import {worldArt} from '../app/worldArt';

export const customLooks=[
  {id:'custom-man-1',label:'Мужской образ 1',eyes:[.431,.567,.289]},
  {id:'custom-man-2',label:'Мужской образ 2',eyes:[.431,.566,.285]},
  {id:'custom-woman-1',label:'Женский образ 1',eyes:[.426,.566,.308]},
  {id:'custom-woman-2',label:'Женский образ 2',eyes:[.438,.574,.317]},
];
export const customLook=(seed:string)=>customLooks.find(look=>look.id===seed)??customLooks[0];
export const customPortrait=(seed:string)=>`/assets/avatars/3d/${customLook(seed).id}-v9.png`;
export type AvatarBackground='none'|'world'|'blue'|'plum';
export type AvatarGlasses='none'|'round'|'square';
const loadImage=async(src:string)=>{const image=new Image();image.src=src;await image.decode();return image;};

/** Compose reusable cutouts and accessory geometry; this does not generate a new face. */
export async function composeAvatar(seed:string,background:AvatarBackground,glasses:AvatarGlasses,worldId:string){
  const canvas=document.createElement('canvas');canvas.width=640;canvas.height=640;
  const ctx=canvas.getContext('2d')!;
  const world=worldArt[worldId];
  if(background==='world'&&world){
    const image=await loadImage(world.smallCover),side=Math.min(image.width,image.height);
    ctx.drawImage(image,(image.width-side)/2,(image.height-side)/2,side,side,0,0,640,640);
    ctx.fillStyle='rgba(0,0,0,.3)';ctx.fillRect(0,0,640,640);
  }else if(background!=='none'){
    const gradient=ctx.createLinearGradient(0,0,640,640);
    gradient.addColorStop(0,background==='plum'?'#66425f':'#365366');gradient.addColorStop(1,'#15191f');
    ctx.fillStyle=gradient;ctx.fillRect(0,0,640,640);
  }
  ctx.drawImage(await loadImage(customPortrait(seed)),0,0,640,640);
  if(glasses!=='none'){
    const [left,right,y]=customLook(seed).eyes.map(n=>n*640),width=(right-left)*.83,height=width*.65;
    ctx.lineWidth=3;ctx.strokeStyle=glasses==='round'?'#b8a487':'#25272b';ctx.fillStyle='rgba(187,217,228,.09)';
    for(const x of [left,right]){
      ctx.beginPath();if(glasses==='round')ctx.ellipse(x,y+3,width/2,height/2,0,0,Math.PI*2);else ctx.roundRect(x-width/2,y+3-height/2,width,height,8);ctx.fill();ctx.stroke();
    }
    ctx.beginPath();ctx.moveTo(left+width/2,y);ctx.quadraticCurveTo((left+right)/2,y-5,right-width/2,y);ctx.stroke();
    ctx.beginPath();ctx.moveTo(left-width/2,y-4);ctx.lineTo(left-width*.7,y-8);ctx.moveTo(right+width/2,y-4);ctx.lineTo(right+width*.7,y-8);ctx.stroke();
  }
  return canvas.toDataURL('image/webp',.9);
}
