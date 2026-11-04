import { allocate, validWallet, type XPWallet } from '../xp/wallet';
export type StoryPlay = XPWallet & { version: 1; enabled: boolean; campaignId: string; completed: string[]; actions: string[]; crystals: number; boosters: number; bonusUsed: boolean };
export const freshStory = (campaignId: string): StoryPlay => ({ version: 1, enabled: false, campaignId, total: 0, available: 0, levels: [1,1,1], completed: [], actions: [], crystals: 0, boosters: 0, bonusUsed: false });
export function buyBooster(s: StoryPlay): StoryPlay { return !s.enabled || s.crystals < 8 ? s : { ...s, crystals: s.crystals - 8, boosters: s.boosters + 1 }; }
export function grantDemoCrystals(s: StoryPlay): StoryPlay { return !s.enabled || s.bonusUsed ? s : { ...s, crystals: s.crystals + 24, bonusUsed: true }; }
export function finishQuest(s: StoryPlay, questId: string, actionId: string, xp: number, boosted: boolean): StoryPlay {
  if (!s.enabled || s.completed.includes(questId) || s.actions.includes(actionId) || !questId || !actionId || !Number.isSafeInteger(xp) || xp <= 0) return s;
  const use = boosted && s.boosters > 0;
  const amount = xp * (use ? 2 : 1) * 100;
  return { ...s, total: s.total + amount, available: s.available + amount, completed: [...s.completed, questId], actions: [...s.actions, actionId], boosters: s.boosters - (use ? 1 : 0) };
}
export function spendStoryXP(s: StoryPlay, points: [number,number,number]): StoryPlay { const next = allocate(s, points); return next && s.enabled ? { ...s, ...next } : s; }

export function isStoryPlay(value: unknown, campaignId: string): value is StoryPlay {
  if (!value || typeof value !== 'object' || !validWallet(value)) return false;
  const s = value as StoryPlay;
  return s.version === 1 && s.campaignId === campaignId && typeof s.enabled === 'boolean' && Array.isArray(s.completed) && s.completed.every(id => typeof id === 'string') && Array.isArray(s.actions) && s.actions.every(id => typeof id === 'string') && Number.isSafeInteger(s.crystals) && s.crystals >= 0 && Number.isSafeInteger(s.boosters) && s.boosters >= 0;
}
