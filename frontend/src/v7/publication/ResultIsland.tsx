import {useEffect,useId,useRef,useState} from 'react';
import {Icon} from '../ui';
import {RewardArt} from '../ui/RewardArt';
import {SkillChanges,type PublicationReceipt} from './PublicationCards';
import {audiencePercent,pickResultPhrase} from './resultCopy';
import './resultIsland.css';

export function ResultIsland({receipt,onRead,onClose}:{receipt:PublicationReceipt;onRead:()=>void;onClose:()=>void}) {
 const [paused,setPaused]=useState(false),[leaving,setLeaving]=useState(false),[expanded,setExpanded]=useState(false);
 const [fallbackPhrase]=useState(()=>pickResultPhrase(receipt.result.delta));
 const detailsId=useId();
 const onCloseRef=useRef(onClose);onCloseRef.current=onClose;
 useEffect(()=>{if(paused||expanded||leaving)return;const timer=setTimeout(()=>setLeaving(true),12000);return()=>clearTimeout(timer);},[paused,expanded,leaving]);
 useEffect(()=>{if(!leaving)return;const timer=setTimeout(()=>onCloseRef.current(),180);return()=>clearTimeout(timer);},[leaving]);
 const known=receipt.audienceKnown!==false,delta=receipt.result.delta;
 const mood=known?(delta>0?'gain':delta<0?'loss':'neutral'):'neutral';
 const percent=known?audiencePercent(delta,receipt.beforeSubscribers):undefined;
 const title=receipt.actionKind==='comment'?'Комментарий сохранён':'Пост опубликован';
 const change=`${delta>0?'+':delta<0?'−':''}${new Intl.NumberFormat('ru').format(Math.abs(delta))}`;
 return <aside className={`result-island result-island--${mood}${leaving?' is-leaving':''}`} aria-label="Результат отправки" onPointerEnter={()=>setPaused(true)} onPointerLeave={()=>setPaused(false)} onFocus={()=>setPaused(true)} onBlur={e=>{if(!e.currentTarget.contains(e.relatedTarget))setPaused(false);}}>
  <span className="sr-only" role="status">{title}{known?`. ${change} подписчиков${percent?`, ${percent}`:''}`:'. Данные аудитории уточняются'}</span>
  <button className="result-island-open" aria-expanded={expanded} aria-controls={detailsId} onClick={()=>setExpanded(value=>!value)}>
   <span className="result-island-title">{title}</span>
   <span className="result-island-audience"><RewardArt kind="followers"/><span>Подписчики</span></span>
   <span className="result-island-numbers"><strong className="result-island-delta">{known?change:'Уточняем'}</strong>{percent&&<span className="result-island-percent">{percent}</span>}</span>
   <span className="result-island-phrase">{known?(receipt.reactionText??fallbackPhrase):'Публикация сохранена. Данные аудитории уточняются.'}</span>
   <span className="result-island-more">{expanded?'Скрыть изменения навыков':'Показать изменения навыков'}<Icon name="chevron"/></span>
  </button>
  {expanded&&<div id={detailsId} className="result-island-details"><h2>Навыки за ход</h2><SkillChanges receipt={receipt} detailed/><button className="result-island-read" onClick={()=>{onRead();onClose();}}>Читать отклики<Icon name="arrow"/></button></div>}
  <button className="result-island-close" aria-label="Скрыть результат" onClick={()=>setLeaving(true)}><Icon name="close"/></button>
 </aside>;
}
