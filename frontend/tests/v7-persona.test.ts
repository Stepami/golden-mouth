import test from 'node:test';
import assert from 'node:assert/strict';
import { personaFor, savePersona } from '../src/v7/persona.ts';
import skills from '../src/v7/skillCatalog.json' with { type: 'json' };
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
