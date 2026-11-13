const MIN_TYPING_MS=2400;

/** Hold only the visual delivery of a fast reply; the saved response stays on the server. */
export async function waitForTyping(started:number,signal?:AbortSignal){
 if(signal?.aborted)throw new DOMException('Ожидание прервано','AbortError');
 const remaining=MIN_TYPING_MS-(performance.now()-started);
 if(remaining<=0)return;
 await new Promise<void>((resolve,reject)=>{
  const finish=()=>{signal?.removeEventListener('abort',stop);resolve();};
  const timer=setTimeout(finish,remaining);
  const stop=()=>{clearTimeout(timer);signal?.removeEventListener('abort',stop);reject(new DOMException('Ожидание прервано','AbortError'));};
  signal?.addEventListener('abort',stop,{once:true});
 });
}
