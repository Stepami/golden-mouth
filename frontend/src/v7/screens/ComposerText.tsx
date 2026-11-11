import {useEffect,useState,type ChangeEvent} from 'react';

/** Animate presentation only: the complete draft stays in the textarea and storage. */
export function ComposerText({value,onChange,animate,disabled,maxLength,autoFocus}:{
 value:string;onChange:(event:ChangeEvent<HTMLTextAreaElement>)=>void;animate:boolean;disabled:boolean;maxLength:number;autoFocus:boolean;
}){
 const [interrupted,setInterrupted]=useState(false);
 const reduced=matchMedia('(prefers-reduced-motion: reduce)').matches;
 const [visible,setVisible]=useState(animate&&!reduced?'':value);
 const [typing,setTyping]=useState(animate&&!reduced);
 const finish=()=>{setInterrupted(true);setVisible(value);setTyping(false);};
 useEffect(()=>{
  if(!animate||interrupted||reduced){setVisible(value);setTyping(false);return;}
  const letters=Array.from(value),duration=Math.min(2400,Math.max(800,letters.length*18));
  let frame=0,start:number|undefined;
  setVisible('');setTyping(true);
  const tick=(time:number)=>{
   start??=time;
   const count=Math.min(letters.length,Math.floor(Math.max(0,time-start-120)/duration*letters.length));
   setVisible(letters.slice(0,count).join(''));
   if(count===letters.length)setTyping(false);else frame=requestAnimationFrame(tick);
  };
  frame=requestAnimationFrame(tick);
  return()=>cancelAnimationFrame(frame);
 },[value,animate,interrupted,reduced]);
 return <div className="composer-editor">
  <textarea id="post-text" className={`composer-text${typing?' is-autotyping':''}`} value={value} onFocus={finish} onChange={event=>{finish();onChange(event);}} disabled={disabled} maxLength={maxLength} autoFocus={autoFocus} aria-busy={typing||undefined}/>
  {typing&&<div className="composer-typing" aria-hidden="true">{visible}<span className="composer-typing-caret"/></div>}
 </div>;
}
