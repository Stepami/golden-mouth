// Browser-only fixture: real BackendApp, isolated in-memory API, no account mutations.
import React from 'react';
import {createRoot} from 'react-dom/client';
import BackendApp from '../../src/v7/BackendApp';
import {api} from '../../src/api/client';
import {player as sample} from '../../src/api/mock';
import {installWorldContent, installWorldTemplates} from '../../src/api/worldContent';
import type {WorldDocument, FeedItem, Post} from '../../src/api/types';
import '../../src/v7/theme.css';
import '../../src/v7/ui/ui.css';
import '../../src/v7/app.css';
import '../../src/v7/integration.css';
import '../../src/v7/night.css';
const world: WorldDocument = {id:'qa-history',templateId:null,title:'QA-мир',emoji:'',name:'QA',avatar:'qa',role:'Автор',description:'Тест',summary:'Тест',difficulty:'normal',drama:'mid',voice:'',rules:[],firstContext:'QA',firstPrompt:'',postIdeas:[],subscribers:0,stars:[{id:'npc',name:'QA-персонаж',avatarSeed:'npc',role:'Собеседник',about:'',trait:0,relation:0}]};
const p={...sample,id:'qa-npc-inbox',name:'QA',world:world.id,worldInstanceId:world.id,streak:{...sample.streak,days:0},wants:[],collection:[]};
const row=(id:string):FeedItem=>({kind:'post',postId:id,author:'QA-персонаж',text:'Текст '+id,avatar:'npc'});
const qa={items:[row('npc-1'),row('own')],failOpen:false,failDetail:false,feedCalls:0,postCalls:[] as string[], add(id:string){this.items.unshift(row(id));},resume(){window.dispatchEvent(new Event('focus'));}};
Object.assign(window,{qa});
Object.assign(api,{
 loadWorldTemplates:async()=>{installWorldTemplates([world]);installWorldContent([world]);},
 me:async()=>p,ownedWorldsSnapshot:()=>[world],playerId:()=>p.id,
 energyWallet:async()=>({amount:10,cap:10,crystals:0,regenSeconds:600,nextEnergyAtUtc:null,exchangeQuote:{available:false,crystalCost:3,targetEnergy:10,unavailableReason:'energy_full'}}),
 feed:async()=>{qa.feedCalls++;return {items:[...qa.items],nextCursor:null};},
 post:async(id:string):Promise<Post>=>{qa.postCalls.push(id);if(qa.failDetail||(qa.failOpen&&id==='npc-1'))throw Error('offline');return {id,authorId:id==='own'?p.id:'Star_npc',text:'Полный текст '+id,comments:[],views:0,reactions:[]};},
});
document.documentElement.dataset.theme='dark';
createRoot(document.getElementById('root')!).render(<BackendApp/>);
