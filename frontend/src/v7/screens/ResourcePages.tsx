import {useId} from 'react';
import {WalletView} from '../energy/WalletView';
import {RewardArt} from '../ui/RewardArt';
import type {EnergyWallet} from '../energy/exchange';
import {Button, ScreenHeading} from '../ui';
import './resourcePages.css';
export type ResourcePageName = 'resources' | 'crystals';
export function ResourcePages({wallet, exchangeBusy=false, exchangeMessage='', onExchange=()=>{}, onWalletRefresh=()=>{}, page, energy, onPage, onBack}: {
  wallet?: EnergyWallet; exchangeBusy?: boolean; exchangeMessage?: string;
  onExchange?: ()=>void; onWalletRefresh?: ()=>void;
  page: ResourcePageName; energy?: number;
  onPage: (page: ResourcePageName)=>void; onBack: ()=>void;
}) {
  const tabsId=useId();
  const releasePage=page;
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
