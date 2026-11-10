import {useEffect,useRef} from 'react';
import {customLook,customLooksFor,customPortrait} from '../avatarBuilder';

export function AvatarBuilder({seed,onLook,onRender,worldId}:{seed:string;worldId?:string;onLook:(seed:string)=>void;onRender:(src:string)=>void}){
  const render=useRef(onRender);render.current=onRender;
  useEffect(()=>{render.current(customPortrait(seed,worldId));},[seed,worldId]);
  return <div className="avatar-builder">
    <h3>Выбери свой образ</h3>
    <div className="studio-looks">{customLooksFor(worldId).map(look=><button key={look.id} aria-label={look.label} aria-pressed={customLook(seed,worldId).id===look.id} onClick={()=>onLook(look.id)}><img src={customPortrait(look.id,worldId)} alt=""/></button>)}</div>
  </div>;
}
