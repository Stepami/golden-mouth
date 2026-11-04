import {EnergyInventory} from './EnergyInventory';
import type {Person} from '../domain/types';
import { useEffect, useState } from 'react';
import { Avatar, Button, Chip, Icon, Progress } from '../ui';
import { formatXP, upgradeCost } from '../xp/wallet';
import type { StoryPlay } from './model';
import type {Curriculum} from './curriculum';
import type { PilotCampaign } from '../story/pilot';
import { storyForCampaign } from '../story/pilot';
import { boosterCatalog, stockCount, type BoosterId } from './boosters';
import { BoosterArt } from './BoosterPicker';

const openingCopy: Record<string,{title:string;premise:string}> = {
  sorcerers: { title:'Спаси Хогвартс', premise:'Школу хотят превратить в отель. Ты можешь помочь вернуть её ученикам.' },
  evening: { title:'Улица на чужом снимке', premise:'На ночном фото появилась давно снесённая улица. Воспоминания жителей не совпадают. Разберись, что произошло.' },
  houses: { title:'Две печати одного совета', premise:'Два дома получили письма с одним обещанным престолом. Печати одинаковые, подписи разные. Выясни, какому обещанию можно доверять.' },
  it: { title:'Демо, которое можно повторить', premise:'Громкий анонс «Смены» уже вышел, доказательств ещё нет. Разберись, что действительно показали и какой результат можно повторить.' },
};
export function StoryOpening({ campaign, goal, skills, step, onNext, onBack, playerName, avatar, worldTitle, firstVisit=false }: {
  campaign:PilotCampaign; goal:string; skills:string[]; step:'mission'|'skills'; onNext:()=>void; onBack:()=>void;
  playerName:string; avatar?:string; worldTitle:string; firstVisit?:boolean;
}) {
  const story=storyForCampaign(campaign), copy=openingCopy[story.worldKey];
  const descriptions:Record<string,string>={Смелость:'Задавай прямые вопросы и проявляй инициативу.',Юмор:'Находи общий язык и снимай напряжение.','В тему':'Замечай детали и обосновывай позицию.',Дипломатия:'Согласовывай интересы и находи общий язык.',Фристайл:'Пробуй неожиданные формулировки и собственный стиль.'};
  return <section className="section story-opening" aria-label={step==='mission'?'Знакомство с историей':'Знакомство с навыками'}>
    <header className="opening-header">
      {step==='skills'&&<button className="opening-back" onClick={onBack} aria-label="Назад к цели">←</button>}
      <Avatar name={playerName} src={avatar}/><div className="opening-identity"><strong>{worldTitle}</strong><span>{playerName}</span></div>
      <span className="opening-pagination" aria-label={`Шаг ${step==='mission'?1:2} из 2`}>{step==='mission'?1:2} <span>из 2</span></span>
    </header>
    {step==='mission'?<>
      <h1 className="display">{copy?.title ?? story.scenario}</h1><p className="opening-premise">{copy?.premise ?? story.premise}</p>
      <div className="opening-goal"><div className="opening-goal-label"><Icon name="goals"/><h2>Твоя цель</h2></div><p>{goal}</p></div>
      <p className="opening-explanation">Проходи квесты, чтобы приблизиться к своей цели.</p>
    </>:<>
      <h1 className="display">Твои навыки</h1><div className="opening-skills">{skills.map((name,i)=><article key={name}><span aria-hidden="true">{['↗','✳','◎'][i]}</span><div><h2>{name}</h2><p>{descriptions[name]||'Развивай навык, чтобы открывать новые подходы к цели.'}</p></div></article>)}</div>
      <p className="opening-explanation">Развивай навыки — открывай более острые квесты.</p>
      <details className="opening-xp-info"><summary>Как работает XP <span aria-hidden="true">+</span></summary><p>{import.meta.env.DEV?'XP за квесты можно распределять между навыками в локальной пробе; серверные навыки остаются прежними.':'Навыки развиваются после подтверждённых действий. Ручное распределение XP появится позже.'}</p></details>
    </>}
    <footer className="opening-actions"><Button onClick={onNext}>{step==='mission'?'Дальше: твои навыки':firstVisit?'К первому квесту':'К текущему квесту'}</Button></footer>
  </section>;
}
export function StoryMap({ campaign, state, currentId, onSelect,actors=[],preview=true }: { preview?:boolean; campaign: Curriculum; actors?:Person[]; state: StoryPlay; currentId?: string; onSelect: (id: string) => void }) {
  const quests=campaign.days.flatMap(d=>d.quests),availableIndex=quests.findIndex(q=>!state.completed.includes(q.id));
  const currentDay=Math.max(0,campaign.days.findIndex(d=>d.quests.some(q=>q.id===currentId)));
  const [selectedDay,setSelectedDay]=useState(currentDay);
  const day=campaign.days[selectedDay]??campaign.days[0];
  const complete=preview&&day.quests.every(q=>state.completed.includes(q.id));
  const actorId=day.day%7===0?campaign.actorOrder?.filter(id=>!campaign.initialActors?.includes(id))[Math.floor(day.day/7)-1]:undefined;
  return <div className="story-map vector-map"><p className="caption muted">Твой маршрут · {campaign.days.length} дней · по 3 квеста</p>
    <nav className="vector-days" aria-label="Дни истории">{campaign.days.map((d,i)=>{const done=preview&&d.quests.every(q=>state.completed.includes(q.id));return <button key={d.day} aria-pressed={selectedDay===i} className={`${selectedDay===i?'is-selected ':''}${done?'is-done':''}`} onClick={()=>setSelectedDay(i)}><span>{done?'✓':d.day}</span><small>День {d.day}</small></button>;})}</nav>
    <section className="vector-detail"><h3>День {day.day}<span className="caption muted">{preview?`${day.quests.filter(q=>state.completed.includes(q.id)).length}/3`:'План'}</span></h3>
    <div className="vector-quests">{day.quests.map((q,i)=><button key={q.id} disabled={!preview||(state.questAwards?.[q.id]?.status!=='partial'&&(state.completed.includes(q.id)||availableIndex<0||quests.findIndex(v=>v.id===q.id)>availableIndex))} className={`vector-quest${preview&&q.id===currentId?' is-current':''}${preview&&state.completed.includes(q.id)?' is-done':''}${preview&&state.questAwards?.[q.id]?.status==='partial'?' is-partial':''}`} onClick={()=>onSelect(q.id)}><span className="vector-node">{preview&&state.questAwards?.[q.id]?.status==='partial'?'◐':preview&&state.completed.includes(q.id)?'✓':i+1}</span><span>{q.title}{preview&&<small>{state.questAwards?.[q.id]?.status==='partial'?`Допройти · ещё ${formatXP(state.questAwards[q.id].capCents-state.questAwards[q.id].baseCents)} базового XP`:`${q.baseXP} XP`}</small>}</span><span aria-hidden="true">↗</span></button>)}</div>
    {preview&&<div className="vector-milestone"><strong>{complete?'День пройден':'Награда дня'}</strong><span>XP для развития навыков{actorId?` · ${actors.find(a=>a.id===actorId)?.name??'новый персонаж'} подпишется в пробе`:''}</span></div>}</section>
    <p className="caption muted">{preview?'Локальный маршрут · прогресс сохраняется на этом устройстве.':'План истории. Отслеживание квестов появится позже.'}</p></div>;

}
export function PlayMode({ value, onChange }: { value:'story'|'sandbox'; onChange:(mode:'story'|'sandbox')=>void }) {
  return <div className="play-mode"><div className="row"><Chip selected={value==='story'} onClick={()=>onChange('story')}>Сюжет</Chip><Chip selected={value==='sandbox'} onClick={()=>onChange('sandbox')}>Песочница</Chip></div><details><summary aria-label="О сюжете и песочнице">i</summary><p>Сюжет — выполняй текущий квест. В локальной пробе подтверждённая публикация или ответ могут закрыть его. Песочница — свободный разговор без сюжетного XP.</p></details></div>;
}
export function DemoInventory({state,onEnable,onDisable,onGrant,onBuy,selected,onSelect}:{state:StoryPlay;onEnable:()=>void;onDisable:()=>void;onGrant:()=>void;onBuy:(id?:BoosterId)=>void;selected?:BoosterId;onSelect:(id?:BoosterId)=>void}) {
 return <div className="stack"><p className="local-story-notice">Локальная проба. Это тестовый инвентарь, не серверные покупки.</p><div className="inventory-balance"><span>Кристаллы <b>{state.crystals}</b></span><span>Бустеры XP <b>{state.boosters}</b></span></div>
 {!state.enabled?<Button onClick={onEnable}>Включить локальную пробу</Button>:<><EnergyInventory items={state.energyBoosters??[]}/>{boosterCatalog.map(b=><article className="inventory-item stack" key={b.id}><div className="booster-product-head"><BoosterArt id={b.id}/><div><h3>{b.name}</h3><p>+{b.percent}% XP за квест</p><small>В наличии: {stockCount(state,b.id)}</small></div></div><Button disabled={state.crystals<b.price} onClick={()=>onBuy(b.id)}>Купить за {b.price} {b.price===2||b.price===4?'кристалла':'кристаллов'}</Button><Button variant="secondary" disabled={!stockCount(state,b.id)} onClick={()=>onSelect(selected===b.id?undefined:b.id)}>{selected===b.id?'Убрать бустер':'Использовать в следующем действии'}</Button></article>)}{!state.bonusUsed&&<Button variant="ghost" onClick={onGrant}>Добавить 24 тестовых кристалла</Button>}<Button variant="ghost" onClick={onDisable}>Выключить локальную пробу</Button><p className="caption muted">Бустер расходуется при любом подтверждённом сюжетном исходе, включая неуспех. Техническая ошибка до результата его не расходует.</p></>}
 </div>;
}
export function StoryWallet({ state, labels, onEnable, onSpend }: { state:StoryPlay; labels:string[]; onEnable:()=>void; onSpend:(points:[number,number,number])=>void }) {
  const [points,setPoints]=useState<[number,number,number]>([0,0,0]); const [message,setMessage]=useState('');
  let cost=0;points.forEach((n,i)=>{for(let k=0;k<n;k++)cost+=upgradeCost(state.levels[i]+k);});
  return <section className="section stack compact-wallet"><div className="xp-summary"><h2>XP персонажа</h2><p className="xp-balance">{formatXP(state.total)} / {formatXP(state.available)} XP</p><p className="caption muted">Всего / доступно для навыков</p></div>{!!state.pendingXP&&<div className="xp-day-pending"><strong>{formatXP(state.pendingXP)} XP ждут завершения дня</strong><p>Закрой три квеста — и направь опыт на навыки.</p></div>}{!state.enabled?<Button onClick={onEnable}>Включить локальную пробу</Button>:<><details className="wallet-skills" open={state.available>0||undefined}><summary>Развить навыки <span>{formatXP(state.available)} XP доступно</span></summary>{labels.map((name,i)=><article className="xp-skill stack" key={name}><div className="row between"><strong>{name}</strong><span>{state.levels[i]}{points[i]?` → ${state.levels[i]+points[i]}`:''}/100</span></div><Progress label={name} value={state.levels[i]+points[i]} max={100}/><div className="xp-skill-actions"><Button variant="secondary" aria-label={`Убрать улучшение ${name}`} disabled={!points[i]} onClick={()=>setPoints(old=>old.map((n,k)=>k===i?n-1:n) as typeof old)}>−</Button><span>{formatXP(upgradeCost(state.levels[i]+points[i]))} XP за +1</span><Button variant="secondary" aria-label={`Улучшить ${name}`} disabled={state.levels[i]+points[i]>=100||cost+upgradeCost(state.levels[i]+points[i])>state.available} onClick={()=>setPoints(old=>old.map((n,k)=>k===i?n+1:n) as typeof old)}>+</Button></div></article>)}<p>Останется {formatXP(Math.max(0,state.available-cost))} XP</p><Button disabled={!cost||cost>state.available} onClick={()=>{onSpend(points);setPoints([0,0,0]);setMessage('Навыки улучшены. Карта мира учитывает новые уровни пробы.');}}>Подтвердить улучшения</Button><p role="status" className="caption">{message}</p><p className="caption muted">Локальная проба · серверные навыки не меняются.</p></details></>}</section>;
}
export function QuestReward({ amount, title, animate=false }: { amount:number; title:string; animate?:boolean }) {
  const [count,setCount]=useState(animate?0:amount);
  useEffect(()=>{const reduced=matchMedia('(prefers-reduced-motion: reduce)').matches;if(reduced||!animate){setCount(amount);return;}let frame=0;const start=performance.now();const tick=(now:number)=>{const t=Math.min(1,(now-start)/600);setCount(Math.round(amount*t*10)/10);if(t<1)frame=requestAnimationFrame(tick);};frame=requestAnimationFrame(tick);return()=>cancelAnimationFrame(frame);},[amount,animate]);
  return <div className="quest-reward" role="status"><span>✓ Квест закрыт в пробе</span><strong>{title}</strong><b aria-hidden="true">+{formatXP(Math.round(count*100))} XP</b><span className="sr-only">Получено {amount} XP</span></div>;
}
export function XPFlight({ amount, nonce }: { amount:number; nonce:string }) {
  const [position,setPosition]=useState<{x:number;y:number;dx:number;dy:number}>();
  useEffect(()=>{if(amount<=0||matchMedia('(prefers-reduced-motion: reduce)').matches)return;const a=document.querySelector('.quest-reward')?.getBoundingClientRect(),b=document.querySelector('.profile-access')?.getBoundingClientRect();if(a&&b)setPosition({x:a.x+a.width/2,y:a.y+20,dx:b.x+b.width/2-a.x-a.width/2,dy:b.y+b.height/2-a.y-20});const timer=setTimeout(()=>setPosition(undefined),1700);return()=>clearTimeout(timer);},[nonce]);
  return position?<span className="xp-flight" style={{left:position.x,top:position.y,'--fly-x':`${position.dx}px`,'--fly-y':`${position.dy}px`} as React.CSSProperties}>+{formatXP(Math.round(amount*100))} XP</span>:null;
}
export function TypingDraft({ value, animate, disabled, onChange, onTyping }: { value:string; animate:boolean; disabled:boolean; onChange:(text:string)=>void; onTyping:(active:boolean)=>void }) {
  const [limit,setLimit]=useState(()=>animate&&!matchMedia('(prefers-reduced-motion: reduce)').matches?0:value.length);
  useEffect(()=>{if(!animate||matchMedia('(prefers-reduced-motion: reduce)').matches){setLimit(value.length);onTyping(false);return;}onTyping(true);let timer=setInterval(()=>setLimit(n=>Math.min(value.length,n+Math.max(5,Math.ceil(value.length/35)))),35);const end=setTimeout(()=>{clearInterval(timer);setLimit(value.length);onTyping(false);},1300);return()=>{clearInterval(timer);clearTimeout(end);onTyping(false);};},[animate]);
  const typing=animate&&limit<value.length;
  return <><textarea id="post-text" className="composer-text" aria-label="Текст публикации" value={typing?value.slice(0,limit):value} onFocus={()=>{if(typing){setLimit(value.length);onTyping(false);}}} onChange={e=>{setLimit(e.target.value.length);onTyping(false);onChange(e.target.value);}} disabled={disabled} maxLength={2000}/>{typing&&<button className="link-button caption" onClick={()=>{setLimit(value.length);onTyping(false);}}>Показать весь текст</button>}</>;
}
