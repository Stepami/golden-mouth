import {useEffect,useRef,type ReactNode} from 'react';

/** Native touch scrolling, with mouse drag and wheel support for the desktop demo. */
export function WorldCarousel({children}:{children:ReactNode}) {
 const root=useRef<HTMLDivElement>(null);
 const drag=useRef<{x:number;left:number;active:boolean}|null>(null);
 const suppressClick=useRef(false);
 useEffect(()=>{
  const el=root.current;if(!el)return;
  const wheel=(event:WheelEvent)=>{
   if(event.ctrlKey||event.deltaX||!event.deltaY)return;
   const delta=event.deltaY*(event.deltaMode===1?16:event.deltaMode===2?el.clientWidth:1);
   const max=el.scrollWidth-el.clientWidth;
   if((delta>0&&el.scrollLeft>=max-1)||(delta<0&&el.scrollLeft<=0))return;
   event.preventDefault();el.scrollLeft+=delta;
  };
  el.addEventListener('wheel',wheel,{passive:false});
  return()=>el.removeEventListener('wheel',wheel);
 },[]);
 const step=(el:HTMLDivElement)=>(el.firstElementChild as HTMLElement)?.offsetWidth+8;
 return <div ref={root} className="saved-worlds-scroll" tabIndex={0} role="region" aria-label="Мои миры — листай влево и вправо"
  onDragStart={e=>e.preventDefault()}
  onPointerDown={e=>{suppressClick.current=false;if(e.pointerType==='mouse'&&e.button===0)drag.current={x:e.clientX,left:e.currentTarget.scrollLeft,active:false};}}
  onPointerMove={e=>{
   const state=drag.current;if(!state)return;
   const dx=e.clientX-state.x;
   if(!state.active&&Math.abs(dx)>6){state.active=true;suppressClick.current=true;e.currentTarget.setPointerCapture(e.pointerId);e.currentTarget.classList.add('is-dragging');}
   if(state.active){e.preventDefault();e.currentTarget.scrollLeft=state.left-dx;}
  }}
  onPointerUp={e=>{
   const state=drag.current;drag.current=null;e.currentTarget.classList.remove('is-dragging');
   if(state?.active){const el=e.currentTarget;el.releasePointerCapture(e.pointerId);el.scrollTo({left:Math.round(el.scrollLeft/step(el))*step(el),behavior:matchMedia('(prefers-reduced-motion: reduce)').matches?'instant':'smooth'});}
  }}
  onPointerCancel={e=>{drag.current=null;e.currentTarget.classList.remove('is-dragging');}}
  onClickCapture={e=>{if(suppressClick.current){e.preventDefault();e.stopPropagation();suppressClick.current=false;}}}
  onKeyDown={e=>{if(!['ArrowLeft','ArrowRight','Home','End'].includes(e.key))return;e.preventDefault();const el=e.currentTarget;el.scrollTo({left:e.key==='Home'?0:e.key==='End'?el.scrollWidth:el.scrollLeft+(e.key==='ArrowRight'?1:-1)*step(el),behavior:'instant'});}}
 >{children}</div>;
}
