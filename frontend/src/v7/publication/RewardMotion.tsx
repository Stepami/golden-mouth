import {useEffect,useState,useRef} from 'react';
export function AnimatedGain({value,animate=false}:{value:number;animate?:boolean}) {
 const [shown,setShown]=useState(animate?0:value);
 useEffect(()=>{if(!animate||matchMedia('(prefers-reduced-motion: reduce)').matches){setShown(value);return;}let frame=0;const begin=performance.now();const tick=(time:number)=>{const t=Math.min(1,(time-begin)/700);setShown(Math.round(value*(1-Math.pow(1-t,3))*10)/10);if(t<1)frame=requestAnimationFrame(tick);};frame=requestAnimationFrame(tick);return()=>cancelAnimationFrame(frame);},[value,animate]);
 return <><span aria-hidden="true">{shown>0?'+':''}{new Intl.NumberFormat('ru',{maximumFractionDigits:1}).format(shown)}</span><span className="sr-only">{value>0?'+':''}{value}</span></>;
}
export function FollowersFlight({delta,nonce}:{delta:number;nonce:string}){
 const [point,setPoint]=useState<{x:number;y:number;dx:number;dy:number}>();
 useEffect(()=>{if(!delta||matchMedia('(prefers-reduced-motion: reduce)').matches)return;const a=document.querySelector('.result-followers-gain')?.getBoundingClientRect(),b=document.querySelector('.channel-count-target')?.getBoundingClientRect();if(a&&b)setPoint({x:a.x+a.width/2,y:a.y+16,dx:b.x+b.width/2-a.x-a.width/2,dy:b.y+b.height/2-a.y-16});const timer=setTimeout(()=>setPoint(undefined),1900);return()=>clearTimeout(timer);},[nonce]);
 return point?<span className="followers-flight" style={{left:point.x,top:point.y,'--fly-x':`${point.dx}px`,'--fly-y':`${point.dy}px`} as React.CSSProperties}>{delta>0?'+':''}{delta} подписчиков</span>:null;
}
