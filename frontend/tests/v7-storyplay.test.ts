import test from 'node:test';
import assert from 'node:assert/strict';
import {initialInventory} from '../src/v7/storyplay/boosters.ts';
import { freshStory, finishQuest, grantDemoCrystals, buyBooster, spendStoryXP, isStoryPlay } from '../src/v7/storyplay/model.ts';
test('local story is opt-in; credit is idempotent by both quest and saved action', () => {
  const off = freshStory('campaign'); assert.equal(finishQuest(off,'q1','post1',10,false),off);
  const on = {...off,enabled:true}; const first = finishQuest(on,'q1','post1',10,false);
  assert.equal(first.total,1000); assert.equal(first.available,1000);
  assert.equal(finishQuest(first,'q1','post2',10,false),first);
  assert.equal(finishQuest(first,'q2','post1',10,false),first);
  assert.deepEqual(on.completed,[]);
});
test('new inventory gives ten crystals once, stock distinguishes booster effects', () => {
  const start=initialInventory(); assert.equal(start.crystals,10);
  assert.equal(initialInventory({...start,crystals:0}).crystals,0);
  const funded={...freshStory('campaign'),...start,enabled:true};
  const purchased=buyBooster(funded,'spark'); assert.equal(purchased.crystals,8); assert.equal(purchased.boosterStock?.spark,1);
  const credited=finishQuest(purchased,'q','post',10,'spark'); assert.equal(credited.total,1250); assert.equal(credited.boosters,0);
  assert.equal(finishQuest(credited,'q','post',10,'spark'),credited);
  const focus=buyBooster(funded); assert.equal(focus.crystals,2); assert.equal(buyBooster(focus),focus);
  assert.equal(finishQuest(focus,'q','other',10,true).total,2000);
  const bonus=grantDemoCrystals(funded); assert.equal(bonus.crystals,34); assert.equal(grantDemoCrystals(bonus),bonus);
});
test('skill allocation shares the actual local quest wallet and never reduces cumulative XP', () => {
  const s=finishQuest({...freshStory('campaign'),enabled:true},'q','post',10,false);
  const next=spendStoryXP(s,[1,1,0]); assert.equal(next.total,1000); assert.equal(next.available,600); assert.deepEqual(next.levels,[2,2,1]);
  assert.equal(spendStoryXP(next,[99,0,0]),next);
});
test('another campaign or corrupt persisted wallet is rejected', () => {
  const s=freshStory('campaign'); assert.equal(isStoryPlay(s,'campaign'),true); assert.equal(isStoryPlay(s,'another'),false);
  assert.equal(isStoryPlay({...s,crystals:-1},'campaign'),false); assert.equal(isStoryPlay({...s,completed:'q'},'campaign'),false);
});
