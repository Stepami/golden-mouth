import {hapticFeedback} from '@tma.js/sdk';
import {inTelegram} from '../../telegram';
import {useEffect,useId,useLayoutEffect,useRef,useState} from 'react';
import {Avatar,Button,Icon} from '../ui';
import {RewardArt} from '../ui/RewardArt';
import {SkillChanges,SkillGains,type PublicationReceipt} from './PublicationCards';
import {audiencePercent,pickResultPhrase,growthPhrases,declinePhrases} from './resultCopy';
import './resultIsland.css';

function CommentResultIsland({receipt,onRead,onClose,archive=false}:{receipt:PublicationReceipt;onRead:()=>void;onClose?:()=>void;archive?:boolean}) {
 const [paused,setPaused]=useState(false),[leaving,setLeaving]=useState(false),[expanded,setExpanded]=useState(false);
 const remaining=useRef(5000);
 const [seconds,setSeconds]=useState(5);
 const [fallbackPhrase]=useState(()=>archive?'':pickResultPhrase(receipt.result.delta));
 const detailsId=useId();
 const panel=useRef<HTMLElement>(null);
 const [position,setPosition]=useState({top:8,maxHeight:'calc(100dvh - 32px)'});
 useLayoutEffect(()=>{
  if(archive)return;
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
 },[archive]);
 const onCloseRef=useRef(onClose);onCloseRef.current=onClose;
 useEffect(()=>{
  if(archive||paused||expanded||leaving)return;
  const started=performance.now(),initial=remaining.current;
  const tick=()=>{remaining.current=Math.max(0,initial-(performance.now()-started));setSeconds(Math.ceil(remaining.current/1000));if(remaining.current===0)setLeaving(true);};
  const timer=setInterval(tick,100);
  return()=>{remaining.current=Math.max(0,initial-(performance.now()-started));clearInterval(timer);};
 },[archive,paused,expanded,leaving]);
 useEffect(()=>{if(archive||!leaving)return;const timer=setTimeout(()=>onCloseRef.current?.(),180);return()=>clearTimeout(timer);},[archive,leaving]);
 const known=receipt.audienceKnown!==false,delta=receipt.result.delta;
 const mood=known?(delta>0?'gain':delta<0?'loss':'neutral'):'neutral';
 const reduced=matchMedia('(prefers-reduced-motion: reduce)').matches||new URLSearchParams(location.search).has('shot');
 const [shown,setShown]=useState(archive||reduced?Math.abs(delta):0);
 const hapticPlayed=useRef(false);
 useEffect(()=>{
  if(!known)return;
  if(archive||reduced||delta===0){setShown(Math.abs(delta));return;}
  const start=performance.now()+200;let frame=0;
  const tick=(time:number)=>{const k=Math.min(1,Math.max(0,time-start)/2000);setShown(Math.round(Math.abs(delta)*(1-(1-k)**2)));if(k<1)frame=requestAnimationFrame(tick);};
  frame=requestAnimationFrame(tick);return()=>cancelAnimationFrame(frame);
 },[archive,known,delta,reduced]);
 useEffect(()=>{if(archive||!known||hapticPlayed.current)return;hapticPlayed.current=true;try{if(inTelegram)hapticFeedback.notificationOccurred.ifAvailable(delta>=0?'success':'warning');else if(typeof navigator.vibrate==='function')navigator.vibrate(delta>=0?[15,40,20]:[35,40,15]);}catch{/* Feedback must not interrupt the saved result. */}},[archive,known,delta]);
 const percent=known?audiencePercent(delta,receipt.beforeSubscribers):undefined;
 const shownPercent=known?audiencePercent(Math.sign(delta)*shown,receipt.beforeSubscribers):undefined;
 const phrases=delta>0?growthPhrases:declinePhrases;
 const phrase=archive?(receipt.reactionText??'Фраза этого результата не сохранена'):receipt.reactionText&&phrases.includes(receipt.reactionText)?receipt.reactionText:fallbackPhrase;
 const title=receipt.actionKind==='comment'?'Комментарий сохранён':'Пост опубликован';
 const change=`${delta>0?'+':delta<0?'−':''}${new Intl.NumberFormat('ru').format(Math.abs(delta))}`;
 const shownChange=`${shown===0?'':delta>0?'+':'−'}${new Intl.NumberFormat('ru').format(shown)}`;
 return <aside ref={panel} style={archive?undefined:position} className={`result-island result-island--${mood}${archive?' result-island--archive':''}${leaving?' is-leaving':''}`} aria-label={archive?"Сохранённый результат":"Результат отправки"} onPointerEnter={archive?undefined:()=>setPaused(true)} onPointerLeave={archive?undefined:()=>setPaused(false)} onFocus={archive?undefined:()=>setPaused(true)} onBlur={archive?undefined:e=>{if(!e.currentTarget.contains(e.relatedTarget))setPaused(false);}}>
  <span className="sr-only" role={archive?undefined:'status'}>{title}{known?`. ${change} подписчиков${percent?`, ${percent}`:''}`:'. Данные аудитории уточняются'}</span>
  <button className="result-island-open" aria-expanded={expanded} aria-controls={detailsId} onClick={()=>setExpanded(value=>!value)}>
   <span className="result-island-title">{title}</span>
   <span className="result-island-audience"><RewardArt kind="followers"/><span>Подписчики</span></span>
   <span className="result-island-numbers"><strong className="result-island-delta">{known?shownChange:'Уточняем'}</strong>{percent&&<span className="result-island-percent">{shownPercent}</span>}</span>
   <span className="result-island-phrase">{known?phrase:'Публикация сохранена. Данные аудитории уточняются.'}</span>
   {(receipt.questReward?.source==='server'||(import.meta.env.DEV&&receipt.questReward?.source==='preview'))&&receipt.questReward.amount>0&&<span className="result-crystal-reward"><span>{receipt.questReward.source==='preview'?'Превью: за ход':'Задание выполнено'}</span><span><RewardArt kind="crystal"/>+{receipt.questReward.amount}</span></span>}
   {!expanded&&<SkillGains receipt={receipt}/>}
   <span className="result-island-more">{expanded?'Скрыть изменения навыков':'Показать изменения навыков'}<Icon name="chevron"/></span>
  </button>
  {expanded&&<div id={detailsId} className="result-island-details"><h2>Навыки за ход</h2><SkillChanges receipt={receipt} detailed/><button className="result-island-read" onClick={()=>{onRead();if(!archive)onClose?.();}}>Читать отклики<Icon name="arrow"/></button></div>}
  {!archive&&<span className="result-island-dismiss"><span className="result-island-countdown" aria-live="off" title={paused||expanded?'Таймер на паузе':`Автоскрытие через ${seconds} с`}>{paused||expanded?'Пауза':`${seconds} с`}</span><button className="result-island-close" aria-label="Скрыть результат" onClick={()=>setLeaving(true)}><Icon name="close"/></button></span>}
 </aside>;
}

