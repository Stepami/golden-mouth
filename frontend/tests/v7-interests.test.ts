import test from 'node:test';
import assert from 'node:assert/strict';
import { rankCatalog } from '../src/v7/interests.ts';
import { catalogEntries } from '../src/v7/catalog.ts';
import type { World } from '../src/v7/domain/types.ts';
const ids = { evening: 'b3a1a522-0263-4796-a197-57a8731fc291', school: 'aec9494b-4118-4525-8458-e85d759c8dd9', houses: '8c02a7fb-3b55-4eb0-b957-1b4869ed9834', it: '5720946a-ea7c-4a10-9c50-51ce3deefc53' };
const world = (id: string) => ({ id, title: id, description: '', genre: 'test' } as World);
const real = [world(ids.it), world(ids.evening), world(ids.school), world(ids.houses)];
const planned = [world('hogwarts'), world('twin-peaks'), world('gorpcore'), world('voroniny')];
const entries = catalogEntries(real, planned);
test('different communication interests produce different first recommendations', () => {
  for (const [interest, expected] of [['Лучше слышать других', ids.evening], ['Находить общий язык', ids.school], ['Увереннее убеждать', ids.houses], ['Легче шутить', ids.it]]) assert.equal(rankCatalog(entries, [interest])[0].world.id, expected);
});
test('ranking keeps every UUID, planned status and the original array', () => {
  const before = entries.map(e => e.key), ranked = rankCatalog(entries, ['Смелее высказываться']);
  assert.deepEqual(new Set(ranked.map(e => e.key)), new Set(before));
  assert.equal(ranked.filter(e => e.available).length, 4);
  assert.deepEqual(ranked.slice(4).map(e => e.world.id), planned.map(w => w.id));
  assert.ok(ranked.slice(4).every(e => !e.available));
  assert.deepEqual(entries.map(e => e.key), before);
});
test('skipping or unknown interests preserves order; repeated choices do not add weight', () => {
  assert.deepEqual(rankCatalog(entries, []), entries);
  assert.deepEqual(rankCatalog(entries, ['unknown']), entries);
  assert.deepEqual(rankCatalog(entries, ['Быть собой', 'Быть собой']), rankCatalog(entries, ['Быть собой']));
});
test('missing server templates never activate planned worlds', () => {
  assert.ok(rankCatalog(catalogEntries([], planned), ['Легче шутить']).every(e => !e.available));
});
