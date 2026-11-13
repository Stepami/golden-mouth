import {Avatar} from './index';

/** Compact notification preview; the full message belongs to the destination. */
export function ActivityItem({title,text,actorName,actorAvatar,symbol,read,onOpen,detail}:{title:string;text:string;actorName?:string;actorAvatar?:string;symbol:string;detail?:string;read:boolean;onOpen:()=>void}){
  return <article className={`activity-row${read?' is-read':''}`}><button className="activity-open" onClick={onOpen}>
    {actorName?<Avatar name={actorName} src={actorAvatar}/>:<span className="activity-symbol" aria-hidden="true">{symbol}</span>}
    <span className="activity-copy"><strong>{title}</strong><span className="activity-text">{text}</span>{detail&&<span className="activity-detail">{detail}</span>}</span>
    {!read&&<i className="activity-unread" aria-label="Не прочитано"/>}
  </button></article>;
}
