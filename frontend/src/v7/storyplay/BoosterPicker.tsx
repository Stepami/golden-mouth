import {useState} from 'react';
import {Button} from '../ui';
import {boosterCatalog,stockCount,type BoosterId} from './boosters';
import type {StoryPlay} from './model';
export function BoosterArt({id}:{id:BoosterId}){
 const b=boosterCatalog.find(b=>b.id===id)!;
 return <svg viewBox="0 0 64 64" className="booster-art" aria-hidden="true"><circle cx="32" cy="32" r="31" fill={b.color}/><g fill="none" stroke="var(--color-ink)" strokeWidth="2.6" strokeLinecap="round" strokeLinejoin="round">
 {id==='spark'?<path d="M32 12 37 26 51 32 37 38 32 52 26 38 12 32 26 26Z"/>:id==='lift'?<path d="m18 40 14-14 14 14M18 29l14-14 14 14"/>:id==='wave'?<><path d="M12 24c10-17 20 17 40 0M12 34c10-17 20 17 40 0M12 44c10-17 20 17 40 0"/></>:id==='focus'?<><circle cx="32" cy="32" r="17"/><circle cx="32" cy="32" r="7"/><path d="M32 8v9m0 30v9M8 32h9m30 0h9"/></>:<path d="m36 9-20 27h15l-3 20 20-29H33Z"/>}
 </g></svg>;
}
export function OwnedBoosterPicker({state,selected,onSelect,onStore,available,known=available,sandbox}:{state:StoryPlay;selected?:BoosterId;onSelect:(id?:BoosterId)=>void;onStore:()=>void;available:boolean;known?:boolean;sandbox:boolean}){
 const [open,setOpen]=useState(false),item=boosterCatalog.find(b=>b.id===selected);
 const owned=boosterCatalog.filter(b=>stockCount(state,b.id)>0);
 return <section className={`owned-boosters${sandbox?' is-sandbox':''}`}><div className="row between"><h3>Бустеры</h3><span className="caption muted">{known?state.boosters:'—'} в наличии</span></div>
 <button className={`owned-booster-main${item?' is-selected':''}`} onClick={()=>available&&owned.length&&!sandbox?setOpen(v=>!v):onStore()}>
  <BoosterArt id={item?.id??owned[0]?.id??'spark'}/><span><strong>{item?item.name:'Выбрать бустер'}</strong><small>{item?`+${item.percent}% XP за этот квест`:sandbox?'В песочнице сюжетный XP не начисляется':owned.length?'Из твоего инвентаря':'Посмотреть каталог'}</small></span><span aria-hidden="true">⌄</span>
 </button>
 {open&&!sandbox&&<div className="owned-booster-list"><button className="booster-none" onClick={()=>{onSelect(undefined);setOpen(false);}}>Без бустера</button>{owned.map(b=><button key={b.id} className="owned-booster-option" onClick={()=>{onSelect(b.id);setOpen(false);}}><BoosterArt id={b.id}/><span><strong>{b.name}</strong><small>+{b.percent}% XP</small></span><b>×{stockCount(state,b.id)}</b></button>)}</div>}
 {selected&&!sandbox&&<p className="caption muted">Применится при зачёте. При ошибке останется в инвентаре.</p>}
 <Button variant="ghost" onClick={onStore}>Каталог бустеров</Button></section>;
}
