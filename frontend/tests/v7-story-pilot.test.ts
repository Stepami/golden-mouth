import test from 'node:test';
import assert from 'node:assert/strict';
import { storyPilot, campaignFor, pilotDefinition, pilotBinding, boundCampaign } from '../src/v7/story/pilot.ts';
import type { WorldDefinition, WorldDocument } from '../src/api/types.ts';
const base = { ...storyPilot.campaigns[0].request, drama: 'soft', rules: [{ text: 'Safety', locked: true }] } as WorldDefinition;
const instanceId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
test('all pilot roles keep the source template, exactly three NPCs, and player outside the cast', () => {
  for (const campaign of storyPilot.campaigns) {
    const def = pilotDefinition(base, campaign.roleId === 'own' ? 'custom' : campaign.roleId);
    assert.equal(def.templateId, storyPilot.templateId);
    assert.equal(def.stars.length, 3);
    assert.equal(new Set(def.stars.map(s => s.id)).size, 3);
    if (campaign.roleId !== 'own') assert.ok(!def.stars.some(s => s.id === campaign.roleId));
    assert.deepEqual(def.wants, []);
    assert.deepEqual(def.demoPosts, []);
    assert.equal(def.drama, 'soft');
    assert.ok(def.rules.some(r => r.locked && r.text === 'Safety'));
  }
});
test('custom identity and source definition are preserved', () => {
  const input = { ...base, name: 'My name', avatar: 'handle', role: 'Witness' };
  const copy = structuredClone(input), def = pilotDefinition(input, 'custom');
  assert.deepEqual(input, copy);
  assert.equal(def.name, input.name); assert.equal(def.avatar, input.avatar); assert.equal(def.role, input.role);
  assert.throws(() => pilotDefinition({ ...base, templateId: 'other' }, 'custom'));
  assert.throws(() => pilotDefinition(base, 'unknown'));
});
test('resume needs current binding AND matching server-owned content, never a template or old world', () => {
  const campaign = campaignFor('custom')!, def = pilotDefinition(base, 'custom');
  const doc = { ...def, id: instanceId, subscribers: 0 } as WorldDocument;
  const binding = pilotBinding(campaign);
  assert.equal(boundCampaign(doc, binding)?.id, campaign.id);
  assert.equal(boundCampaign(doc, undefined), undefined);
  assert.equal(boundCampaign(doc, { ...binding, version: 'stale' }), undefined);
  assert.equal(boundCampaign({ ...doc, id: storyPilot.templateId }, binding), undefined);
  assert.equal(boundCampaign({ ...doc, firstContext: 'old scenario' }, binding), undefined);
  assert.equal(boundCampaign({ ...doc, stars: doc.stars.slice(1) }, binding), undefined);
});
test('first day contains authored hints only: no synthetic outcome, XP or completion state', () => {
  for (const c of storyPilot.campaigns) {
    assert.equal(c.day.quests.length, 3);
    for (const q of c.day.quests) {
      assert.ok(q.prompt && q.criterion);
      assert.ok(!('approaches' in q)); assert.ok(!('completed' in q)); assert.ok(!('baseXP' in q));
    }
  }
});