// Posts keep the saved NPC response visible; comment receipts retain their existing presentation.
export function ResultIsland(props: ResultIslandProps) {
 return props.receipt.actionKind==='comment'?<CommentResultIsland {...props}/>:<PostResultIsland {...props}/>;
}

type ResultIslandProps={receipt:PublicationReceipt;onRead:()=>void;onClose?:()=>void;onNewPost?:()=>void;onShare?:()=>void;archive?:boolean};
function PostResultIsland({receipt,onRead,onClose,onNewPost,onShare,archive=false}:ResultIslandProps){
 const panel=useRef<HTMLElement>(null),close=useRef<HTMLButtonElement>(null),closeHandler=useRef(onClose);closeHandler.current=onClose;
 const [position,setPosition]=useState({top:8,maxHeight:'calc(100dvh - 32px)'});
 useLayoutEffect(()=>{
  if(archive)return;
  const app=panel.current?.closest('.app'),hud=app?.querySelector('.world-hud');
  const measure=()=>{const a=app?.getBoundingClientRect(),h=hud?.getBoundingClientRect();const top=Math.max(8,h&&a?h.bottom-a.top+16:8);setPosition({top,maxHeight:`calc(100dvh - ${top+16}px)`});};
  measure();const observer=new ResizeObserver(measure);if(app)observer.observe(app);if(hud)observer.observe(hud);window.addEventListener('resize',measure);
  return()=>{observer.disconnect();window.removeEventListener('resize',measure);};
 },[archive]);
 useEffect(()=>{if(archive)return;const previous=document.activeElement as HTMLElement|null;close.current?.focus();const keyboard=(event:KeyboardEvent)=>{if(event.key==='Escape'){event.preventDefault();closeHandler.current?.();}if(event.key==='Tab'){const items=panel.current?.querySelectorAll<HTMLElement>('button:not(:disabled),summary,a[href]');if(!items?.length)return;const first=items[0],last=items[items.length-1];if(event.shiftKey&&document.activeElement===first){event.preventDefault();last.focus();}else if(!event.shiftKey&&document.activeElement===last){event.preventDefault();first.focus();}}};document.addEventListener('keydown',keyboard);return()=>{document.removeEventListener('keydown',keyboard);previous?.focus({preventScroll:true});};},[archive]);
 const result=receipt.result,delta=result.delta,known=receipt.audienceKnown!==false;
 const comment=receipt.comment;
 const name=comment?.author??receipt.commentAuthor?.name??'Персонаж';
 const text=comment?.text??result.bestComment?.text;
 const explanation=delta===0?'Число подписчиков не изменилось.':delta<0?result.whys.find(why=>!why.value.startsWith('×') && why.label !== 'В тему')?.label??result.headline:result.headline;
 return <aside ref={panel} style={archive?undefined:position} className={`result-island post-result-island${result.viral?' post-result-island--viral':''}${archive?' result-island--archive':''}`} role={archive?undefined:'dialog'} aria-modal={archive?undefined:true} aria-label="Пост опубликован">
  <header className="post-result-island-heading"><span>Пост опубликован</span>{!archive&&<button ref={close} className="result-island-close" aria-label="Закрыть итог и вернуться в канал" onClick={onClose}><Icon name="close"/></button>}</header>
  <section className="post-result-island-reaction" aria-label="Ответ персонажа"><div className="row"><Avatar name={name} src={receipt.commentAuthor?.avatar}/><div><strong>{name}</strong><p className="caption muted">{receipt.worldTitle?`${receipt.worldTitle} · `:''}AI-персонаж</p></div></div><p role={text?undefined:'status'}>{text??(receipt.commentState==='pending'?'Персонаж готовит ответ…':'Ответ персонажа пока не загрузился.')}</p></section>
  <strong className={`post-result-island-delta ${delta<0?'down':delta>0?'up':''}`}>{known?`${delta>0?'+':delta<0?'−':''}${Math.abs(delta).toLocaleString('ru')} подписчиков`:'Аудитория уточняется'}</strong>
  {known&&<p className="caption muted">{explanation}</p>}
  <details className="post-result-island-analysis"><summary>Разбор поста</summary><dl>{result.whys.map((why,index)=><div key={`${why.label}:${index}`}><dt>{why.label}</dt><dd>{why.value}</dd></div>)}</dl><SkillChanges receipt={receipt} detailed/>{result.ribbons.map((ribbon,index)=><p key={index}>{ribbon.icon} {ribbon.title}</p>)}</details>
  <div className="post-result-island-actions">{onNewPost&&<Button onClick={onNewPost}>Новый пост</Button>}<Button variant="secondary" onClick={onRead}>Комментировать</Button>{onShare&&<Button variant="ghost" onClick={onShare}>Поделиться</Button>}</div>
 </aside>;
}
