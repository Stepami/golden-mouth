import {useId} from 'react';
import {WalletView} from '../energy/WalletView';
import {RewardArt} from '../ui/RewardArt';
import type {EnergyWallet} from '../energy/exchange';
import { Button, Chip, ScreenHeading } from '../ui';
import { DemoInventory } from '../storyplay/StorySurfaces';
import type {BoosterId} from '../storyplay/boosters';
import type { StoryPlay } from '../storyplay/model';
import './resourcePages.css';
export type ResourcePageName = 'resources'|'crystals'|'boosters';
export function ResourcePages({wallet,exchangeBusy=false,exchangeMessage='',onExchange=()=>{},onWalletRefresh=()=>{},preview=false,page,energy,state,activeStory,selectedBooster,onPage,onBack,onEnable,onDisable,onGrant,onBuy,onToggle,onChooseWorld}: {
 wallet?:EnergyWallet;exchangeBusy?:boolean;exchangeMessage?:string;onExchange?:()=>void;onWalletRefresh?:()=>void;
 preview?:boolean;page:ResourcePageName;energy?:number;state:StoryPlay;activeStory:boolean;selectedBooster?:BoosterId;
 onPage:(page:ResourcePageName)=>void;onBack:()=>void;onEnable:()=>void;onDisable:()=>void;onGrant:()=>void;onBuy:(id?:BoosterId)=>void;onToggle:(id?:BoosterId)=>void;onChooseWorld:()=>void;
}) {
 const tabsId=useId();
 const title={resources:'Энергия',crystals:'Кристаллы',boosters:'Бустеры'}[page];
 if(!import.meta.env.DEV||!preview){
  const releasePage=page==='boosters'?'resources':page;
  return <><ScreenHeading title="Ресурсы" back={onBack}/><section className="section resource-page resource-page--release">
   {!wallet&&exchangeMessage&&<p role="status">{exchangeMessage}</p>}
   <div className="resource-switch" role="tablist" aria-label="Ресурсы">{([['resources','Энергия'],['crystals','Кристаллы']] as const).map(([id,label],index)=><button key={id} id={`${tabsId}-${id}`} role="tab" aria-selected={releasePage===id} aria-controls={`${tabsId}-panel`} tabIndex={releasePage===id?0:-1} onClick={()=>onPage(id)} onKeyDown={event=>{if(!['ArrowLeft','ArrowRight','Home','End'].includes(event.key))return;event.preventDefault();const next=event.key==='Home'?0:event.key==='End'?1:1-index;onPage(next===0?'resources':'crystals');(event.currentTarget.parentElement?.children[next] as HTMLButtonElement)?.focus();}}>{label}</button>)}</div>
   <div id={`${tabsId}-panel`} role="tabpanel" aria-labelledby={`${tabsId}-${releasePage}`} tabIndex={0}>
   {wallet?<WalletView wallet={wallet} page={releasePage} busy={exchangeBusy} message={exchangeMessage} onExchange={onExchange} onRefresh={onWalletRefresh}/>:<>
    {releasePage==='resources'&&<><div className="resource-balance"><RewardArt kind="energy"/><div><strong>{energy??'—'}</strong><span>энергии доступно</span></div></div><h2>{energy===0?'Энергия закончилась':'Для следующих ходов'}</h2><p>Публикация и ответ расходуют по 1 энергии. Запас общий для всех миров. Читать ленту можно бесплатно.</p></>}
    <p>Баланс кошелька и обмен пока недоступны. Попробуй обновить баланс.</p><Button variant="ghost" disabled={exchangeBusy} onClick={onWalletRefresh}>Обновить баланс</Button>
   </>}
   </div>
  </section></>;
 }

 const walletView=wallet?<WalletView wallet={wallet} page={page==='crystals'?'crystals':'resources'} busy={exchangeBusy} message={exchangeMessage} onExchange={onExchange} onRefresh={onWalletRefresh}/>:undefined;
 const demo=activeStory&&state.enabled;
 return <><ScreenHeading title={title} back={onBack}/><section className="section resource-page">
  <nav className="resource-tabs" aria-label="Ресурсы">{([['resources','Энергия'],['crystals','Кристаллы'],['boosters','Бустеры']] as const).map(([id,label])=><Chip key={id} selected={page===id} onClick={()=>onPage(id)}>{label}</Chip>)}</nav>
  {wallet&&page!=='boosters'?walletView:page==='resources'?<>
   <div className="resource-balance"><span className="resource-symbol resource-symbol--energy" aria-hidden="true">ϟ</span><div><strong>{energy??'—'}</strong><span>энергии доступно</span></div></div>
   <h2>{energy===0?'Энергия закончилась':'Для твоих следующих ходов'}</h2><p>Публикация и ответ расходуют по 1 энергии. Запас общий для всех миров; чтение ленты бесплатно.</p>
   <div className="resource-explanation"><strong>Правило мира: максимум 10</strong><p>Новый кошелёк восстанавливает 1 энергию каждые 10 минут. Баланс кошелька пока недоступен в этой среде.</p></div>
   <article className="resource-product"><div><h3>10 энергии</h3><p>Предложение обмена на 3 кристалла. Начисление серверной энергии ещё не подключено.</p></div><strong>3 ◇</strong><Button disabled>Обменять на 10 энергии</Button></article><Button variant="secondary" onClick={()=>onPage('crystals')}>Кристаллы и обмен</Button>
  </>:page==='crystals'?<>
   <div className="resource-balance"><span className="resource-symbol resource-symbol--crystal" aria-hidden="true">◇</span><div><strong>{activeStory?state.crystals:'—'}</strong><span>{activeStory?'кристаллов в пробе':'баланс не подключён'}</span></div></div>
   {demo&&<p className="local-story-notice">Локальная проба. Тестовые кристаллы общие для аккаунта.</p>}
   <h2>На что обменять</h2><article className="resource-product"><div><h3>Удвоение XP за квест</h3><p>Бустер на одно действие</p></div><strong>8 ◇</strong><Button disabled={!demo||state.crystals<8} onClick={()=>onBuy('focus')}>Обменять на бустер</Button><button className="link-button" onClick={()=>onPage('boosters')}>Посмотреть инвентарь</button></article>
   <article className="resource-product"><div><h3>Энергия</h3><p>Обмен кристаллов на настоящую энергию пока не подключён.</p></div><Button disabled>Обмен недоступен</Button></article>
   {demo&&!state.bonusUsed&&<Button variant="ghost" onClick={onGrant}>Добавить 24 тестовых кристалла</Button>}
   {!demo&&<Button variant="secondary" onClick={()=>onPage('boosters')}>Открыть бустеры и пробу</Button>}
  </>:activeStory?<DemoInventory state={state} onEnable={onEnable} onDisable={onDisable} onGrant={onGrant} onBuy={onBuy} selected={selectedBooster} onSelect={id=>{onToggle(id);onBack();}}/>:<>
   <h2>Больше XP за квест</h2><article className="resource-product"><div><h3>Бустер ×2 XP</h3><p>Применяется к сюжетному квесту перед публикацией.</p></div><strong>8 ◇</strong></article>
   <p className="caption muted">Серверный инвентарь пока не подключён. Локальную пробу покупки и применения можно открыть в сюжетном мире.</p><Button onClick={onChooseWorld}>Выбрать сюжетный мир</Button>
  </>}
 </section></>;
}
