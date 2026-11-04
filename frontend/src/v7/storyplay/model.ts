import { boosterCatalog, stockOf, stockCount, totalStock, type BoosterId, type BoosterStock, type EnergyBooster } from './boosters';
import { allocate, validWallet, type XPWallet } from '../xp/wallet';
export type StoryPlay = XPWallet & { version: 1; enabled: boolean; campaignId: string; completed: string[]; actions: string[]; crystals: number; boosters: number; bonusUsed: boolean; boosterStock?:BoosterStock; openingGrant?:boolean; pendingXP?:number; closedDays?:string[]; followingActors?:string[]; energyBoosters?:EnergyBooster[]; dayLevels?:Record<string,[number,number,number]>; questAwards?:Record<string,{baseCents:number;capCents:number;status:'partial'|'success'}> };
export const freshStory = (campaignId: string): StoryPlay => ({ version: 1, enabled: false, campaignId, total: 0, available: 0, levels: [1,1,1], completed: [], actions: [], crystals: 10, boosters: 0, bonusUsed: false, pendingXP:0, closedDays:[], followingActors:[] });
export function buyBooster(s:StoryPlay,id:BoosterId='focus'):StoryPlay {
 const item=boosterCatalog.find(b=>b.id===id);if(!s.enabled||!item||s.crystals<item.price)return s;
 const stock=stockOf(s);stock[id]=(stock[id]??0)+1;
 return {...s,crystals:s.crystals-item.price,boosterStock:stock,boosters:totalStock(stock)};
}
export function grantDemoCrystals(s: StoryPlay): StoryPlay { return !s.enabled || s.bonusUsed ? s : { ...s, crystals: s.crystals + 24, bonusUsed: true }; }
export type QuestGrade={status:'success'|'partial'|'failed';earnedBasisPoints?:number};
export function freezeStoryDay(s:StoryPlay,dayId:string):StoryPlay {
 if(!s.enabled||!dayId||s.dayLevels?.[dayId])return s;
 return {...s,dayLevels:{...s.dayLevels,[dayId]:[...s.levels] as [number,number,number]}};
}
export function finishQuest(s: StoryPlay, questId: string, actionId: string, xp: number, boosted: boolean | BoosterId, day?:{id:string;number?:number;questIds:string[];unlockActor?:string;requires?:string}, grade:QuestGrade={status:'success'}): StoryPlay {
 const previous=s.questAwards?.[questId];
 if(!s.enabled||s.actions.includes(actionId)||!questId||!actionId||!Number.isSafeInteger(xp)||xp<=0||!Number.isSafeInteger(xp*100))return s;
 if(s.completed.includes(questId)&&previous?.status!=='partial')return s;
 if(day){const index=day.questIds.indexOf(questId);if(index<0||day.questIds.slice(0,index).some(id=>!s.completed.includes(id))||(day.requires&&!(s.closedDays??[]).includes(day.requires)))return s;}
 const basis=grade.status==='success'?10000:grade.status==='failed'?0:grade.earnedBasisPoints;
 if(basis===undefined||!Number.isInteger(basis)||basis<0||basis>10000||(grade.status==='partial'&&(basis===0||basis===10000)))return s;
 const id=boosted===true?'focus':boosted||undefined,item=id?boosterCatalog.find(b=>b.id===id):undefined;
 const use=!!id&&!!item&&stockCount(s,id)>0,stock=stockOf(s);if(use&&id)stock[id]=(stock[id]??0)-1;
 const cap=previous?.capCents??xp*100;
 const entitlement=grade.status==='failed'?0:Math.min(grade.status==='partial'?cap-1:cap,Math.round(cap*basis/10000));
 const baseDelta=Math.max(0,entitlement-(previous?.baseCents??0));
 const amount=Math.round(baseDelta*(1+(use?item!.percent:0)/100));
 const completed=grade.status==='failed'||s.completed.includes(questId)?s.completed:[...s.completed,questId];
 const closedDays=s.closedDays??[],closes=!!day&&day.questIds.length===3&&day.questIds.every(id=>completed.includes(id))&&!closedDays.includes(day.id);
 const dayClosed=!!day&&closedDays.includes(day.id),pending=(s.pendingXP??0)+amount;
 const dayNumber=day?.number??Number(day?.id.match(/day(\d+)$/)?.[1]??0);
 const unlock=closes&&dayNumber>0&&dayNumber%7===0?day?.unlockActor:undefined;
 const frozen=day?freezeStoryDay(s,day.id):s;
 return {...frozen,total:s.total+amount,available:s.available+(day?(closes?pending:dayClosed?amount:0):amount),pendingXP:day?(closes?0:dayClosed?(s.pendingXP??0):pending):(s.pendingXP??0),completed,actions:[...s.actions,actionId],closedDays:closes?[...closedDays,day!.id]:closedDays,followingActors:unlock?[...new Set([...(s.followingActors??[]),unlock])]:s.followingActors??[],boosterStock:stock,boosters:totalStock(stock),questAwards:grade.status==='failed'?s.questAwards:{...s.questAwards,[questId]:{baseCents:Math.max(entitlement,previous?.baseCents??0),capCents:cap,status:grade.status==='success'?'success':'partial'}}};
}
export function spendStoryXP(s: StoryPlay, points: [number,number,number]): StoryPlay { const next = allocate(s, points); return next && s.enabled ? { ...s, ...next } : s; }

export function isStoryPlay(value: unknown, campaignId: string): value is StoryPlay {
  if (!value || typeof value !== 'object' || !validWallet(value)) return false;
  const s = value as StoryPlay;
  const validLevels=(v:unknown)=>Array.isArray(v)&&v.length===3&&v.every(n=>Number.isInteger(n)&&n>=1&&n<=100);
  if(s.dayLevels!==undefined&&(!s.dayLevels||typeof s.dayLevels!=='object'||!Object.values(s.dayLevels).every(validLevels)))return false;
  if(s.questAwards!==undefined&&(!s.questAwards||typeof s.questAwards!=='object'||!Object.values(s.questAwards).every(a=>a&&typeof a==='object'&&['success','partial'].includes(a.status)&&Number.isSafeInteger(a.baseCents)&&Number.isSafeInteger(a.capCents)&&a.baseCents>=0&&a.capCents>0&&a.baseCents<=a.capCents)))return false;
  return s.version === 1 && s.campaignId === campaignId && typeof s.enabled === 'boolean' && Array.isArray(s.completed) && s.completed.every(id => typeof id === 'string') && Array.isArray(s.actions) && s.actions.every(id => typeof id === 'string') && Number.isSafeInteger(s.crystals) && s.crystals >= 0 && Number.isSafeInteger(s.boosters) && s.boosters >= 0;
}

export function repairClosedDays(s:StoryPlay,days:{id:string;questIds:string[]}[]):StoryPlay {
 const closed=[...new Set([...(s.closedDays??[]),...days.filter(d=>d.questIds.length===3&&d.questIds.every(id=>s.completed.includes(id))).map(d=>d.id)])];
 return {...s,pendingXP:s.pendingXP??0,closedDays:closed,followingActors:s.followingActors??[]};
}
