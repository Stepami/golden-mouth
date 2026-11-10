import test from 'node:test';
import assert from 'node:assert/strict';
import {storyPack} from '../src/v7/story/pilot.ts';
import {freshStory,finishQuest,spendStoryXP,repairClosedDays} from '../src/v7/storyplay/model.ts';
import {claimSeriesGift,drawSeriesGift} from '../src/v7/storyplay/gifts.ts';
import {initialInventory} from '../src/v7/storyplay/boosters.ts';
import {legacyCurriculum} from '../src/v7/storyplay/curriculum.ts';
import {parseQuestOutcome} from '../src/v7/publication/outcomes.ts';
import type {World} from '../src/v7/domain/types.ts';
test('all16roles: ordered3quest days, escrow XP, one release, manual levels and no early NPC unlocks',()=>{
 for(const c of storyPack.worlds.flatMap(w=>w.campaigns)){
  let s={...freshStory(c.id),enabled:true};const first=c.days[0];const context={id:`${c.id}:day1`,questIds:first.quests.map(q=>q.id),unlockActor:c.actorOrder.find(a=>!c.initialActors.includes(a))};
  assert.equal(finishQuest(s,first.quests[1].id,'out-of-order',12,false,context),s);
  first.quests.forEach((q,i)=>{s=finishQuest(s,q.id,`${c.id}:a${i}`,q.baseXP,false,context);assert.deepEqual(s.levels,[1,1,1]);if(i<2){assert.equal(s.available,0);assert.ok(s.pendingXP!>0);}});
  assert.equal(s.total,3600);assert.equal(s.available,3600);assert.equal(s.pendingXP,0);assert.equal(s.closedDays!.length,1);
  assert.equal(finishQuest(s,first.quests[2].id,`${c.id}:a2`,14,false,context),s);
  s=spendStoryXP(s,[1,0,0]);assert.equal(s.total,3600);assert.equal(s.available,3400);assert.equal(s.levels[0],2);
  const second=c.days[1],next={id:`${c.id}:day2`,requires:context.id,questIds:second.quests.map(q=>q.id),unlockActor:c.actorOrder.find(a=>!c.initialActors.includes(a)&&!s.followingActors!.includes(a))};
  assert.equal(finishQuest({...freshStory(c.id),enabled:true},second.quests[0].id,'too-early',10,false,next).total,0);
  second.quests.forEach((q,i)=>{s=finishQuest(s,q.id,`${c.id}:b${i}`,q.baseXP,false,next);});assert.equal(s.closedDays!.length,2);assert.equal(new Set(s.followingActors).size,0);
 }
});
test('gift probabilities and one account-day claim survive world switches without reroll',()=>{
 assert.deepEqual(drawSeriesGift(.1,.1),{kind:'energy',amount:2});assert.deepEqual(drawSeriesGift(.6,.1),{kind:'crystals',amount:1});assert.deepEqual(drawSeriesGift(.95,.1),{kind:'booster',boosterId:'spark'});
 const first=claimSeriesGift(initialInventory(),'2026-11-04',{kind:'crystals',amount:1});assert.equal(first.crystals,11);assert.equal(claimSeriesGift(first,'2026-11-04',{kind:'booster',boosterId:'burst'}),first);
});
test('legacy world gets a separate short curriculum and old completed days never mint XP again',()=>{
 const world={id:'owned',title:'Школа чародеев',topic:'урок',goal:'Узнать историю',quests:[],cast:[{id:'npc',name:'Персонаж'}]} as unknown as World;
 const c=legacyCurriculum(world);assert.equal(c.days[0].quests.length,3);assert.ok(c.id.includes('owned'));
 const old={...freshStory('x'),total:3600,available:3400,completed:['a','b','c']};const repaired=repairClosedDays(old,[{id:'d1',questIds:['a','b','c']}]);assert.deepEqual(repaired.closedDays,['d1']);assert.equal(repaired.available,3400);assert.equal(repaired.total,3600);
});
test('unknown or malformed API grades never become a successful quest outcome',()=>{
 assert.equal(parseQuestOutcome({status:'success',title:'q'}),undefined);assert.equal(parseQuestOutcome({status:'failed',title:'q',reason:'no',xp:-10}),undefined);
 assert.equal(parseQuestOutcome({status:'partial',title:'q',reason:'Нужна смета',xp:7.5})?.status,'partial');
});
test('all authored roles complete seven ordered days without duplicate rewards',()=>{
 let count=0;
 for(const c of storyPack.worlds.flatMap(w=>w.campaigns)){
  let s={...freshStory(c.id),enabled:true},sum=0;
  for(const day of c.days){const ctx={id:`${c.id}:day${day.day}`,requires:day.day>1?`${c.id}:day${day.day-1}`:undefined,questIds:day.quests.map(q=>q.id)};
   for(const q of day.quests){const before=s;s=finishQuest(s,q.id,q.id,q.baseXP,false,ctx);assert.notEqual(s,before,`${c.id}/${q.id}`);assert.equal(finishQuest(s,q.id,`${q.id}-retry`,q.baseXP,false,ctx),s);sum+=q.baseXP*100;count++;}
   assert.equal(s.pendingXP,0);assert.equal(s.total,sum);assert.equal(s.available,sum);assert.deepEqual(s.levels,[1,1,1]);
  }
  assert.equal(s.closedDays!.length,7);assert.equal(s.completed.length,21);
 }
 assert.equal(count,336);
});
