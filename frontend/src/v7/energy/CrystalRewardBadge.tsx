import {createPortal} from 'react-dom';
import {useLayoutEffect,useRef,useState,type CSSProperties} from 'react';
import {ResourceBadge} from '../ui';
import {RewardArt} from '../ui/RewardArt';

export type CrystalAward={id:string;amount:number;before:number;after:number};
/** Visual feedback only. The wallet and reward snapshots are supplied by the caller. */
export function CrystalRewardBadge({balance,award,seen,onClick,preview=false}:{balance?:number;preview?:boolean;award?:CrystalAward;seen:Set<string>;onClick:()=>void}){
 const root=useRef<HTMLSpanElement>(null);
 const [shown,setShown]=useState(balance);
 const [flight,setFlight]=useState<{award:CrystalAward;x:number;y:number;dx:number;dy:number}>();
 useLayoutEffect(()=>{
  setFlight(undefined);setShown(balance);
  if(!award||balance!==award.after||award.after-award.before!==award.amount||!Number.isInteger(award.amount)||award.amount<=0||seen.has(award.id))return;
  seen.add(award.id);
  if(matchMedia('(prefers-reduced-motion: reduce)').matches)return;
  setShown(award.before);
  const frame=requestAnimationFrame(()=>{
   const app=root.current?.closest('.app');
   const source=app?.querySelector('.result-crystal-reward')??app?.querySelector('.result-island-numbers');
   const target=root.current?.querySelector('.resource>.icon');
   if(!source||!target){setShown(balance);return;}
   const a=source.getBoundingClientRect(),b=target.getBoundingClientRect();
   const x=a.x+a.width/2,y=a.y+a.height/2;
   setFlight({award,x,y,dx:b.x+b.width/2-x,dy:b.y+b.height/2-y});
  });
  return()=>cancelAnimationFrame(frame);
 },[balance,award?.id,award?.amount,award?.before,award?.after,seen]);
 const arrive=(index:number)=>{
  if(!flight)return;
  setShown(value=>Math.min(flight.award.after,Math.max(value??flight.award.before,flight.award.before+index+1)));
  root.current?.querySelector('.resource>.icon')?.animate([{transform:'scale(1)'},{transform:'scale(1.35)',offset:.4},{transform:'scale(1)'}],{duration:360,easing:'ease-out'});
  if(index===flight.award.amount-1)setFlight(undefined);
 };
 const app=root.current?.closest('.app');
 const particles=flight&&Array.from({length:flight.award.amount},(_,index)=><span key={`${flight.award.id}:${index}`} className="crystal-award-flight" aria-hidden="true" onAnimationEnd={()=>arrive(index)} style={{left:flight.x,top:flight.y,animationDelay:`${index*380}ms`,'--crystal-x':`${flight.dx}px`,'--crystal-y':`${flight.dy}px`} as CSSProperties}><RewardArt kind="crystal"/></span>);
 return <span className="crystal-hud" ref={root}>
  <span className="crystal-badge-wrap"><ResourceBadge type="crystal" value={shown===undefined?'—':String(shown)} hint={preview?'Превью: +2 за ход. Серверный кошелёк и обмен не изменяются.':balance===undefined?'Баланс кристаллов пока недоступен':'Кристаллы для обмена на энергию'} onClick={onClick}/>{preview&&<small className="crystal-preview-label">Превью</small>}</span>
  {app&&particles?createPortal(particles,app):null}
 </span>;
}
