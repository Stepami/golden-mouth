import type {PublicationReceipt} from './PublicationCards';
import {receiptForRelease} from '../releasePolicy';

export type SavedResult = {id:string;createdAt?:string;text?:string;receipt:PublicationReceipt};
export const resultHistoryKey = (owner:string,worldId:string) => `reki:v7:${owner}:results:${worldId}`;
export const resultId = (value:PublicationReceipt) => value.actionId ?? `legacy:${value.actionKind??'post'}:${value.result.postId}`;

/** New actions prepend; a late refresh replaces its action without moving it or changing its initial snapshot. */
export function upsertResult(items:SavedResult[],value:PublicationReceipt,metadata:{createdAt?:string;text?:string}={}):SavedResult[] {
  const id=resultId(value),previous=items.find(item=>item.id===id);
  const receipt=previous?{...previous.receipt,...value,before:previous.receipt.before,roleBefore:previous.receipt.roleBefore,beforeSubscribers:previous.receipt.beforeSubscribers,actionKind:previous.receipt.actionKind,questReward:previous.receipt.questReward??value.questReward}:value;
  const entry:SavedResult=JSON.parse(JSON.stringify({id,createdAt:previous?previous.createdAt:metadata.createdAt,text:previous?.text??metadata.text,receipt}));
  return previous?items.map(item=>item.id===id?entry:item):[entry,...items];
}

export function loadResults(storage:Pick<Storage,'getItem'|'setItem'>,owner:string,worldId:string,preview:boolean):SavedResult[] {
  const raw=storage.getItem(resultHistoryKey(owner,worldId));
  let items:SavedResult[];
  if(raw!==null){
    items=JSON.parse(raw);
    if(!Array.isArray(items)||items.some(item=>!item.id||item.receipt?.worldId!==worldId||!item.receipt.result?.postId))throw Error('Invalid result history');
  } else {
    const legacy=JSON.parse(storage.getItem(`reki:v7:${owner}:result:${worldId}`)??'null') as PublicationReceipt|null;
    items=legacy?.worldId===worldId?upsertResult([],legacy):[];
    // Migration is persisted by the caller, so a write failure still leaves the readable legacy result visible.
  }
  return items.map(item=>({...item,receipt:receiptForRelease(item.receipt,preview)!}));
}
