import {RewardArt} from '../ui/RewardArt';
import {audiencePercent} from './resultCopy';
import {SkillArt} from '../ui/GameArt';
import {useEffect,useState} from 'react';
import type { CommentT, Player, PostResult, RoleSkillState } from '../../api/types';
import { Button, Icon, LoadingIndicator, Progress } from '../ui';
import type {QuestOutcome} from './outcomes';
export type PublicationReceipt={worldId:string;worldTitle?:string;postText?:string;comment?:CommentT;commentState?:'pending'|'ready'|'failed';commentAuthor?:{name:string;avatar?:string};actionId?:string;beforeSubscribers?:number;reactionText?:string;questId?:string;actionKind?:'post'|'comment';audienceKnown?:boolean;result:PostResult;before:Player['skills'];roleBefore?:RoleSkillState[];roleAfter?:RoleSkillState[];roleLabels?:string[];after?:Player['skills'];questReward?:{amount:number;title:string;questId:string;source?:'server'|'preview';balanceBefore?:number;balanceAfter?:number};questOutcome?:QuestOutcome};
export function PublicationWaiting({pending,unknown,text,status,onContinue,onStop}:{pending:boolean;unknown:boolean;text:string;status:string;onContinue:()=>void;onStop:()=>void}){
 const [takingLong,setTakingLong]=useState(false);
 useEffect(()=>{setTakingLong(false);if(!pending)return;const timer=setTimeout(()=>setTakingLong(true),15000);return()=>clearTimeout(timer);},[pending]);
 const title=pending?(status==='ready_for_moderation'?'Проверяем публикацию':'Готовим отклики'):unknown?'Проверь отправку':'Черновик сохранён';
 const message=pending?(takingLong?'Ответ задерживается. Текст сохранён на устройстве.':'Ждём подтверждения публикации и ответа персонажей.'):unknown?'Подтверждение не получено. Проверим прежний запрос, без повторной публикации.':'Публикация не подтверждена. Можно отредактировать текст и попробовать снова.';
 return <section className="section publication-waiting"><article className="publication-card publication-card--waiting" aria-label="Состояние отправки">
  <header className="publication-heading">{pending?<LoadingIndicator/>:<span className="publication-state-icon"><Icon name={unknown?'retry':'check'}/></span>}<h2>{title}</h2></header>
  <p className="publication-status" role="status">{message}</p>
  <details className="publication-draft"><summary>Текст публикации<Icon name="chevron"/></summary><p>{text}</p></details>
  {pending?<Button className="publication-stop" variant="ghost" onClick={onStop}>Остановить ожидание</Button>:<Button onClick={onContinue}>{unknown?'Проверить публикацию':'Продолжить публикацию'}</Button>}
 </article></section>;
}
export function PublicationResult({receipt,onRead,preview=false,animateReward:_animateReward=false,animateFollowers:_animateFollowers=false}:{receipt:PublicationReceipt;onRead:()=>void;preview?:boolean;animateReward?:boolean;animateFollowers?:boolean}){
 const {result}=receipt;
 const isComment=receipt.actionKind==='comment';
 const outcome=receipt.questOutcome??({status:'unverified',title:'Твоя публикация',reason:'Пост сохранён. Серверный зачёт квеста пока не подключён.',source:'server'});
 const title={success:'Квест выполнен',partial:'Есть прогресс',failed:'Пока не получилось',unverified:isComment?'Комментарий сохранён':'Пост опубликован'}[outcome.status];
 return <details className="receipt-history" aria-label="Результат публикации">
  <summary><Icon name="check"/><span>{title}</span><b>{receipt.audienceKnown===false?'':`${result.delta>=0?'+':''}${result.delta} подписчиков`}</b><Icon name="chevron"/></summary>
  <div className="stack">{outcome.status!=='unverified'&&<><strong>{outcome.title}</strong><p>{outcome.reason}</p></>}
   {receipt.audienceKnown===false&&<p>Рост аудитории уточняется</p>}
   {receipt.audienceKnown!==false&&<p>Аудитория: {receipt.beforeSubscribers===undefined?'':`${receipt.beforeSubscribers.toLocaleString('ru')} → ${(receipt.beforeSubscribers+result.delta).toLocaleString('ru')}. `}{result.delta>=0?'+':''}{result.delta} подписчиков{audiencePercent(result.delta,receipt.beforeSubscribers)?` (${audiencePercent(result.delta,receipt.beforeSubscribers)})`:''}</p>}
   {receipt.reactionText&&<p>{receipt.reactionText}</p>}
   <SkillChanges receipt={receipt} detailed/>
   {(receipt.questReward?.source==='server'||(import.meta.env.DEV&&receipt.questReward?.source==='preview'))&&receipt.questReward.amount>0&&<p className="receipt-reward"><RewardArt kind="crystal"/>{receipt.questReward.source==='preview'?'Превью: за ход':'Награда'}: +{receipt.questReward.amount} кристаллов{receipt.questReward.balanceBefore!==undefined&&receipt.questReward.balanceAfter!==undefined&&<span> · {receipt.questReward.balanceBefore} → {receipt.questReward.balanceAfter}</span>}</p>}
   {result.bestComment&&<blockquote>{result.bestComment.text}</blockquote>}
   <p className="caption muted">{result.views} просмотров{isComment?' у исходной записи':''}</p>
   <Button variant="ghost" onClick={onRead}>Читать отклики</Button>
  </div>
 </details>;
}

