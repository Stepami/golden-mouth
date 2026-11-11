// Real application, isolated API and browser storage. No account or model requests.
import React from 'react';
import {createRoot} from 'react-dom/client';
import BackendApp from '../../src/v7/BackendApp';
import {api} from '../../src/api/client';
import {player as sample} from '../../src/api/mock';
import {installWorldContent,installWorldTemplates} from '../../src/api/worldContent';
import templates from '../../src/design-preview/worlds.json';
import '../../src/v7/theme.css';import '../../src/v7/ui/ui.css';import '../../src/v7/app.css';import '../../src/v7/integration.css';import '../../src/v7/night.css';import '../../src/v7/ui/motion.css';
const owner=new URLSearchParams(location.search).get('owner')??'qa-notifications';
const worlds=[{...templates[0],id:'qa-world-a',templateId:templates[0].id,name:'QA Автор'},{...templates[3],id:'qa-world-b',templateId:templates[3].id,name:'QA Второй'}];
const ownText='Моя запись о проверке обещаний: сначала сравним результаты, потом будем спорить. '.repeat(8);
const post=(id:string)=>({id,postId:id,authorId:id==='thread'?`Star_${worlds[0].stars[0].id}`:owner,text:id==='thread'?'Публикация собеседника для обсуждения':ownText,views:100,reactions:[{emoji:'♥',count:6}],comments:[{id:'reply-1',authorId:`Star_${worlds[0].stars[0].id}`,author:worlds[0].stars[0].name,avatar:'npc',text:'Ответ на твою публикацию',star:true,parentCommentId:null}]});
const player=(worldId:string)=>({...sample,id:owner,name:'QA Автор',world:worldId,worldInstanceId:worldId,subs:qa.subscribers,skills:structuredClone(qa.skills),streak:{...sample.streak,days:0},wants:[],collection:[]});
const qa={feedCalls:0,channelCalls:0,posts:[] as string[],publishCalls:0,replyCalls:0,subscribers:120,skills:structuredClone(sample.skills),failHistory:false,deferRefresh:false,release:undefined as undefined|(()=>void),intervals:[] as number[],vibrations:0};
Object.assign(window,{qa});
const interval=window.setInterval.bind(window);window.setInterval=((fn:TimerHandler,ms?:number,...args:unknown[])=>{qa.intervals.push(ms??0);return interval(fn,ms,...args);}) as typeof window.setInterval;
Object.defineProperty(navigator,'vibrate',{configurable:true,value:()=>{qa.vibrations++;return true;}});
const setItem=Storage.prototype.setItem;Storage.prototype.setItem=function(key,value){if(qa.failHistory&&key.includes(':results:'))throw new DOMException('QA quota','QuotaExceededError');return setItem.call(this,key,value);};
const prefix=`reki:v7:${owner}`;
if(owner==='qa-notifications'&&!localStorage.getItem('qa-seeded')){
 localStorage.setItem('qa-seeded','1');
 localStorage.setItem(`${prefix}:activity`,JSON.stringify([
  {id:'old-npc',worldId:worlds[0].id,title:'Скрытая публикация NPC',text:'Не должно быть видно',kind:'npc-post',postId:'npc-old',read:false},
  {id:'comment:reply-1',worldId:worlds[0].id,title:'Подтверждённый ответ',text:'Ответ на твою публикацию',kind:'reaction',postId:'thread',read:false},
  {id:'likes:own',worldId:worlds[0].id,title:'Старые реакции',text:'Сейчас реакций: 3',kind:'likes',postId:'own',count:3,read:false},
  {id:'streak',worldId:worlds[0].id,title:'Серия: 4 дня',text:'Твоя серия продолжается',kind:'streak',read:false},
 ]));
 localStorage.setItem(`${prefix}:result:${worlds[0].id}`,JSON.stringify({worldId:worlds[0].id,actionId:'legacy',before:sample.skills,beforeSubscribers:120,result:{postId:'own',delta:2,headline:'',whys:[],ribbons:[],viral:false,flop:false,views:10}}));
}
Object.assign(api,{
 loadWorldTemplates:async()=>{installWorldTemplates(templates);installWorldContent(worlds);},
 me:async(id?:string)=>{const key=`reki:selected-world:v2:${owner}`,worldId=id??localStorage.getItem(key)??worlds[0].id;localStorage.setItem(key,worldId);const snapshot=player(worldId);if(!id&&qa.deferRefresh){qa.deferRefresh=false;await new Promise<void>(resolve=>{qa.release=resolve;});}return snapshot;},
 ownedWorldsSnapshot:()=>worlds,playerId:()=>owner,
 energyWallet:async()=>({amount:10,cap:10,crystals:10,regenSeconds:600,nextEnergyAtUtc:null,exchangeQuote:{available:false,crystalCost:3,targetEnergy:10,unavailableReason:'energy_full'}}),
 feed:async()=>{qa.feedCalls++;return {items:[{kind:'post',postId:'thread',author:worlds[0].stars[0].name,avatar:worlds[0].stars[0].avatarSeed,starId:worlds[0].stars[0].id,text:'Публикация собеседника для обсуждения',commentCount:1}],nextCursor:null};},
 channelPosts:async()=>{qa.channelCalls++;return {items:[{postId:'own',text:ownText,createdAtUtc:'2026-11-11T10:00:00Z',views:100,reactions:[{emoji:'♥',count:6}],commentCount:1}],nextCursor:null};},
 post:async(id:string)=>{qa.posts.push(id);return post(id);},
 publish:(text:string,id:string,onEvent:(e:unknown)=>void)=>{qa.publishCalls++;const timer=setTimeout(()=>{qa.subscribers+=10;qa.skills.brave.progress+=.05;onEvent({type:'result',result:{postId:'own-new-'+qa.publishCalls,delta:10,headline:'Пост опубликован',whys:[],ribbons:[],viral:false,flop:false,views:100}});},40);return()=>clearTimeout(timer);},
 reply:async(_postId:string,_comment:string|null,_id:string,_text:string,_author:string,options:{onSaved:(v:unknown)=>void})=>{qa.replyCalls++;qa.subscribers+=3;qa.skills.humor.progress+=.05;const saved={post:post('thread'),subscribers:qa.subscribers,energyLeft:9};options.onSaved(saved);return saved;},
});
document.documentElement.dataset.theme='dark';
createRoot(document.getElementById('root')!).render(<BackendApp/>);
