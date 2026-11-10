import test from 'node:test';
import assert from 'node:assert/strict';
import { catalogEntries, filterCatalog, withHogwartsCard } from '../src/v7/catalog.ts';
import { scenarioForTemplate } from '../src/v7/story/roles.ts';
import type { World } from '../src/v7/domain/types.ts';
const scenario = scenarioForTemplate('aec9494b-4118-4525-8458-e85d759c8dd9')!;
const story = {templateId:scenario.templateId,scenario:scenario.title,premise:scenario.opening,subtitle:''};
const school = { id: story.templateId, title: 'Школа чародеев', description: 'Original', genre: 'Магия' } as World;
test('Hogwarts replaces the active school card and keeps its template ID', () => {
  const entries = withHogwartsCard(catalogEntries([school], []), story);
  assert.equal(entries.length, 1); assert.equal(school.title, 'Школа чародеев');
  assert.equal(entries[0].world.title, 'Спасти Хогвартс'); assert.equal(entries[0].world.id, school.id);
  assert.equal(entries[0].storyVariant, 'hogwarts'); assert.ok(entries[0].available);
  assert.equal(filterCatalog(entries, 'Хогвартс', 'Все')[0].storyVariant, 'hogwarts');
});
test('missing/disabled server template never creates an active Hogwarts card', () => {
  assert.deepEqual(withHogwartsCard([], story), []);
  const disabled = catalogEntries([], [school]);
  assert.deepEqual(withHogwartsCard(disabled, story), disabled);
});
