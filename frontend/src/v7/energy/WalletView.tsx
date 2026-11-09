import {useEffect,useState} from 'react';
import {Button,Icon} from '../ui';
import {RewardArt} from '../ui/RewardArt';
import type {EnergyWallet} from './exchange';
export function EnergyCountdown({wallet}:{wallet:EnergyWallet}){
 const [now,setNow]=useState(Date.now());useEffect(()=>{const timer=setInterval(()=>setNow(Date.now()),1000);return()=>clearInterval(timer);},[]);
 if(wallet.amount>=wallet.cap)return <span>Запас полный</span>;
 if(!wallet.nextEnergyAtUtc)return <span>Восстанавливается</span>;
 const seconds=Math.max(0,Math.ceil((Date.parse(wallet.nextEnergyAtUtc)-now)/1000));
 return <span>{seconds?`+1 через ${Math.floor(seconds/60)}:${String(seconds%60).padStart(2,'0')}`:'Обновляем запас…'}</span>;
}
export function WalletView({wallet,page='resources',busy,message,onExchange,onRefresh}:{wallet:EnergyWallet;page?:'resources'|'crystals';busy:boolean;message:string;onExchange:()=>void;onRefresh:()=>void}){
 const energy=page==='resources';
 const quote=wallet.exchangeQuote;
 const action=quote.unavailableReason==='energy_full'?'Запас полный':quote.unavailableReason==='insufficient_crystals'?'Недостаточно кристаллов':`Пополнить за ${quote.crystalCost} кристалла`;
 return <section className={`energy-wallet energy-wallet--${page}`} aria-label={energy?'Запас энергии':'Баланс кристаллов'}>
  <div className="wallet-hero">
   <div className="wallet-amount"><strong>{energy?wallet.amount:wallet.crystals}{energy&&<small> / {wallet.cap}</small>}</strong></div>
   <RewardArt kind={energy?'energy':'crystal'}/>
  </div>
  {energy?<div className="wallet-recovery">
   <div className="wallet-meter" role="progressbar" aria-label="Запас энергии" aria-valuemin={0} aria-valuemax={wallet.cap} aria-valuenow={wallet.amount}>{Array.from({length:wallet.cap},(_,i)=><span key={i} className={i<wallet.amount?'is-filled':''}/>)}</div>
   {wallet.amount<wallet.cap&&<div className="wallet-recovery-time"><span>Восстановление</span><EnergyCountdown wallet={wallet}/></div>}
   <dl className="wallet-rules"><div><dt>Публикация или ответ</dt><dd>1 энергия</dd></div><div><dt>Восстанавливается</dt><dd>1 за {wallet.regenSeconds/60} мин</dd></div></dl>
   <p className="wallet-note">Запас общий для всех миров. Читать ленту можно бесплатно.</p>
  </div>:null}
  <div className="wallet-exchange">
   <div className="wallet-offer"><RewardArt kind={energy?'crystal':'energy'}/><div><h2>{energy?'Пополнить запас':'Обменять на энергию'}</h2><p>До {quote.targetEnergy} энергии · {quote.crystalCost} кристалла</p></div></div>
   <Button busy={busy} disabled={!quote.available} onClick={onExchange}>{action}</Button>
   {(energy||quote.available)&&<p className="wallet-note">{energy?`На аккаунте: ${wallet.crystals} кристаллов`:`После обмена останется: ${wallet.crystals-quote.crystalCost}`}</p>}
  </div>
  {message&&<p className="wallet-message" role="status">{message}</p>}
  <button className="wallet-refresh" disabled={busy} onClick={onRefresh}>Обновить баланс</button>
 </section>;
}

export function EnergyBadge({wallet,amount,onClick}:{wallet?:EnergyWallet;amount?:number;onClick:()=>void}){
 const [now,setNow]=useState(Date.now());
 useEffect(()=>{const timer=setInterval(()=>setNow(Date.now()),1000);return()=>clearInterval(timer);},[]);
 const full=wallet&&wallet.amount>=wallet.cap;
 const remaining=wallet?.nextEnergyAtUtc?Date.parse(wallet.nextEnergyAtUtc)-now:undefined;
 const fill=full?1:remaining!==undefined&&Number.isFinite(remaining)&&wallet?Math.min(1,Math.max(0,1-remaining/(wallet.regenSeconds*1000))):0;
 const value=wallet?`${wallet.amount}/${wallet.cap}`:String(amount??'—');
 return <><button className="resource energy-ring-button" aria-label={`Энергия: ${value}${wallet?`. ${full?'Запас полный':'Восстанавливается'}`:''}`} style={{background:`conic-gradient(var(--campaign-ring) ${fill*100}%,var(--color-divider) 0)`}} onClick={onClick}><span className="resource-ring-face"><Icon name="energy"/><b>{value}</b></span></button>{wallet&&!full&&<EnergyCountdown wallet={wallet}/>}</>;
}
