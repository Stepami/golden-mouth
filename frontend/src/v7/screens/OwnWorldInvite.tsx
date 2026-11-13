import {Button,Progress} from '../ui';
import './ownWorldInvite.css';

export function OwnWorldInvite({unlocked,onInvite,onCreate,onCheck,checking,message}:{unlocked:boolean;onInvite:()=>void;onCreate:()=>void;onCheck:()=>void;checking:boolean;message:string}){
 return <section className="own-world-invite" aria-label="Создание своего мира">
  <h2>{unlocked?'Свой мир доступен':'Открой создание своего мира'}</h2>
  <p>{unlocked?'Придумай место, персонажей и правила своей истории.':'Пригласи 1 друга. Когда он опубликует первый пост, ты получишь 3 кристалла и доступ к созданию своего мира.'}</p>
  <div className="own-world-unlock"><span>Доступ к конструктору</span><strong>{unlocked?'1/1':'0/1'}</strong></div>
  <Progress value={unlocked?1:0} max={1} label="Доступ к созданию своего мира"/>
  <div className="own-world-invite-actions">{unlocked?<><Button onClick={onCreate}>Создать свой мир</Button><Button variant="ghost" onClick={onInvite}>Пригласить друга · 3 кристалла</Button></>:<><Button onClick={onInvite}>Пригласить друга</Button><Button variant="ghost" busy={checking} onClick={onCheck}>Проверить доступ</Button></>}</div>
  {message&&!unlocked&&<p className="caption" role="status">{message}</p>}
 </section>;
}