function skillsFor(receipt:PublicationReceipt){
 const legacy=['brave','humor','topic'] as const;
 return (receipt.roleLabels??['Смелость','Юмор','В тему']).map((name,index)=>({
  name,
  before:receipt.roleLabels?receipt.roleBefore?.find(s=>s.name===name):receipt.before[legacy[index]],
  after:receipt.roleLabels?receipt.roleAfter?.find(s=>s.name===name):receipt.after?.[legacy[index]],
 }));
}

export function SkillGains({receipt}:{receipt:PublicationReceipt}){
 const gains=skillsFor(receipt).flatMap(({name,before,after})=>{
  if(!before||!after)return [];
  const levels=after.level-before.level,points=Math.round((after.progress-before.progress)*1000)/10;
  if(levels<0||(levels===0&&points<=0))return [];
  const label=levels>0?`Ур. ${after.level} · ${Math.round(after.progress*100)}%`:`+${points} п.п.`;
  return [<span key={name}><SkillArt name={name}/><span className="result-skill-name">{name}</span><b>{label}</b></span>];
 });
 return gains.length?<span className="result-island-skill-gains" aria-label="Выросшие навыки">{gains}</span>:null;
}

export function SkillChanges({receipt,detailed=false}:{receipt:PublicationReceipt;detailed?:boolean}){
 return <ul className={`turn-skill-changes${detailed?' turn-skill-changes--detailed':''}`} aria-label="Навыки за ход">{skillsFor(receipt).map(({name,before,after})=>{
  let change='—';
  if(before&&after){
   const levels=after.level-before.level,points=Math.round((after.progress-before.progress)*1000)/10;
   change=levels?`Ур. ${before.level} → ${after.level}`:points?`${points>0?'+':'−'}${Math.abs(points)} п.п.`:'Без изменений';
  }
  return <li key={name}><div><strong className="skill-name">{detailed?<><SkillArt name={name}/><span>{name}</span></>:name}</strong>{detailed&&<small>{before&&after?`Ур. ${before.level} · ${Math.round(before.progress*100)}% → Ур. ${after.level} · ${Math.round(after.progress*100)}%`:'Изменение навыка пока не получено'}</small>}{detailed&&after&&<Progress value={after.progress} max={1} label={`${name}: прогресс текущего уровня`}/>}</div><span data-direction={before&&after?Math.sign(after.level-before.level||after.progress-before.progress):0}>{change}</span></li>;
 })}</ul>;
}
