import {useCallback,useEffect,useMemo,useRef,useState} from 'react';
import {EnergyExchange,exchangeMessage,walletRefreshDelay,type EnergyWallet,type WalletClient} from './exchange';
export function useEnergyWallet(owner:string|undefined,client:WalletClient,onSnapshot:(wallet:EnergyWallet)=>void){
 const [wallet,setWallet]=useState<EnergyWallet>(),[busy,setBusy]=useState(false),[message,setMessage]=useState(''),[refreshError,setRefreshError]=useState('');
 const revision=useRef(0),exchanging=useRef(false);
 const generation=useRef(0),flight=useRef<Promise<void>|undefined>(undefined),notify=useRef(onSnapshot);notify.current=onSnapshot;
 const exchange=useMemo(()=>new EnergyExchange(client),[client,owner]);
 const install=useCallback((value:EnergyWallet)=>{setRefreshError('');setWallet(value);notify.current(value);},[]);
 const refresh=useCallback(()=>{
  if(!owner||exchanging.current)return Promise.resolve();if(flight.current)return flight.current;
  const token=generation.current,readVersion=revision.current;const run=(async()=>{try{const next=await client.read();if(token===generation.current&&readVersion===revision.current)install(next);}catch(cause){if(token===generation.current&&(cause as {status?:number}|null)?.status!==404)setRefreshError('Не удалось обновить кошелёк. Проверь связь и обнови баланс.');}})();
  flight.current=run;void run.finally(()=>{if(flight.current===run)flight.current=undefined;});return run;
 },[owner,client,install]);
 useEffect(()=>{generation.current++;exchanging.current=false;flight.current=undefined;setWallet(undefined);setMessage('');setRefreshError('');setBusy(false);void refresh();return()=>{generation.current++;};},[owner,refresh]);
 useEffect(()=>{if(!owner)return;const resume=()=>{if(document.visibilityState==='visible')void refresh();};window.addEventListener('focus',resume);window.addEventListener('online',resume);document.addEventListener('visibilitychange',resume);return()=>{window.removeEventListener('focus',resume);window.removeEventListener('online',resume);document.removeEventListener('visibilitychange',resume);};},[owner,refresh]);
 useEffect(()=>{if(!wallet)return;const delay=walletRefreshDelay(wallet);if(delay===undefined)return;const timer=setTimeout(()=>{if(document.visibilityState==='visible')void refresh();},Math.min(2147483647,delay));return()=>clearTimeout(timer);},[wallet,refresh]);
 const exchangeNow=useCallback(async()=>{if(!owner||exchanging.current)return;const token=generation.current;exchanging.current=true;revision.current++;flight.current=undefined;setBusy(true);setMessage('');try{const outcome=await exchange.run();if(token!==generation.current||!outcome)return;if(outcome.wallet)install(outcome.wallet);setMessage(exchangeMessage(outcome));}finally{if(token===generation.current){revision.current++;exchanging.current=false;setBusy(false);}}},[owner,busy,exchange,install]);
 return {wallet,busy,message:[message,refreshError].filter(Boolean).join(' '),refresh,exchange:exchangeNow};
}
