import test from 'node:test';
import assert from 'node:assert/strict';
import {freshStory,finishQuest,freezeStoryDay,spendStoryXP,isStoryPlay} from '../src/v7/storyplay/model.ts';
import {initialInventory} from '../src/v7/storyplay/boosters.ts';
import {claimSeriesGift,moscowDay} from '../src/v7/storyplay/gifts.ts';
const start=()=>({...freshStory('c'),enabled:true});
test('partial closes a slot; completing it later only grants the missing base XP once',()=>{
 const context={id:'c:day1',number:1,questIds:['q1','q2','q3']};let s=finishQuest(start(),'q1','partial',10,false,context,{status:'partial',earnedBasisPoints:7500});
 assert.deepEqual(s.completed,['q1']);assert.equal(s.total,750);assert.equal(s.available,0);
 s=finishQuest(s,'q2','next2',12,false,context);s=finishQuest(s,'q3','next3',14,false,context);assert.equal(s.closedDays!.length,1);assert.equal(s.available,3350);
 s=finishQuest(s,'q1','improve',10,false,context,{status:'success'});assert.equal(s.total,3600);assert.equal(s.available,3600);assert.equal(s.questAwards!.q1.status,'success');
 assert.equal(finishQuest(s,'q1','improve-replay',10,false,context),s);assert.equal(s.closedDays!.length,1);
});
test('a failed quest attempt burns the selected booster once without closing or awarding XP',()=>{
 const s={...start(),boosters:2,boosterStock:{spark:2}};const next=finishQuest(s,'q','failed',10,'spark',undefined,{status:'failed'});
 assert.equal(next.boosters,1);assert.equal(next.total,0);assert.equal(next.completed.length,0);assert.equal(finishQuest(next,'q','failed',10,'spark',undefined,{status:'failed'}),next);
 const done=finishQuest(next,'q','retry',10,'spark');assert.equal(done.boosters,0);assert.equal(done.total,1250);
});
test('boosting an improvement affects only its new base credit, never the whole quest twice',()=>{
 let s=finishQuest(start(),'q','partial',10,false,undefined,{status:'partial',earnedBasisPoints:7500});
 s={...s,boosters:1,boosterStock:{spark:1}};s=finishQuest(s,'q','improve',10,'spark');assert.equal(s.total,1063);assert.equal(s.boosters,0);
});
test('skill upgrades do not change the current day snapshot, but affect the next day',()=>{
 let s=freezeStoryDay({...start(),total:1000,available:1000},'c:day1');s=spendStoryXP(s,[1,0,0]);assert.equal(s.levels[0],2);assert.equal(s.dayLevels!['c:day1'][0],1);
 s=freezeStoryDay(s,'c:day2');assert.equal(s.dayLevels!['c:day2'][0],2);
});
test('NPC subscriptions unlock only on the seventh and fourteenth completed story day',()=>{
 let s=start();for(let d=1;d<=14;d++){const ids=[1,2,3].map(i=>`d${d}q${i}`),ctx={id:`c:day${d}`,number:d,requires:d>1?`c:day${d-1}`:undefined,questIds:ids,unlockActor:d<=7?'npc1':'npc2'};
 for(const id of ids)s=finishQuest(s,id,id,10,false,ctx);assert.equal(s.followingActors!.length,Math.floor(d/7));}
});
test('energy gifts are persistent distinct inventory items; repeat claims do not duplicate them',()=>{
 const a=claimSeriesGift(initialInventory(),'2026-11-04',{kind:'energy',amount:2});assert.equal(a.energyBoosters?.length,1);assert.equal(a.energyBoosters![0].amount,2);assert.equal(a.energyGift,0);
 const reloaded=initialInventory(JSON.parse(JSON.stringify(a)));assert.equal(claimSeriesGift(reloaded,'2026-11-04',{kind:'energy',amount:2}),reloaded);
 const b=claimSeriesGift(reloaded,'2026-11-05',{kind:'energy',amount:2});assert.equal(b.energyBoosters?.length,2);
 const migrated=initialInventory({...initialInventory(),energyBoosters:undefined,energyGift:4});assert.equal(migrated.energyBoosters![0].amount,4);assert.equal(initialInventory(migrated).energyBoosters?.length,1);assert.equal(initialInventory({...initialInventory(),energyGift:4}).energyBoosters![0].amount,4);
});
test('the daily gift boundary is midnight Moscow, independent of story progress',()=>{
 assert.equal(moscowDay(new Date('2026-11-03T20:59:59Z')),'2026-11-03');assert.equal(moscowDay(new Date('2026-11-03T21:00:00Z')),'2026-11-04');
});

test('malformed persisted day snapshots and partial grants cannot restore as valid state',()=>{assert.equal(isStoryPlay({...start(),dayLevels:{day1:[0]}},'c'),false);assert.equal(isStoryPlay({...start(),questAwards:{q:{status:'partial',baseCents:2000,capCents:1000}}},'c'),false);});
