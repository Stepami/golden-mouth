export function channelSubscribers(value:unknown):number|undefined {
 if(!value||typeof value!=='object')return;
 const record=value as Record<string,unknown>,count=record.channelSubscribers??record.subscribers;
 return typeof count==='number'&&Number.isFinite(count)&&count>=0?count:undefined;
}
export function formatAudience(value:number){return new Intl.NumberFormat('ru',{notation:value>=10000?'compact':'standard',maximumFractionDigits:1}).format(value);}
