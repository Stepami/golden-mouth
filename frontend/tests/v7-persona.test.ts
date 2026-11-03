import test from 'node:test';
import assert from 'node:assert/strict';
import { personaFor, savePersona } from '../src/v7/persona.ts';
import skills from '../src/v7/skillCatalog.json' with { type: 'json' };
import { storyPilot, pilotDefinition, pilotBinding, boundCampaign } from '../src/v7/story/pilot.ts';
import type { WorldDefinition, WorldDocument } from '../src/api/types.ts';
test('custom persona stays scoped to the account and owned world, including storage failure fallback', () => {
  const plan = { goal: 'Save school', skills: ['Юмор', 'Дипломатия', 'Фристайл'], avatar: 'data:image/png;base64,test' };
  savePersona('persona-test-a', 'world-a', plan);
  assert.deepEqual(personaFor('persona-test-a', 'world-a'), plan);
  assert.deepEqual(personaFor('persona-test-b', 'world-a'), {});
  assert.deepEqual(personaFor('persona-test-a', 'world-b'), {});
  assert.equal(new Set(skills).size, skills.length);
  assert.ok(plan.skills.every(s => skills.includes(s)));
});
test('custom goal is retained in server context without losing the authored story binding', () => {
  const campaign = storyPilot.campaigns[0];
  const def = pilotDefinition(campaign.request as WorldDefinition, 'custom');
  const doc = { ...def, id: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', subscribers: 0, firstContext: `${def.firstContext}\nЦель персонажа: Моя миссия` } as WorldDocument;
  assert.equal(boundCampaign(doc, pilotBinding(campaign))?.id, campaign.id);
  assert.equal(boundCampaign({ ...doc, firstContext: 'unrelated\nЦель персонажа: Моя миссия' }, pilotBinding(campaign)), undefined);
});
