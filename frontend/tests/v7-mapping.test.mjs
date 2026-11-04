import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createServer } from 'vite';
const server = await createServer({ configFile: false, server: { middlewareMode: true }, appType: 'custom' });
const { feedView, detailView, formatDate } = await server.ssrLoadModule('/src/v7/backend.ts');
const player = { id: 'player', name: 'Автор', handle: '@channel', role: 'Автор', boundary: '' };
try {
  await test('kind=post does not identify a server NPC post as the player', () => {
    const value = feedView({ kind: 'post', postId: 'npc-post', author: 'NPC', avatar: 'npc', text: 'hello', role: 'Собеседник' }, player);
    assert.equal(value.author.name, 'NPC'); assert.notEqual(value.post.authorId, 'player');
  });
  await test('the player post keeps identity and real reaction counts', () => {
    const value = feedView({ kind: 'post', postId: 'mine', author: 'Автор', avatar: '@channel', text: 'hello', reactions: [{ emoji: '❤️', count: 3 }], commentCount: 2 }, player);
    assert.equal(value.post.authorId, 'player'); assert.equal(value.post.likes, 3); assert.equal(value.count, 2);
  });
  await test('thread author uses Star_ identifier rather than the player fallback', () => {
    const value = detailView({ id: 'post', authorId: 'Star_npc', text: 'hello', comments: [], reactions: [], views: 0 }, { cast: [{ id: 'npc', name: 'NPC' }] }, player);
    assert.equal(value.author.name, 'NPC'); assert.notEqual(value.post.authorId, 'player');
  });
  await test('dates with .NET fractional seconds are compact and malformed input is retained', () => {
    assert.ok(!formatDate('2026-10-28T18:25:01.1820000+00:00').includes('T18:'));
    assert.equal(formatDate('вчера'), 'вчера');
  });
} finally { await server.close(); }
