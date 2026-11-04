import { boosterCatalog, stockOf, stockCount, totalStock, type BoosterId, type BoosterStock } from './boosters';
import { allocate, validWallet, type XPWallet } from '../xp/wallet';
export type StoryPlay = XPWallet & { version: 1; enabled: boolean; campaignId: string; completed: string[]; actions: string[]; crystals: number; boosters: number; bonusUsed: boolean; boosterStock?:BoosterStock; openingGrant?:boolean };
export const freshStory = (campaignId: string): StoryPlay => ({ version: 1, enabled: false, campaignId, total: 0, available: 0, levels: [1,1,1], completed: [], actions: [], crystals: 10, boosters: 0, bonusUsed: false });
export function buyBooster(s:StoryPlay,id:BoosterId='focus'):StoryPlay {
 const item=boosterCatalog.find(b=>b.id===id);if(!s.enabled||!item||s.crystals<item.price)return s;
 const stock=stockOf(s);stock[id]=(stock[id]??0)+1;
 return {...s,crystals:s.crystals-item.price,boosterStock:stock,boosters:totalStock(stock)};
}
export function grantDemoCrystals(s: StoryPlay): StoryPlay { return !s.enabled || s.bonusUsed ? s : { ...s, crystals: s.crystals + 24, bonusUsed: true }; }
export function finishQuest(s: StoryPlay, questId: string, actionId: string, xp: number, boosted: boolean | BoosterId): StoryPlay {
  if (!s.enabled || s.completed.includes(questId) || s.actions.includes(actionId) || !questId || !actionId || !Number.isSafeInteger(xp) || xp <= 0) return s;
  const id=boosted===true?'focus':boosted||undefined;
  const item=id?boosterCatalog.find(b=>b.id===id):undefined;
  const use=!!id&&!!item&&stockCount(s,id)>0;
  const stock=stockOf(s);if(use&&id)stock[id]=(stock[id]??0)-1;
  const amount=Math.round(xp*100*(1+(use?item!.percent:0)/100));
  return { ...s, total: s.total + amount, available: s.available + amount, completed: [...s.completed, questId], actions: [...s.actions, actionId], boosterStock:stock, boosters:totalStock(stock) };
}
export function spendStoryXP(s: StoryPlay, points: [number,number,number]): StoryPlay { const next = allocate(s, points); return next && s.enabled ? { ...s, ...next } : s; }

export function isStoryPlay(value: unknown, campaignId: string): value is StoryPlay {
  if (!value || typeof value !== 'object' || !validWallet(value)) return false;
  const s = value as StoryPlay;
  return s.version === 1 && s.campaignId === campaignId && typeof s.enabled === 'boolean' && Array.isArray(s.completed) && s.completed.every(id => typeof id === 'string') && Array.isArray(s.actions) && s.actions.every(id => typeof id === 'string') && Number.isSafeInteger(s.crystals) && s.crystals >= 0 && Number.isSafeInteger(s.boosters) && s.boosters >= 0;
}
