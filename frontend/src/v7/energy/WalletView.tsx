import {useEffect,useState} from 'react';
import {Button} from '../ui';
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
   <div className="wallet-amount"><span>{energy?'Энергия сейчас':'Кристаллы на аккаунте'}</span><strong>{energy?wallet.amount:wallet.crystals}{energy&&<small> / {wallet.cap}</small>}</strong><p>{energy?'Для публикаций и ответов':'Для пополнения энергии'}</p></div>
   <RewardArt kind={energy?'energy':'crystal'}/>
  </div>
  {energy?<div className="wallet-recovery">
   <div className="wallet-meter" role="progressbar" aria-label="Запас энергии" aria-valuemin={0} aria-valuemax={wallet.cap} aria-valuenow={wallet.amount}>{Array.from({length:wallet.cap},(_,i)=><span key={i} className={i<wallet.amount?'is-filled':''}/>)}</div>
   <div className="wallet-recovery-time"><span>Восстановление</span><EnergyCountdown wallet={wallet}/></div>
   <dl className="wallet-rules"><div><dt>Публикация или ответ</dt><dd>1 энергия</dd></div><div><dt>Восстанавливается</dt><dd>1 за {wallet.regenSeconds/60} мин</dd></div></dl>
   <p className="wallet-note">Запас общий для всех миров. Читать ленту можно бесплатно.</p>
  </div>:<div className="wallet-crystal-info"><h2>На что потратить</h2><p>Обменивай кристаллы на энергию, чтобы продолжать публиковать и отвечать персонажам.</p></div>}
  <div className="wallet-exchange">
   <div className="wallet-offer"><RewardArt kind={energy?'crystal':'energy'}/><div><h2>{energy?'Пополнить запас':'Обменять на энергию'}</h2><p>До {quote.targetEnergy} энергии · {quote.crystalCost} кристалла</p></div></div>
   <Button busy={busy} disabled={!quote.available} onClick={onExchange}>{action}</Button>
   <p className="wallet-note">{energy?`На аккаунте: ${wallet.crystals} кристаллов`:quote.available?`После обмена останется: ${wallet.crystals-quote.crystalCost}`:'Кристаллы останутся на аккаунте.'}</p>
  </div>
  {message&&<p className="wallet-message" role="status">{message}</p>}
  <button className="wallet-refresh" disabled={busy} onClick={onRefresh}>Обновить баланс</button>
 </section>;
}
