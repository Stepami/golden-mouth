import test from 'node:test'
import assert from 'node:assert/strict'
import { register } from 'node:module'
register('./typescript-resolver.mjs', import.meta.url)
const { post, channelPost, feed } = await import('../src/api/mappers.ts')

test('post detail and channel show audience and author likes with a fire reaction', () => {
  const source = { postId: 'post', text: 'text', views: 100, likes: 7, likedBy: ['Player_one', 'Player_two'], comments: [], createdAtUtc: '2026-01-01T00:00:00Z', commentCount: 0 }
  for (const mapped of [post(source), channelPost(source)]) {
    assert.equal(mapped.likes, 7)
    assert.deepEqual(mapped.likedBy, ['Player_one', 'Player_two'])
    assert.deepEqual(mapped.reactions, [{ emoji: '🔥', count: 9 }])
  }
})

test('feed preserves zero likes and does not fabricate reactions for non-post rows', () => {
  const row = feed({ kind: 'Post', text: '', likes: 0, likedBy: [], views: 0, commentCount: 0 })
  assert.deepEqual(row.reactions, [{ emoji: '🔥', count: 0 }])
  assert.equal(feed({ kind: 'Other', text: '', likes: null, likedBy: null }).reactions, undefined)
})
