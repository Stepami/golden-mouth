import type { Player, PostResult } from '../../api/types';
import { Button,Icon } from '../ui';
import {AnimatedGain} from './RewardMotion';
import type {QuestOutcome} from './outcomes';
export type PublicationReceipt={worldId:string;actionKind?:'post'|'comment';audienceKnown?:boolean;result:PostResult;before:Player['skills'];after?:Player['skills'];questReward?:{amount:number;title:string;questId:string};questOutcome?:QuestOutcome};
export function PublicationWaiting({pending,unknown,text,status,onContinue,onStop}:{pending:boolean;unknown:boolean;text:string;status:string;onContinue:()=>void;onStop:()=>void}){
 return <section className="section"><article className="publication-card stack" aria-label="Состояние отправки"><h2>{pending?status==='ready_for_moderation'?'Проверяем публикацию':'Готовим отклики':unknown?'Проверь отправку':'Черновик сохранён'}</h2><p role="status">{pending?'Ждём подтверждения сервера. Пока неизвестно, опубликована ли запись.':unknown?'Результат пока неизвестен. Проверка использует тот же запрос и не создаёт повторный пост.':'Публикация не подтверждена. Можно изменить текст и попробовать снова.'}</p><blockquote>{text}</blockquote>{pending?<Button variant="secondary" onClick={onStop}>Остановить ожидание</Button>:<Button onClick={onContinue}>{unknown?'Проверить публикацию':'Продолжить публикацию'}</Button>}</article></section>;
}
export function PublicationResult({receipt,onRead,animateReward=false,animateFollowers=false}:{receipt:PublicationReceipt;onRead:()=>void;animateReward?:boolean;animateFollowers?:boolean}){
 const {result}=receipt;
 const isComment=receipt.actionKind==='comment';
 const outcome=receipt.questOutcome??(receipt.questReward?{status:'success',title:receipt.questReward.title,reason:'Действие сохранено. Квест закрыт в локальной пробе.',source:'local',xp:receipt.questReward.amount}:{status:'unverified',title:'Твоя публикация',reason:'Пост сохранён. Серверный зачёт квеста пока не подключён.',source:'server'});
 const title={success:'Квест выполнен',partial:'Есть прогресс',failed:'Пока не получилось',unverified:isComment?'Комментарий сохранён':'Пост опубликован'}[outcome.status];
 const xp=receipt.questReward?.amount??outcome.xp;
 return <section className="section"><article className={`publication-card publication-card--result result-summary result-summary--${outcome.status}`} aria-label="Результат публикации">
  <header className="result-outcome"><span className="result-emblem" aria-hidden="true">{({success:'✓',partial:'↗',failed:'↻',unverified:'…'} as const)[outcome.status]}</span><div><p>{outcome.source==='local'?'Результат сюжетной пробы':'Результат публикации'}</p><h2>{title}</h2></div></header>
  <div className="result-quest-copy"><strong>{outcome.title}</strong><p>{outcome.reason}</p></div>
  <div className="result-gains"><div className="result-gain result-followers-gain"><Icon name="member"/><b>{receipt.audienceKnown===false?'—':<AnimatedGain value={result.delta} animate={animateFollowers}/>}</b><span>подписчиков</span></div><div className={`result-gain quest-reward${xp===undefined?' is-unverified':''}`}><span className="result-xp-mark" aria-hidden="true">✦</span><b>{xp===undefined?'—':<AnimatedGain value={xp} animate={animateReward}/>}</b><span>XP за квест</span></div></div>
  <Button className="result-read" onClick={onRead}>Читать реакцию персонажей</Button>
  <details className="result-details"><summary>Что произошло<span aria-hidden="true">+</span></summary><div className="stack"><p>{outcome.reason}</p>{result.bestComment&&<blockquote>{result.bestComment.text}</blockquote>}<p className="caption muted">{result.views} просмотров{isComment?' у исходной записи':''}</p>{outcome.source==='local'&&<p className="caption muted">Это клиентская модель зачёта, не серверное решение NPC.</p>}</div></details>
 </article></section>;
}
