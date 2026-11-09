import {Avatar,Icon} from '../ui';
import {useEffect,useRef,useState} from 'react';
export type ReactionNotice={id:string;postId:string;worldId:string;kind:'reaction'|'mention'|'follow';actorName:string;text:string;relation?:number;local?:boolean};
export function ReactionTicker({notice,onOpen,onClose,inline=false,read=false}:{notice:ReactionNotice;onOpen:()=>void;onClose:()=>void;inline?:boolean;read?:boolean}){
 const [paused,setPaused]=useState(false);const close=useRef(onClose);close.current=onClose;
 useEffect(()=>{if(inline||paused)return;const timer=setTimeout(()=>close.current(),6000);return()=>clearTimeout(timer);},[notice.id,inline,paused]);
 return <aside className={`reaction-ticker${inline?' reaction-ticker--inline':''}`} role={inline?'article':'status'} onPointerEnter={()=>setPaused(true)} onPointerLeave={()=>setPaused(false)} onFocus={()=>setPaused(true)} onBlur={e=>{if(!e.currentTarget.contains(e.relatedTarget))setPaused(false);}}>
  <button className="reaction-ticker-open" onClick={onOpen}>
   <span className="reaction-emotion" aria-hidden="true"><span className="reaction-portrait"><Avatar name={notice.actorName} size="sm"/>{!!notice.relation&&<small>{notice.relation!>0?'+':''}{notice.relation}</small>}</span></span>
   <span className="reaction-ticker-copy"><strong>{notice.actorName}{notice.kind==='mention'?' упоминает тебя':notice.kind==='follow'?' подписывается на канал':' отвечает на твою запись'}</strong><span className="reaction-ticker-preview">{notice.text}</span></span>
  </button>
  {(!inline||!read)&&<button className="reaction-ticker-close" aria-label={inline?'Отметить прочитанным':'Закрыть реакцию'} onClick={onClose}><Icon name={inline?'check':'close'}/></button>}
 </aside>;
}
