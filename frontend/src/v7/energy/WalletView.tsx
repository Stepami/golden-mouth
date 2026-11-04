import {useEffect,useState} from 'react';
import {Button} from '../ui';
import type {EnergyWallet} from './exchange';
export function EnergyCountdown({wallet}:{wallet:EnergyWallet}){
 const [now,setNow]=useState(Date.now());useEffect(()=>{const timer=setInterval(()=>setNow(Date.now()),1000);return()=>clearInterval(timer);},[]);
 if(wallet.amount>=wallet.cap)return <span>Запас полный</span>;
 if(!wallet.nextEnergyAtUtc)return <span>Восстанавливается</span>;
 const seconds=Math.max(0,Math.ceil((Date.parse(wallet.nextEnergyAtUtc)-now)/1000));
 return <span>{seconds?`+1 через ${Math.floor(seconds/60)}:${String(seconds%60).padStart(2,'0')}`:'Обновляем запас…'}</span>;
}
export function WalletView({wallet,busy,message,onExchange,onRefresh}:{wallet:EnergyWallet;busy:boolean;message:string;onExchange:()=>void;onRefresh:()=>void}){
 return <section className="resource-product"><div className="row between"><h3>Энергия {wallet.amount}/{wallet.cap}</h3><strong>◇ {wallet.crystals}</strong></div><p><EnergyCountdown wallet={wallet}/></p><p>Заполнить запас до {wallet.exchangeQuote.targetEnergy} за {wallet.exchangeQuote.crystalCost} кристалла.</p><Button busy={busy} disabled={!wallet.exchangeQuote.available} onClick={onExchange}>{wallet.exchangeQuote.unavailableReason==='energy_full'?'Запас полный':wallet.exchangeQuote.unavailableReason==='insufficient_crystals'?'Недостаточно кристаллов':'Обменять кристаллы на энергию'}</Button>{message&&<p role="status">{message}</p>}<Button variant="ghost" disabled={busy} onClick={onRefresh}>Обновить баланс</Button></section>;
}
