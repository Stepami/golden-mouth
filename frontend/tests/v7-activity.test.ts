import { test } from 'node:test';
import assert from 'node:assert/strict';
import { activitiesForWorld, markWorldActivitiesRead, npcPostActivity } from '../src/v7/activity.ts';
import type {FeedItem, Post, WorldDocument} from '../src/api/types.ts';
const entries = [
  { id: 'a', worldId: 'history-a', title: 'A', text: '', read: false },
  { id: 'b', worldId: 'history-b', title: 'B', text: '', read: false },
];
test('another history cannot suppress the empty state of a new history', () => {
  assert.deepEqual(activitiesForWorld(entries, 'history-c'), []);
  assert.equal(activitiesForWorld(entries, 'history-a')[0].id, 'a');
});
test('read all in one history preserves unread activity of another history', () => {
  const updated = markWorldActivitiesRead(entries, 'history-a');
  assert.equal(updated[0].read, true); assert.equal(updated[1].read, false);
  assert.equal(entries[0].read, false);
});
test('without a selected history no world notification is marked as read', () => {
  assert.deepEqual(markWorldActivitiesRead(entries), entries);
  assert.deepEqual(activitiesForWorld(entries), []);
});
const world = {id:'history-a',stars:[{id:'npc',name:'Персонаж'}]} as WorldDocument;
const row: FeedItem = {kind:'post',postId:'p1',author:'Персонаж',text:'preview'};
const post = {id:'p1',authorId:'Star_npc',text:'full server text'} as Post;
test('NPC publication targets the exact server post in its history', () => {
  const entry = npcPostActivity(row,post,world)!;
  assert.equal(entry.postId,'p1'); assert.equal(entry.worldId,'history-a');
  assert.equal(entry.kind,'npc-post'); assert.equal(entry.read,false);
  assert.equal(entry.text,post.text); assert.equal(entry.actorName,'Персонаж');
});
test('display names never turn own or unknown authors into NPC publications', () => {
  for(const authorId of ['Player_1',undefined,'Star_foreign']) assert.equal(npcPostActivity(row,{...post,authorId},world),undefined);
});
test('demo/news/missing IDs/mismatched detail cannot create a post notification', () => {
  for(const item of [{...row,demo:true},{...row,kind:'news' as const},{...row,postId:undefined}]) assert.equal(npcPostActivity(item,post,world),undefined);
  assert.equal(npcPostActivity(row,{...post,id:'another-post'},world),undefined);
});
