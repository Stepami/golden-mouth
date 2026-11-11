import type {PublicationReceipt} from './publication/PublicationCards';
/** A cached DEV reward is never a production quest outcome. */
export function receiptForRelease(value:PublicationReceipt|undefined,preview:boolean):PublicationReceipt|undefined {
 if(!value||preview)return value;
 const {questReward:_,questOutcome,...receipt}=value;
 return {...receipt,questOutcome:questOutcome?.source==='server'?questOutcome:undefined};
}
