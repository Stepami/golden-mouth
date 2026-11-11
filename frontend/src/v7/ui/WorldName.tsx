import {useLayoutEffect,useRef,useState,type CSSProperties} from 'react';

export function WorldName({name}:{name:string}){
 const viewport=useRef<HTMLSpanElement>(null),text=useRef<HTMLSpanElement>(null);
 const [cycle,setCycle]=useState(0);
 useLayoutEffect(()=>{
  const measure=()=>{if(viewport.current&&text.current){const width=text.current.getBoundingClientRect().width;setCycle(width>viewport.current.clientWidth?width+16:0);}};
  const observer=new ResizeObserver(measure);
  if(viewport.current)observer.observe(viewport.current);
  if(text.current)observer.observe(text.current);
  measure();return()=>observer.disconnect();
 },[name]);
 return <span ref={viewport} className={`world-switch-name${cycle>0?' is-overflowing':''}`} title={name}>
  <span className="world-name-track" style={{'--name-cycle':`${cycle}px`,'--name-duration':`${cycle/24}s`} as CSSProperties}>
   <span ref={text} className="world-name-text">{name}</span>
   {cycle>0&&<span className="world-name-text world-name-copy" aria-hidden="true">{name}</span>}
  </span>
 </span>;
}
