import content from './worlds.json' with { type: 'json' };
import type { WorldDefinition, WorldDocument } from '../../api/types';

export const storyPack = content;
export const storyPilot = content.worlds.find(w => w.worldKey === 'evening')!;
export type PilotCampaign = typeof content.worlds[number]['campaigns'][number];
export type PilotBinding = { packId: string; version: string; campaignId: string };
export const storyForTemplate = (id?: string | null) => content.worlds.find(w => w.templateId === id);
export const storyForCampaign = (campaign: PilotCampaign) => content.worlds.find(w => w.campaigns.some(c => c.id === campaign.id))!;
export function campaignFor(role: string | null, templateId: string | null | undefined = storyPilot.templateId): PilotCampaign | undefined {
  return storyForTemplate(templateId)?.campaigns.find(c => c.roleId === (role === 'custom' ? 'own' : role));
}
/** Settings for a NEW instance only; this does not create a story run. */
export function pilotDefinition(base: WorldDefinition, role: string): WorldDefinition {
  const campaign = campaignFor(role, base.templateId);
  if (!campaign) throw new Error('Unsupported story role or template');
  const request = structuredClone(campaign.request);
  return { ...base, ...request, drama: base.drama, difficulty: base.difficulty,
    name: role === 'custom' ? base.name : campaign.heroName,
    avatar: role === 'custom' ? base.avatar : request.avatar,
    role: role === 'custom' ? base.role : request.role,
    rules: [...base.rules.filter(rule => rule.locked), ...request.rules],
  };
}
export function pilotBinding(campaign: PilotCampaign): PilotBinding {
  return { packId: content.packId, version: content.version, campaignId: campaign.id };
}
/** A local binding cannot turn an old/template world into a story variant. */
export function boundCampaign(doc: WorldDocument, binding: PilotBinding | undefined): PilotCampaign | undefined {
  if (!binding || binding.packId !== content.packId || binding.version !== content.version || content.worlds.some(w => w.templateId === doc.id)) return;
  const campaign = content.worlds.flatMap(w => w.campaigns).find(c => c.id === binding.campaignId);
  if (!campaign || doc.description !== campaign.request.description || doc.firstContext.split('\nЦель персонажа: ')[0] !== campaign.request.firstContext) return;
  const expected = campaign.request.stars.map(s => `${s.id}:${s.name}`).sort();
  const actual = doc.stars.map(s => `${s.id}:${s.name}`).sort();
  if (JSON.stringify(actual) !== JSON.stringify(expected)) return;
  return campaign;
}
