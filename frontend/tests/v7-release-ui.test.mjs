import test from 'node:test';
import assert from 'node:assert/strict';
import {createElement} from 'react';
import {renderToStaticMarkup as render} from 'react-dom/server';
import {createServer} from 'vite';
const server=await createServer({configFile:false,server:{middlewareMode:true},appType:'custom'});
try{
 const {AccountDeletion}=await server.ssrLoadModule('/src/v7/screens/AccountDeletion.tsx');
 const {ReportComment}=await server.ssrLoadModule('/src/v7/screens/ReportComment.tsx');
 const {PostCard}=await server.ssrLoadModule('/src/v7/ui/index.tsx');
 const {ResourcePages}=await server.ssrLoadModule('/src/v7/screens/ResourcePages.tsx');
 const {CurrentSkills}=await server.ssrLoadModule('/src/v7/screens/CurrentSkills.tsx');
 const {WalletView}=await server.ssrLoadModule('/src/v7/energy/WalletView.tsx');
 await test('opening account deletion only displays an explicit irreversible confirmation',()=>{let calls=0;const html=render(createElement(AccountDeletion,{onDelete:async()=>{calls++;},onClose(){}}));assert.equal(calls,0);assert.match(html,/Удалить аккаунт навсегда/);assert.match(html,/Отмена/);assert.match(html,/Вернуть их будет нельзя/);});
 await test('report action is optional and opening its form never sends a report',()=>{const props={post:{id:'c',parentId:'p',text:'reply',createdAt:'',likes:0,authorId:'npc'},author:{id:'npc',name:'NPC',handle:'npc',role:'',boundary:''}};assert.doesNotMatch(render(createElement(PostCard,props)),/Пожаловаться/);assert.match(render(createElement(PostCard,{...props,onReport(){}})),/Пожаловаться на реплику NPC/);let calls=0;const html=render(createElement(ReportComment,{onSend:async()=>{calls++;},onClose(){}}));assert.equal(calls,0);assert.match(html,/maxLength="128"/i);assert.match(html,/Отправить жалобу/);});
 await test('public resources ignore a cached enabled preview inventory',()=>{const state={enabled:true,crystals:999,boosters:12,bonusUsed:false};for(const page of ['resources','crystals']){const html=render(createElement(ResourcePages,{preview:false,page,state,activeStory:true,onBack(){},onPage(){}}));assert.doesNotMatch(html,/999|Добавить 24|Купить за|Включить локальную пробу/);assert.match(html,/пока недоступны/);}});
 await test('public profile displays server level and progress without a spendable XP balance',()=>{const html=render(createElement(CurrentSkills,{skills:{brave:{level:4,progress:.25},humor:{level:2,progress:.5},topic:{level:1,progress:0}},labels:['Смелость','Юмор','В тему']}));assert.match(html,/Уровень 4/);assert.match(html,/aria-valuemax="1" aria-valuenow="0.25"/);assert.doesNotMatch(html,/XP за \+1|Подтвердить улучшения|36 XP/);});
 await test('the real wallet exchange button is disabled during the request and obeys the server quote',()=>{let sent=0;const wallet={amount:0,cap:10,crystals:10,regenSeconds:600,nextEnergyAtUtc:null,exchangeQuote:{available:true,crystalCost:3,targetEnergy:10,unavailableReason:null}};const html=render(createElement(WalletView,{wallet,busy:true,message:'',onExchange(){sent++;},onRefresh(){}}));assert.match(html,/disabled=""/);assert.equal(sent,0);const full=render(createElement(WalletView,{wallet:{...wallet,amount:10,exchangeQuote:{...wallet.exchangeQuote,available:false,unavailableReason:'energy_full'}},busy:false,message:'',onExchange(){sent++;},onRefresh(){}}));assert.match(full,/Запас полный/);assert.equal(sent,0);});
}finally{await server.close();}
