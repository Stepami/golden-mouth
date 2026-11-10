import {useEffect,useRef,useState} from 'react';
import {Chip} from '../ui';
import {composeAvatar,customLooksFor,customPortrait,type AvatarGlasses} from '../avatarBuilder';

export function AvatarBuilder({seed,onLook,onRender,worldId}:{seed:string;worldId?:string;onLook:(seed:string)=>void;onRender:(src:string,glasses:AvatarGlasses)=>void}){
  const [glasses,setGlasses]=useState<AvatarGlasses>('none'),[error,setError]=useState('');
  const render=useRef(onRender);render.current=onRender;
  useEffect(()=>{let active=true;setError('');void composeAvatar(seed,glasses,worldId).then(src=>{if(active)render.current(src,glasses);}).catch(()=>{if(active)setError('Не удалось собрать образ. Выбери другой вариант.');});return()=>{active=false;};},[seed,glasses,worldId]);
  return <div className="avatar-builder">
    <h3>Выбери свой образ</h3>
    <div className="studio-looks">{customLooksFor(worldId).map(look=><button key={look.id} aria-label={look.label} aria-pressed={seed===look.id||seed==='custom'&&look===customLooksFor(worldId)[0]} onClick={()=>onLook(look.id)}><img src={customPortrait(look.id,worldId)} alt=""/></button>)}</div>
    <h4>Очки</h4><div className="avatar-builder-options">{([['none','Без очков'],['round','Круглые'],['square','Прямоугольные']] as const).map(([id,label])=><Chip key={id} selected={glasses===id} onClick={()=>setGlasses(id)}>{label}</Chip>)}</div>
    {error&&<p role="alert">{error}</p>}
  </div>;
}
