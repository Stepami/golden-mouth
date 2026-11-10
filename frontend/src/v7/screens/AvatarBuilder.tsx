import {useEffect,useRef,useState} from 'react';
import {Chip} from '../ui';
import {composeAvatar,customLooks,customPortrait,type AvatarBackground,type AvatarGlasses} from '../avatarBuilder';

export function AvatarBuilder({seed,worldId,onLook,onRender}:{seed:string;worldId:string;onLook:(seed:string)=>void;onRender:(src:string)=>void}){
  const [background,setBackground]=useState<AvatarBackground>('none'),[glasses,setGlasses]=useState<AvatarGlasses>('none'),[error,setError]=useState('');
  const render=useRef(onRender);render.current=onRender;
  useEffect(()=>{let active=true;setError('');void composeAvatar(seed,background,glasses,worldId).then(src=>{if(active)render.current(src);}).catch(()=>{if(active)setError('Не удалось собрать образ. Выбери другой вариант.');});return()=>{active=false;};},[seed,background,glasses,worldId]);
  return <div className="avatar-builder">
    <h3>Выбери свой образ</h3>
    <div className="studio-looks">{customLooks.map(look=><button key={look.id} aria-label={look.label} aria-pressed={seed===look.id||seed==='custom'&&look===customLooks[0]} onClick={()=>onLook(look.id)}><img src={customPortrait(look.id)} alt=""/></button>)}</div>
    <h4>Фон</h4><div className="avatar-builder-options">{([['none','Без фона'],['world','Этот мир'],['blue','Синий'],['plum','Сливовый']] as const).map(([id,label])=><Chip key={id} selected={background===id} onClick={()=>setBackground(id)}>{label}</Chip>)}</div>
    <h4>Очки</h4><div className="avatar-builder-options">{([['none','Без очков'],['round','Круглые'],['square','Прямоугольные']] as const).map(([id,label])=><Chip key={id} selected={glasses===id} onClick={()=>setGlasses(id)}>{label}</Chip>)}</div>
    {error&&<p role="alert">{error}</p>}
  </div>;
}
