import {useEffect,useRef,useState} from 'react';
import type {PublicationReceipt} from './PublicationCards';
import {loadResults,resultHistoryKey,resultId,upsertResult,type SavedResult} from './savedResults';
import {receiptForRelease} from '../releasePolicy';

export function useResultHistory(owner:string|undefined,worldId:string|undefined) {
  const cache=useRef(new Map<string,SavedResult[]>());
  const [snapshot,setSnapshot]=useState<{key:string;items:SavedResult[]}>({key:'',items:[]});
  const [errors,setErrors]=useState<Record<string,string>>({});
  const scope=owner&&worldId?resultHistoryKey(owner,worldId):'';
  const activeScope=useRef(scope);activeScope.current=scope;
  const fail=(key:string)=>setErrors(old=>({...old,[key]:'Не удалось сохранить историю результатов на устройстве. Не закрывай вкладку: новые результаты пока доступны только здесь.'}));
  useEffect(()=>{
    if(!owner||!worldId)return;
    const key=resultHistoryKey(owner,worldId);
    let items=cache.current.get(key);
    try{if(!items){items=loadResults(localStorage,owner,worldId,import.meta.env.DEV);cache.current.set(key,items);}}catch{fail(key);return;}
    setSnapshot({key,items});
    try{localStorage.setItem(key,JSON.stringify(items));}catch{fail(key);}
  },[owner,worldId]);

  const save=(value:PublicationReceipt,text?:string):PublicationReceipt=>{
    if(!owner)return value;
    const key=resultHistoryKey(owner,value.worldId);
    let items=cache.current.get(key);
    try{if(!items)items=loadResults(localStorage,owner,value.worldId,import.meta.env.DEV);}catch{fail(key);return value;}
    const next=upsertResult(items,receiptForRelease(value,import.meta.env.DEV)!,{text,createdAt:new Date().toISOString()});
    cache.current.set(key,next);if(activeScope.current===key)setSnapshot({key,items:next});
    const entry=next.find(item=>item.id===resultId(value))!;
    try{localStorage.setItem(key,JSON.stringify(next));if(next[0].id===entry.id)localStorage.setItem(`reki:v7:${owner}:result:${value.worldId}`,JSON.stringify(entry.receipt));setErrors(old=>({...old,[key]:''}));}catch{fail(key);}
    return entry.receipt;
  };
  return {items:snapshot.key===scope?snapshot.items:[],error:errors[scope],save};
}
