import {Button} from '../ui';
import {boosterCatalog,type Inventory} from './boosters';
import {moscowDay} from './gifts';
export function SeriesGiftCard({inventory,onClaim}:{inventory:Inventory;onClaim:()=>void}) {
 const claimed=inventory.lastGift?.day===moscowDay(),gift=claimed?inventory.lastGift!.gift:undefined;
 const description=gift?.kind==='booster'?`${boosterCatalog.find(b=>b.id===gift.boosterId)!.name}: бустер XP`:gift?.kind==='crystals'?`+${gift.amount} кристалл` : gift?.kind==='energy'?`Запас энергии: +${gift.amount}`:'';
 return <section className="series-gift-card"><span className="series-gift-art" aria-hidden="true">✦</span><div><h3>{claimed?'Подарок открыт':'Подарок за возвращение'}</h3><p>{claimed?description:'Каждый день серии — случайный подарок.'}</p><small>Локальная проба · один раз в сутки</small></div>{!claimed&&<Button onClick={onClaim}>Открыть подарок</Button>}{gift?.kind==='energy'&&<small className="gift-energy-note">Подарок сохранён в «Бустеры → Запас энергии». Применение к серверной энергии ещё не подключено.</small>}</section>;
}
