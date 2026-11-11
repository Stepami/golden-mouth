import {useEffect,type RefObject} from 'react';

/** Fade only the edge that still has content beyond it. Scrolling stays native. */
export function useScrollEdges(root:RefObject<HTMLElement|null>) {
 useEffect(()=>{
  const scope=root.current;
  if(!scope)return;
  const areas=new Set<HTMLElement>();
  let frame=0;
  const measure=(el:HTMLElement)=>{
   const horizontal=el.scrollWidth>el.clientWidth+1;
   const start=horizontal?el.scrollLeft:el.scrollTop;
   const remaining=horizontal?el.scrollWidth-el.clientWidth-start:el.scrollHeight-el.clientHeight-start;
   el.dataset.scrollAxis=horizontal?'x':'y';
   el.style.setProperty('--fade-start',start>2?'12px':'0px');
   el.style.setProperty('--fade-end',remaining>2?'16px':'0px');
  };
  const schedule=()=>{cancelAnimationFrame(frame);frame=requestAnimationFrame(()=>areas.forEach(measure));};
  const resize=new ResizeObserver(schedule);
  const scan=()=>{
   for(const el of areas)if(!scope.contains(el)){el.removeEventListener('scroll',schedule);resize.unobserve(el);areas.delete(el);}
   scope.querySelectorAll<HTMLElement>('.app-body, .filter-strip, .saved-worlds-scroll, .dialog-inner, .skill-options, .age-entry-options, textarea').forEach(el=>{
    if(!areas.has(el)){areas.add(el);el.addEventListener('scroll',schedule,{passive:true});resize.observe(el);}
   });
   schedule();
  };
  const mutation=new MutationObserver(scan);
  mutation.observe(scope,{childList:true,subtree:true,characterData:true});
  scope.addEventListener('load',schedule,true);
  scan();
  void document.fonts.ready.then(schedule);
  return()=>{cancelAnimationFrame(frame);mutation.disconnect();resize.disconnect();scope.removeEventListener('load',schedule,true);areas.forEach(el=>el.removeEventListener('scroll',schedule));};
 },[root]);
}
