import test from 'node:test';
import assert from 'node:assert/strict';
import {upsertResult,loadResults,resultHistoryKey} from '../src/v7/publication/savedResults.ts';
import type {PublicationReceipt} from '../src/v7/publication/PublicationCards';
const skills={brave:{level:1,progress:.1},humor:{level:1,progress:.2},topic:{level:1,progress:.3}};
const receipt=(actionId:string,worldId='a'):PublicationReceipt=>({worldId,actionId,actionKind:'comment',result:{postId:'same-thread',delta:2,headline:'',whys:[],ribbons:[],viral:false,flop:false,views:3},before:skills,beforeSubscribers:100,questReward:{amount:2,title:'Превью',questId:'dev-turn-preview',source:'preview',balanceBefore:10,balanceAfter:12}});
const storage=()=>{const m=new Map<string,string>();return {getItem:(key:string)=>m.get(key)??null,setItem:(key:string,value:string)=>{m.set(key,value);}};};
test('two comments on one publication are separate actions, newest first',()=>{
 const a=upsertResult([],receipt('comment-1'),{createdAt:'2026-11-11T10:00:00Z',text:'Первый'});
 const b=upsertResult(a,receipt('comment-2'),{createdAt:'2026-11-11T10:01:00Z',text:'Второй'});
 assert.deepEqual(b.map(x=>x.id),['comment-2','comment-1']);
});
test('late refresh keeps original before, time, text, reward, and list position',()=>{
 let entries=upsertResult([],receipt('first'),{createdAt:'2026-11-11T10:00:00Z',text:'Исходный текст'});entries=upsertResult(entries,receipt('second'));
 const after={...skills,brave:{level:2,progress:.5}};
 entries=upsertResult(entries,{...receipt('first'),before:after,beforeSubscribers:900,after,questReward:{...receipt('first').questReward!,balanceBefore:99,balanceAfter:101}},{text:'Подмена',createdAt:'2026-11-12T10:00:00Z'});
 const first=entries[1];assert.equal(first.id,'first');assert.equal(first.text,'Исходный текст');assert.equal(first.createdAt,'2026-11-11T10:00:00Z');assert.deepEqual(first.receipt.before,skills);assert.equal(first.receipt.beforeSubscribers,100);assert.equal(first.receipt.questReward?.balanceAfter,12);assert.deepEqual(first.receipt.after,after);
});
test('snapshot is independent of mutable server objects and preserves more than 100 actions',()=>{
 const value=receipt('one');const a=upsertResult([],value);value.result.delta=99;assert.equal(a[0].receipt.result.delta,2);
 let entries=a;for(let i=0;i<150;i++)entries=upsertResult(entries,receipt('id-'+i));assert.equal(entries.length,151);
});
test('persistence isolates account/history; production strips DEV rewards without granting anything',()=>{
 const s=storage();const entries=upsertResult([],receipt('one'));s.setItem(resultHistoryKey('user-a','a'),JSON.stringify(entries));
 assert.equal(loadResults(s,'user-a','a',true)[0].receipt.questReward?.amount,2);assert.equal(loadResults(s,'user-a','a',false)[0].receipt.questReward,undefined);
 assert.deepEqual(loadResults(s,'user-b','a',true),[]);assert.deepEqual(loadResults(s,'user-a','b',true),[]);
});
test('only the available legacy result migrates; no invented time, text or previous actions',()=>{
 const s=storage();s.setItem('reki:v7:user-a:result:a',JSON.stringify(receipt('legacy')));const entries=loadResults(s,'user-a','a',true);
 assert.equal(entries.length,1);assert.equal(entries[0].createdAt,undefined);assert.equal(entries[0].text,undefined);assert.equal(entries[0].id,'legacy');
});
test('unreadable history is reported instead of silently overwriting it',()=>{
 const s=storage();s.setItem(resultHistoryKey('user-a','a'),'bad json');assert.throws(()=>loadResults(s,'user-a','a',true));assert.equal(s.getItem(resultHistoryKey('user-a','a')),'bad json');
});
