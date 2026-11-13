import test from 'node:test'
import assert from 'node:assert/strict'
import { register } from 'node:module'
register('./typescript-resolver.mjs', import.meta.url)
const { post, feed } = await import('../src/api/mappers.ts')

test('post detail and feed preserve stored audience likes and personal marks', () => {
  const source = { postId: 'post', text: 'text', views: 100, likes: 7, likedBy: ['Player_one', 'Player_two'], comments: [], createdAtUtc: '2026-01-01T00:00:00Z', commentCount: 0 }
  for (const mapped of [post(source), feed({ ...source, kind: 'Post' })]) {
    assert.equal(mapped.likes, 7)
    assert.deepEqual(mapped.likedBy, ['Player_one', 'Player_two'])
    assert.equal(mapped.reactions, undefined)
  }
})

test('feed preserves zero likes and does not fabricate reactions for non-post rows', () => {
  const row = feed({ kind: 'Post', text: '', likes: 0, likedBy: [], views: 0, commentCount: 0 })
  assert.equal(row.likes, 0)
  assert.deepEqual(row.likedBy, [])
  assert.equal(row.reactions, undefined)
  assert.equal(feed({ kind: 'Other', text: '', likes: null, likedBy: null }).reactions, undefined)
})
