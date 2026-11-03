import content from './evening.json' with { type: 'json' };
import type { WorldDefinition, WorldDocument } from '../../api/types';

export const storyPilot = content;
export type PilotCampaign = typeof content.campaigns[number];
export type PilotBinding = { packId: string; version: string; campaignId: string };
export function campaignFor(role: string | null): PilotCampaign | undefined {
  return content.campaigns.find(c => c.roleId === (role === 'custom' ? 'own' : role));
}
/** Settings for a NEW instance only; this does not create a story run. */
export function pilotDefinition(base: WorldDefinition, role: string): WorldDefinition {
  const campaign = campaignFor(role);
  if (base.templateId !== content.templateId || !campaign) throw new Error('Unsupported pilot role or template');
  const request = structuredClone(campaign.request);
  return { ...base, ...request, drama: base.drama,
    difficulty: base.difficulty,
    name: role === 'custom' ? base.name : campaign.heroName,
    avatar: role === 'custom' ? base.avatar : request.avatar,
    role: role === 'custom' ? base.role : request.role,
    rules: [...base.rules.filter(rule => rule.locked), ...request.rules],
  };
}
export function pilotBinding(campaign: PilotCampaign): PilotBinding {
  return { packId: content.packId, version: content.version, campaignId: campaign.id };
}
/** A local binding cannot turn an old/template world into the pilot. */
export function boundCampaign(doc: WorldDocument, binding: PilotBinding | undefined): PilotCampaign | undefined {
  if (!binding || binding.packId !== content.packId || binding.version !== content.version || doc.id === content.templateId) return;
  const campaign = content.campaigns.find(c => c.id === binding.campaignId);
  if (!campaign || doc.description !== campaign.request.description || doc.firstContext !== campaign.request.firstContext) return;
  const expected = campaign.request.stars.map(s => `${s.id}:${s.name}`).sort();
  const actual = doc.stars.map(s => `${s.id}:${s.name}`).sort();
  if (JSON.stringify(actual) !== JSON.stringify(expected)) return;
  return campaign;
}
