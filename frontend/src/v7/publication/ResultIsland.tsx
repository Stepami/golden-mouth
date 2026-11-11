import {useEffect,useRef,useState} from 'react';
import {Icon} from '../ui';
import {RewardArt} from '../ui/RewardArt';
import {SkillChanges,type PublicationReceipt} from './PublicationCards';
import './resultIsland.css';

export function ResultIsland({receipt,onRead,onClose}:{receipt:PublicationReceipt;onRead:()=>void;onClose:()=>void}) {
 const [paused,setPaused]=useState(false),[leaving,setLeaving]=useState(false);
 const onCloseRef=useRef(onClose);onCloseRef.current=onClose;
 useEffect(()=>{if(paused||leaving)return;const timer=setTimeout(()=>setLeaving(true),12000);return()=>clearTimeout(timer);},[paused,leaving]);
 useEffect(()=>{if(!leaving)return;const timer=setTimeout(()=>onCloseRef.current(),180);return()=>clearTimeout(timer);},[leaving]);
 const outcome=receipt.questOutcome?.status;
 const title=outcome==='success'?'Квест выполнен':outcome==='partial'?'Есть прогресс':outcome==='failed'?'Пока не получилось':receipt.actionKind==='comment'?'Комментарий сохранён':'Пост опубликован';
 return <aside className={`result-island${leaving?' is-leaving':''}`} aria-label="Результат отправки" onPointerEnter={()=>setPaused(true)} onPointerLeave={()=>setPaused(false)} onFocus={()=>setPaused(true)} onBlur={e=>{if(!e.currentTarget.contains(e.relatedTarget))setPaused(false);}}>
  <span className="sr-only" role="status">{title}{receipt.audienceKnown!==false?`. ${receipt.result.delta>=0?'+':''}${receipt.result.delta} подписчиков`:''}</span>
  <button className="result-island-open" onClick={()=>{onRead();onClose();}}><RewardArt kind="followers"/><span><strong>{title}</strong><small>{receipt.audienceKnown===false?'Читать отклики':`${receipt.result.delta>=0?'+':''}${receipt.result.delta} подписчиков · Читать отклики`}</small></span></button>
  <SkillChanges receipt={receipt}/>
  <button className="result-island-close" aria-label="Скрыть результат" onClick={()=>setLeaving(true)}><Icon name="close"/></button>
 </aside>;
}
