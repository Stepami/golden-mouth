import {useEffect,useState} from 'react';
import type { Player, PostResult } from '../../api/types';
import { Button, Icon, LoadingIndicator } from '../ui';
import type {QuestOutcome} from './outcomes';
export type PublicationReceipt={worldId:string;questId?:string;actionKind?:'post'|'comment';audienceKnown?:boolean;result:PostResult;before:Player['skills'];after?:Player['skills'];questReward?:{amount:number;title:string;questId:string};questOutcome?:QuestOutcome};
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
 const outcome=receipt.questOutcome??(receipt.questReward?{status:'success',title:receipt.questReward.title,reason:'Действие сохранено. Квест закрыт в локальной пробе.',source:'local',xp:receipt.questReward.amount}:{status:'unverified',title:'Твоя публикация',reason:'Пост сохранён. Серверный зачёт квеста пока не подключён.',source:'server'});
 const title={success:'Квест выполнен',partial:'Есть прогресс',failed:'Пока не получилось',unverified:isComment?'Комментарий сохранён':'Пост опубликован'}[outcome.status];
 const xp=preview?(receipt.questReward?.amount??outcome.xp):undefined;
 return <details className="receipt-history" aria-label="Результат публикации">
  <summary><Icon name="check"/><span>{title}</span><b>{receipt.audienceKnown===false?'':`${result.delta>=0?'+':''}${result.delta}`}</b><Icon name="chevron"/></summary>
  <div className="stack">{outcome.status!=='unverified'&&<><strong>{outcome.title}</strong><p>{outcome.reason}</p></>}
   <p>{receipt.audienceKnown===false?'Рост аудитории уточняется':`${result.delta>=0?'+':''}${result.delta} подписчиков`}</p>
   {xp!==undefined&&<p>{xp} XP за квест · локальная проба</p>}
   {outcome.status==='unverified'&&<p className="caption muted">Оценка задания пока недоступна. Публикация сохранена.</p>}
   {result.bestComment&&<blockquote>{result.bestComment.text}</blockquote>}
   <p className="caption muted">{result.views} просмотров{isComment?' у исходной записи':''}</p>
   {outcome.source==='local'&&<p className="caption muted">Это клиентская модель зачёта, не серверное решение NPC.</p>}
   <Button variant="ghost" onClick={onRead}>Читать отклики</Button>
  </div>
 </details>;
}
