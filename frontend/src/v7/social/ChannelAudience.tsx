import {Icon} from '../ui';
import {formatAudience} from './audience';
export {formatAudience,channelSubscribers} from './audience';
export function ChannelAudience({value}:{value?:number}) {
 return <div className="channel-audience"><span><Icon name="member"/>Подписчики канала</span><strong>{value===undefined?'—':formatAudience(value)}</strong>{value===undefined&&<small>Статистика канала недоступна</small>}</div>;
}
