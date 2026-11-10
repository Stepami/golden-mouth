import type {SeriesGift} from './gifts';
export const boosterCatalog = [
 {id:'spark',name:'Искра',percent:25,price:2,color:'var(--color-progress)'},
 {id:'lift',name:'Разгон',percent:50,price:4,color:'var(--color-crystal)'},
 {id:'wave',name:'Волна',percent:75,price:6,color:'var(--color-goal)'},
 {id:'focus',name:'Фокус',percent:100,price:8,color:'var(--color-accent)'},
 {id:'burst',name:'Сверхзаряд',percent:200,price:16,color:'var(--color-energy)'},
] as const;
export type BoosterId=typeof boosterCatalog[number]['id'];
export type BoosterStock=Partial<Record<BoosterId,number>>;
export type EnergyBooster={id:string;amount:number;day:string};
export type Inventory={crystals:number;boosters:number;bonusUsed:boolean;boosterStock?:BoosterStock;openingGrant?:boolean;loginGiftDays?:string[];lastGift?:{day:string;gift:SeriesGift};energyGift?:number;energyBoosters?:EnergyBooster[]};
export function stockOf(s:Pick<Inventory,'boosters'|'boosterStock'>):BoosterStock {return s.boosterStock?{...s.boosterStock}:{focus:s.boosters};}
export function stockCount(s:Pick<Inventory,'boosters'|'boosterStock'>,id:BoosterId){return stockOf(s)[id]??0;}
export function totalStock(stock:BoosterStock){return Object.values(stock).reduce((sum,n)=>sum+(n??0),0);}
export function initialInventory(value?:Inventory):Inventory {
 const s=value&&Number.isSafeInteger(value.crystals)&&value.crystals>=0&&Number.isSafeInteger(value.boosters)&&value.boosters>=0?value:{crystals:10,boosters:0,bonusUsed:false};
 const stock=stockOf(s);
 const energyBoosters=Array.isArray(s.energyBoosters)?s.energyBoosters.filter(p=>p&&typeof p.id==='string'&&typeof p.day==='string'&&Number.isSafeInteger(p.amount)&&p.amount>0).map(p=>({...p})):[];
 if(Number.isSafeInteger(s.energyGift)&&s.energyGift!>0){const legacy=energyBoosters.find(p=>p.id==='legacy-energy-gift');if(legacy)legacy.amount=Math.max(legacy.amount,s.energyGift!);else energyBoosters.push({id:'legacy-energy-gift',amount:s.energyGift!,day:'legacy'});}
 return {...s,energyBoosters,energyGift:0,crystals:s.openingGrant||s.crystals>0||s.bonusUsed?s.crystals:10,boosterStock:stock,boosters:totalStock(stock),openingGrant:true};
}
