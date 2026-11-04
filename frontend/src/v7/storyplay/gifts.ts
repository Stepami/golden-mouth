import {initialInventory,stockOf,totalStock,type Inventory,type BoosterId} from './boosters';
export type SeriesGift={kind:'energy'|'crystals';amount:number}|{kind:'booster';boosterId:BoosterId};
export function drawSeriesGift(u:number,v:number):SeriesGift {
 if(u<.45)return {kind:'energy',amount:2};if(u<.8)return {kind:'crystals',amount:1};
 const ids:BoosterId[]=['spark','lift','wave','focus','burst'],weights=[.55,.25,.12,.06,.02];let end=0;
 for(let i=0;i<ids.length;i++){end+=weights[i];if(v<end)return {kind:'booster',boosterId:ids[i]};}return {kind:'booster',boosterId:'burst'};
}
function roll(seed:string){let n=2166136261;for(const c of seed){n^=c.charCodeAt(0);n=Math.imul(n,16777619);}return (n>>>0)/4294967296;}
export function seriesGiftFor(accountId:string,day:string){return drawSeriesGift(roll(`${accountId}:${day}:gift`),roll(`${accountId}:${day}:tier`));}
export function claimSeriesGift(value:Inventory,day:string,gift:SeriesGift):Inventory {
 if(value.loginGiftDays?.includes(day))return value;const s=initialInventory(value),stock=stockOf(s);
 if(gift.kind==='booster')stock[gift.boosterId]=(stock[gift.boosterId]??0)+1;
 return {...s,loginGiftDays:[...(s.loginGiftDays??[]),day],lastGift:{day,gift},crystals:s.crystals+(gift.kind==='crystals'?gift.amount:0),energyBoosters:gift.kind==='energy'?[...(s.energyBoosters??[]),{id:`energy-gift:${day}`,amount:gift.amount,day}]:s.energyBoosters,boosterStock:stock,boosters:totalStock(stock)};
}
export const moscowDay=(date:Date=new Date())=>new Intl.DateTimeFormat('sv-SE',{timeZone:'Europe/Moscow',year:'numeric',month:'2-digit',day:'2-digit'}).format(date);
