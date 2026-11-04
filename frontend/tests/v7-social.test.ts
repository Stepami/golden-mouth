import test from 'node:test';
import assert from 'node:assert/strict';
import {channelSubscribers} from '../src/v7/social/audience.ts';
import {relationEmoji} from '../src/v7/social/emotions.ts';
test('unknown NPC audience never becomes a fake zero',()=>{
 assert.equal(channelSubscribers({}),undefined);assert.equal(channelSubscribers({channelSubscribers:null}),undefined);assert.equal(channelSubscribers({channelSubscribers:0}),0);assert.equal(channelSubscribers({subscribers:120}),120);assert.equal(channelSubscribers({subscribers:-5}),undefined);
});
test('notification emotion follows real relationship state and unknown stays generic',()=>{
 assert.equal(relationEmoji(0),'😐');assert.equal(relationEmoji(3),'😄');assert.equal(relationEmoji(-1),'😞');assert.equal(relationEmoji(-3),'😠');assert.equal(relationEmoji(undefined),'🔔');
});
