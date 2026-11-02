import { test } from 'node:test';
import assert from 'node:assert/strict';
import { submissionFor } from '../src/v7/submission.ts';

test('new publication does not submit retired quest selection', () => {
  const value = submissionFor({ text: 'before', ownWords: false, wantId: 'retired' }, 'new text', () => 'fresh');
  assert.deepEqual(value, { id: 'fresh', text: 'new text', ownWords: true, wantId: undefined, recovery: false });
});
test('unknown outcome preserves original id, text, mode and quest across reload', () => {
  const value = submissionFor({ text: 'edited after sending', ownWords: true, publicationId: 'same-id', submittedText: 'original', submittedOwnWords: false, submittedWantId: 'original-quest' }, 'different', () => { throw new Error('must not mint a duplicate'); });
  assert.deepEqual(value, { id: 'same-id', text: 'original', ownWords: false, wantId: 'original-quest', recovery: true });
});
test('older persisted draft can still recover without submittedText', () => {
  const value = submissionFor({ text: 'saved', ownWords: false, publicationId: 'legacy-id' }, 'changed', () => 'duplicate');
  assert.equal(value.id, 'legacy-id'); assert.equal(value.text, 'saved'); assert.equal(value.ownWords, false); assert.equal(value.recovery, true);
});
