export type QuestOutcome={status:'success'|'partial'|'failed'|'unverified';title:string;reason:string;source:'local'|'server';xp?:number};
export function parseQuestOutcome(value:unknown):QuestOutcome|undefined {
 if(!value||typeof value!=='object')return;const v=value as Record<string,unknown>;
 if(!['success','partial','failed'].includes(String(v.status))||typeof v.title!=='string'||!v.title.trim()||typeof v.reason!=='string'||!v.reason.trim())return;
 if(v.xp!==undefined&&(typeof v.xp!=='number'||!Number.isFinite(v.xp)||v.xp<0))return;
 return {status:v.status as QuestOutcome['status'],title:v.title,reason:v.reason,source:'server',...(typeof v.xp==='number'?{xp:v.xp}:{})};
}
