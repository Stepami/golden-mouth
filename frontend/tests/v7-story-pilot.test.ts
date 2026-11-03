import test from 'node:test';
import assert from 'node:assert/strict';
import { storyPack, storyPilot, storyForCampaign, campaignFor, pilotDefinition, pilotBinding, boundCampaign } from '../src/v7/story/pilot.ts';
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

test('all four stories and sixteen roles preserve template UUIDs, cast and seven authored days', () => {
  assert.equal(storyPack.worlds.length, 4);
  assert.equal(storyPack.worlds.flatMap(w => w.campaigns).length, 16);
  const ids = new Set<string>();
  for (const world of storyPack.worlds) for (const campaign of world.campaigns) {
    const source = { ...campaign.request, drama: 'soft', rules: [{ text: 'Safety', locked: true }] } as WorldDefinition;
    const def = pilotDefinition(source, campaign.roleId === 'own' ? 'custom' : campaign.roleId);
    assert.equal(def.templateId, world.templateId);
    assert.equal(def.stars.length, 3);
    assert.equal(new Set(def.stars.map(s => s.id)).size, 3);
    if (campaign.roleId !== 'own') assert.ok(!def.stars.some(s => s.name === campaign.heroName));
    assert.equal(campaign.days.length, 7);
    assert.equal(campaign.days.flatMap(d => d.quests).length, 21);
    assert.ok(campaign.firstPost);
    assert.deepEqual(def.wants, []); assert.deepEqual(def.demoPosts, []);
    assert.equal(storyForCampaign(campaign).templateId, world.templateId);
    assert.ok(!ids.has(campaign.id)); ids.add(campaign.id);
    const doc = { ...def, id: instanceId, subscribers: 0 } as WorldDocument;
    assert.equal(boundCampaign(doc, pilotBinding(campaign))?.id, campaign.id);
  }
});
test('Harry scenario is explicit and cannot bleed into another world or the old school cast', () => {
  const school = storyPack.worlds.find(w => w.worldKey === 'sorcerers')!;
  const harry = campaignFor('harry', school.templateId)!;
  assert.equal(school.scenario, 'Спасти Хогвартс'); assert.equal(harry.heroName, 'Гарри Поттер');
  assert.ok(harry.request.description.includes('100 000'));
  assert.ok(harry.request.stars.every(s => s.id.startsWith('hp_')));
  assert.equal(campaignFor('harry', storyPilot.templateId), undefined);
  assert.equal(new Set(storyPack.worlds.map(w => campaignFor('custom', w.templateId)!.id)).size, 4);
  const wrong = { ...harry.request, id: instanceId, subscribers: 0, stars: storyPilot.campaigns[0].request.stars } as WorldDocument;
  assert.equal(boundCampaign(wrong, pilotBinding(harry)), undefined);
});
