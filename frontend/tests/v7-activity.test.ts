import { test } from 'node:test';
import assert from 'node:assert/strict';
import { activitiesForWorld, markWorldActivitiesRead } from '../src/v7/activity.ts';
import type {Activity} from '../src/v7/activity.ts';
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
test('saved NPC posts never enter events or the unread badge; other events remain', () => {
  const all:Activity[]=[{...entries[0],kind:'npc-post'},{...entries[0],id:'likes',kind:'likes',count:3},{...entries[0],id:'reply',kind:'reaction'},{...entries[0],id:'streak',kind:'streak'}];
  assert.deepEqual(activitiesForWorld(all,'history-a').map(a=>a.id),['likes','reply','streak']);
  assert.equal(activitiesForWorld(all,'history-a').filter(a=>!a.read).length,3);
});
