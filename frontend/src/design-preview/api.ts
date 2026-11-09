// Explicit design-review fixture adapter. Imported only by design.html.
// Reuses the real screens while keeping every operation in this browser.
import {api, ApiError} from '../api/client';
import {installWorldTemplates, installWorldContent} from '../api/worldContent';
import {world as mapWorld} from '../api/mappers';
import type {WorldResponse} from '../api/generated/reki-api';
import type {Player, WorldDocument, Post, CommentT, FeedItem} from '../api/types';
import type {EnergyWallet} from '../v7/energy/exchange';
import templatesJson from './worlds.json';

const storageKey = 'reki:design-preview';
const playerId = 'design-preview-player';
const templates = (templatesJson as unknown as WorldResponse[]).map(mapWorld);
interface PreviewState { onboarded: boolean; player?: Player; worlds: WorldDocument[]; posts: Record<string, Post[]>; crystals: number; energy: number }
const fresh = (): PreviewState => ({onboarded:false,worlds:[],posts:{},crystals:10,energy:10});
let state: PreviewState;
try { state=JSON.parse(localStorage.getItem(storageKey) || 'null') || fresh(); } catch { state=fresh(); }
const copy = <T,>(value:T):T => structuredClone(value);
const save = () => localStorage.setItem(storageKey,JSON.stringify(state));
const basePlayer:Player={id:playerId,address:'nick',name:'Твой персонаж',handle:'my.story',role:'Автор',world:'',subs:120,postsLeft:10,postsCap:10,firstPostInviteBonus:0,
 streak:{days:1,week:[true,false,false,false,false,false,false],nextReward:''},skills:{brave:{level:1,progress:.25},humor:{level:1,progress:.2},topic:{level:1,progress:.4}},
 wants:[],collection:[],bio:'',type:'',drama:'mid',voice:'',settings:{notifications:'до 3 в день',quiet:'22:00–9:00',plus:false}};
const currentWorld = () => state.worlds.find(w=>w.id===state.player?.world)!;
const currentPosts = () => state.posts[currentWorld().id] ??= [];
const wallet = ():EnergyWallet => ({amount:state.energy,cap:10,crystals:state.crystals,regenSeconds:600,nextEnergyAtUtc:null,
 exchangeQuote:{targetEnergy:10,crystalCost:3,available:state.energy<10&&state.crystals>=3,unavailableReason:state.energy===10?'energy_full':state.crystals<3?'insufficient_crystals':null}});
const player = ():Player => ({...state.player!,postsLeft:state.energy,energyWallet:wallet()});
const actorReply = (text:string,parent:string|null=null):CommentT => {
 const star=currentWorld().stars[0];
 return {id:crypto.randomUUID(),authorId:`Star_${star.id}`,author:star.name,avatar:star.avatarSeed,handle:star.role,role:star.role,star:true,depth:parent?1:0,parentCommentId:parent,text};
};
const sampleReply = 'В этом примере я предлагаю начать с одной детали. Что мы уже знаем наверняка, а что стоит уточнить у остальных?';

export function resetPreview() {
 for(const key of Object.keys(localStorage)) if(key===storageKey||key.startsWith(`reki:v7:${playerId}`)||key.startsWith('reki:v7:guest')||key.includes(playerId))localStorage.removeItem(key);
 location.href='design.html';
}

export function installPreviewApi() {
 // No unimplemented preview action can fall through to the real API.
 for(const key of Object.keys(api)) Object.assign(api,{[key]:()=>{throw new Error('Это действие не входит в дизайн-превью.');}});
 const preview:Partial<typeof api>={
  loadWorldTemplates:async()=>{installWorldTemplates(templates);installWorldContent(state.worlds);},
  hasPlayer:()=>state.onboarded,
  playerId:()=>state.onboarded?playerId:undefined,
  onboarding:async body=>{state.onboarded=true;save();return {...basePlayer,energy:state.energy,address:body.address,experiments:{}};},
  me:async id=>{
   if(id){const world=state.worlds.find(w=>w.id===id)!;state.player={...basePlayer,...state.player,name:world.name,handle:world.avatar,role:world.role,world:world.id,worldInstanceId:world.id,subs:world.subscribers};save();}
   if(!state.player)throw new ApiError(404,'GET','/me');
   return copy(player());
  },
  ownedWorlds:async()=>copy(state.worlds),
  ownedWorldsSnapshot:()=>copy(state.worlds),
  createWorld:async definition=>{
   const world={...copy(definition),id:crypto.randomUUID(),subscribers:120};
   state.worlds.push(world);installWorldContent(state.worlds);
   return api.me(world.id);
  },
  energyWallet:async()=>wallet(),
  exchangeEnergy:async()=>{
   const exchanged=wallet().exchangeQuote.available;
   if(exchanged){state.energy=10;state.crystals-=3;save();}
   return {exchanged,wallet:wallet()};
  },
  feed:async worldId=>({nextCursor:null,items:(state.posts[worldId||state.player!.world]||[]).map((post):FeedItem=>({kind:'post',postId:post.id,text:post.text,author:state.player!.name,avatar:state.player!.handle,ago:post.at,reactions:post.reactions,views:post.views,commentCount:post.comments.length}))}),
  post:async id=>copy(Object.values(state.posts).flat().find(p=>p.id===id)!),
  publish:(text,id,onEvent,onError,onStatus)=>{
   onStatus?.('pending');
   const timer=setTimeout(()=>{
    if(state.energy<1){onError('failed');return;}
    const post:Post={id,text,authorId:`Player_${playerId.replaceAll('-','')}`,at:new Date().toISOString(),views:24,reactions:[],comments:[actorReply(sampleReply)]};
    currentPosts().unshift(post);state.energy--;state.player!.subs+=12;currentWorld().subscribers=state.player!.subs;save();
    onStatus?.('completed');onEvent({type:'result',result:{postId:id,delta:12,views:24,headline:'Пост опубликован',viral:false,flop:false,whys:[],ribbons:[]}});
   },1200);
   return ()=>clearTimeout(timer);
  },
  reply:async(postId,commentId,_requestId,text,_starAuthorId,options={})=>{
   const post=currentPosts().find(p=>p.id===postId)!;
   post.comments.push({id:crypto.randomUUID(),authorId:`Player_${playerId.replaceAll('-','')}`,author:state.player!.name,avatar:state.player!.handle,handle:state.player!.handle,text,me:true,parentCommentId:commentId,depth:commentId?1:0});
   state.energy=Math.max(0,state.energy-1);save();
   const detail=()=>{const {id,...rest}=post;return {post:{...copy(rest),postId:id},energyLeft:state.energy,subscribers:state.player!.subs};};
   options.onSaved?.(detail());options.onTyping?.(`Star_${currentWorld().stars[0].id}`);
   await new Promise(resolve=>setTimeout(resolve,900));
   post.comments.push(actorReply('Спасибо за уточнение. В демо так выглядит продолжение разговора — настоящий ответ появится после подключения сервера.',post.comments.at(-1)!.id));
   options.onTyping?.(null);save();const result=detail();options.onSaved?.(result);return result;
  },
  report:async()=>undefined,
  deleteAccount:async()=>{state=fresh();save();},
  clearSession:()=>{state=fresh();save();},
 };
 Object.assign(api,preview);
}
