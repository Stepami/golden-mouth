/** Temporary DEV-only visual credits. Never passed to the wallet API or exchange. */
type Ledger={total:number;actions:string[]};
const memory=new Map<string,Ledger>();
export function readDevCrystals(owner:string):Ledger{
 if(memory.has(owner))return memory.get(owner)!;
 let value:Ledger={total:0,actions:[]};
 try{const saved=JSON.parse(localStorage.getItem(`reki:dev-crystal-preview:${owner}`)??'null');if(saved&&typeof saved.total==='number'&&Array.isArray(saved.actions))value=saved;}catch{/* Keep the preview usable in memory. */}
 memory.set(owner,value);return value;
}
export function creditDevTurn(owner:string,actionId:string){
 const previous=readDevCrystals(owner);
 if(previous.actions.includes(actionId))return;
 const next={total:previous.total+2,actions:[...previous.actions,actionId]};
 memory.set(owner,next);
 try{localStorage.setItem(`reki:dev-crystal-preview:${owner}`,JSON.stringify(next));}catch{/* Visual credits do not affect real funds. */}
 return {actionId,beforeOffset:previous.total,afterOffset:next.total};
}
