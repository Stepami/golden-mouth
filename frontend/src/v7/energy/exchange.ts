export type EnergyWallet={amount:number;cap:number;nextEnergyAtUtc:string|null;regenSeconds:number;crystals:number;exchangeQuote:{crystalCost:number;targetEnergy:number;available:boolean;unavailableReason:string|null}};
export type WalletClient={read:()=>Promise<EnergyWallet>;exchange:()=>Promise<{exchanged:boolean;wallet:EnergyWallet|null}>};
export type ExchangeOutcome={kind:'success'|'unavailable'|'uncertain';wallet?:EnergyWallet};
/** One click, one POST. A missing reply is resolved with a GET, never another POST. */
export class EnergyExchange {
 private pending=false;
 private readonly client:WalletClient;
 constructor(client:WalletClient){this.client=client;}
 async run():Promise<ExchangeOutcome|undefined>{
  if(this.pending)return;this.pending=true;
  try{
   try{const result=await this.client.exchange();if(result.exchanged&&result.wallet)return {kind:'success',wallet:result.wallet};}
   catch{try{return {kind:'uncertain',wallet:await this.client.read()};}catch{return {kind:'uncertain'};}}
   try{return {kind:'unavailable',wallet:await this.client.read()};}catch{return {kind:'uncertain'};}
  }finally{this.pending=false;}
 }
}
export function exchangeMessage(outcome:ExchangeOutcome){
 if(outcome.kind==='success')return 'Энергия пополнена.';
 if(outcome.kind==='uncertain')return outcome.wallet?'Ответ об обмене не получен. Баланс обновлён. Автоматического повтора нет.':'Не удалось проверить обмен. Проверь связь и обнови баланс. Автоматического повтора нет.';
 return outcome.wallet?.exchangeQuote.unavailableReason==='energy_full'?'Запас энергии уже полный.':outcome.wallet?.exchangeQuote.unavailableReason==='insufficient_crystals'?'Недостаточно кристаллов.':'Обмен сейчас недоступен. Баланс обновлён.';
}
/** First read after the expected tick; bounded retries while the server job is late. */
export function walletRefreshDelay(wallet:EnergyWallet,now=Date.now()):number|undefined{
 if(wallet.amount>=wallet.cap||!wallet.nextEnergyAtUtc)return;
 const remaining=Date.parse(wallet.nextEnergyAtUtc)-now;
 if(!Number.isFinite(remaining))return Math.max(15000,wallet.regenSeconds*1000);
 return remaining>0?Math.min(Math.max(15000,wallet.regenSeconds*1000),remaining+750):15000;
}
