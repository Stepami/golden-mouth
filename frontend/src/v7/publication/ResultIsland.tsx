import {hapticFeedback} from '@tma.js/sdk';
import {inTelegram} from '../../telegram';
import {useEffect,useId,useLayoutEffect,useRef,useState} from 'react';
import {Icon} from '../ui';
import {RewardArt} from '../ui/RewardArt';
import {SkillChanges,SkillGains,type PublicationReceipt} from './PublicationCards';
import {audiencePercent,pickResultPhrase,growthPhrases,declinePhrases} from './resultCopy';
import './resultIsland.css';

export function ResultIsland({receipt,onRead,onClose}:{receipt:PublicationReceipt;onRead:()=>void;onClose:()=>void}) {
 const [paused,setPaused]=useState(false),[leaving,setLeaving]=useState(false),[expanded,setExpanded]=useState(false);
 const remaining=useRef(5000);
 const [seconds,setSeconds]=useState(5);
 const [fallbackPhrase]=useState(()=>pickResultPhrase(receipt.result.delta));
 const detailsId=useId();
 const panel=useRef<HTMLElement>(null);
 const [position,setPosition]=useState({top:8,maxHeight:'calc(100dvh - 32px)'});
 useLayoutEffect(()=>{
  const app=panel.current?.closest('.app'),hud=app?.querySelector('.world-hud'),body=app?.querySelector('.app-body');
  const measure=()=>{
   const a=app?.getBoundingClientRect(),h=hud?.getBoundingClientRect();
   const top=Math.max(8,h&&a?h.bottom-a.top+16:8);
   setPosition({top,maxHeight:`calc(100dvh - ${top+16}px)`});
  };
  measure();
  const observer=new ResizeObserver(measure);if(app)observer.observe(app,{box:'border-box'});if(hud)observer.observe(hud,{box:'border-box'});
  body?.addEventListener('scroll',measure,{passive:true});window.addEventListener('resize',measure);
  return()=>{observer.disconnect();body?.removeEventListener('scroll',measure);window.removeEventListener('resize',measure);};
 },[]);
 const onCloseRef=useRef(onClose);onCloseRef.current=onClose;
 useEffect(()=>{
  if(paused||expanded||leaving)return;
  const started=performance.now(),initial=remaining.current;
  const tick=()=>{remaining.current=Math.max(0,initial-(performance.now()-started));setSeconds(Math.ceil(remaining.current/1000));if(remaining.current===0)setLeaving(true);};
  const timer=setInterval(tick,100);
  return()=>{remaining.current=Math.max(0,initial-(performance.now()-started));clearInterval(timer);};
 },[paused,expanded,leaving]);
 useEffect(()=>{if(!leaving)return;const timer=setTimeout(()=>onCloseRef.current(),180);return()=>clearTimeout(timer);},[leaving]);
 const known=receipt.audienceKnown!==false,delta=receipt.result.delta;
 const mood=known?(delta>0?'gain':delta<0?'loss':'neutral'):'neutral';
 const reduced=matchMedia('(prefers-reduced-motion: reduce)').matches||new URLSearchParams(location.search).has('shot');
 const [shown,setShown]=useState(reduced?Math.abs(delta):0);
 const hapticPlayed=useRef(false);
 useEffect(()=>{
  if(!known)return;
  if(reduced||delta===0){setShown(Math.abs(delta));return;}
  const start=performance.now()+200;let frame=0;
  const tick=(time:number)=>{const k=Math.min(1,Math.max(0,time-start)/2000);setShown(Math.round(Math.abs(delta)*(1-(1-k)**2)));if(k<1)frame=requestAnimationFrame(tick);};
  frame=requestAnimationFrame(tick);return()=>cancelAnimationFrame(frame);
 },[known,delta,reduced]);
 useEffect(()=>{if(!known||hapticPlayed.current)return;hapticPlayed.current=true;try{if(inTelegram)hapticFeedback.notificationOccurred.ifAvailable(delta>=0?'success':'warning');else if(typeof navigator.vibrate==='function')navigator.vibrate(delta>=0?[15,40,20]:[35,40,15]);}catch{/* Feedback must not interrupt the saved result. */}},[known,delta]);
 const percent=known?audiencePercent(delta,receipt.beforeSubscribers):undefined;
 const shownPercent=known?audiencePercent(Math.sign(delta)*shown,receipt.beforeSubscribers):undefined;
 const phrases=delta>0?growthPhrases:declinePhrases;
 const phrase=receipt.reactionText&&phrases.includes(receipt.reactionText)?receipt.reactionText:fallbackPhrase;
 const title=receipt.actionKind==='comment'?'Комментарий сохранён':'Пост опубликован';
 const change=`${delta>0?'+':delta<0?'−':''}${new Intl.NumberFormat('ru').format(Math.abs(delta))}`;
 const shownChange=`${shown===0?'':delta>0?'+':'−'}${new Intl.NumberFormat('ru').format(shown)}`;
 return <aside ref={panel} style={position} className={`result-island result-island--${mood}${leaving?' is-leaving':''}`} aria-label="Результат отправки" onPointerEnter={()=>setPaused(true)} onPointerLeave={()=>setPaused(false)} onFocus={()=>setPaused(true)} onBlur={e=>{if(!e.currentTarget.contains(e.relatedTarget))setPaused(false);}}>
  <span className="sr-only" role="status">{title}{known?`. ${change} подписчиков${percent?`, ${percent}`:''}`:'. Данные аудитории уточняются'}</span>
  <button className="result-island-open" aria-expanded={expanded} aria-controls={detailsId} onClick={()=>setExpanded(value=>!value)}>
   <span className="result-island-title">{title}</span>
   <span className="result-island-audience"><RewardArt kind="followers"/><span>Подписчики</span></span>
   <span className="result-island-numbers"><strong className="result-island-delta">{known?shownChange:'Уточняем'}</strong>{percent&&<span className="result-island-percent">{shownPercent}</span>}</span>
   <span className="result-island-phrase">{known?phrase:'Публикация сохранена. Данные аудитории уточняются.'}</span>
   {(receipt.questReward?.source==='server'||(import.meta.env.DEV&&receipt.questReward?.source==='preview'))&&receipt.questReward.amount>0&&<span className="result-crystal-reward"><span>{receipt.questReward.source==='preview'?'Превью: за ход':'Задание выполнено'}</span><span><RewardArt kind="crystal"/>+{receipt.questReward.amount}</span></span>}
   {!expanded&&<SkillGains receipt={receipt}/>}
   <span className="result-island-more">{expanded?'Скрыть изменения навыков':'Показать изменения навыков'}<Icon name="chevron"/></span>
  </button>
  {expanded&&<div id={detailsId} className="result-island-details"><h2>Навыки за ход</h2><SkillChanges receipt={receipt} detailed/><button className="result-island-read" onClick={()=>{onRead();onClose();}}>Читать отклики<Icon name="arrow"/></button></div>}
  <span className="result-island-dismiss"><span className="result-island-countdown" aria-live="off" title={paused||expanded?'Таймер на паузе':`Автоскрытие через ${seconds} с`}>{paused||expanded?'Пауза':`${seconds} с`}</span><button className="result-island-close" aria-label="Скрыть результат" onClick={()=>setLeaving(true)}><Icon name="close"/></button></span>
 </aside>;
}
