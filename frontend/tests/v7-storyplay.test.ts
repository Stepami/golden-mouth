import test from 'node:test';
import assert from 'node:assert/strict';
import { freshStory, finishQuest, grantDemoCrystals, buyBooster, spendStoryXP, isStoryPlay } from '../src/v7/storyplay/model.ts';
test('local story is opt-in; credit is idempotent by both quest and saved action', () => {
  const off = freshStory('campaign'); assert.equal(finishQuest(off,'q1','post1',10,false),off);
  const on = {...off,enabled:true}; const first = finishQuest(on,'q1','post1',10,false);
  assert.equal(first.total,1000); assert.equal(first.available,1000);
  assert.equal(finishQuest(first,'q1','post2',10,false),first);
  assert.equal(finishQuest(first,'q2','post1',10,false),first);
  assert.deepEqual(on.completed,[]);
});
test('test crystals are one-time, purchases cannot overdraft and booster consumes only on credit', () => {
  let s = grantDemoCrystals({...freshStory('campaign'),enabled:true});
  assert.equal(s.crystals,24); assert.equal(grantDemoCrystals(s),s);
  s=buyBooster(s); assert.equal(s.crystals,16); assert.equal(s.boosters,1);
  const credited=finishQuest(s,'q','post',10,true); assert.equal(credited.total,2000); assert.equal(credited.boosters,0);
  assert.equal(finishQuest(credited,'q','post',10,true),credited);
  s=buyBooster(buyBooster(s)); assert.equal(s.crystals,0); assert.equal(buyBooster(s),s);
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
