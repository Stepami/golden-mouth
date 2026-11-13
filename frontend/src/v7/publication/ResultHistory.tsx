import {EmptyState} from '../ui';
import {ResultIsland} from './ResultIsland';
import type {SavedResult} from './savedResults';
import './resultHistory.css';

export function ResultHistory({items,onOpen,onNewPost,onShare}:{items:SavedResult[];onOpen:(postId:string)=>void;onNewPost?:()=>void;onShare?:(postId:string,text?:string)=>void}) {
  return <section className="result-history" aria-label="История результатов">
    <p className="caption muted result-history-note">История на этом устройстве</p>
    {!items.length&&<EmptyState title="Результаты появятся после отправки">Здесь останутся итоги твоих постов и комментариев.</EmptyState>}
    {items.map(item=><article className="result-history-entry" key={item.id} data-action-id={item.id}>
      <header><strong>{item.receipt.actionKind==='comment'?'Комментарий':'Пост'}</strong>{item.createdAt?<time dateTime={item.createdAt}>{new Intl.DateTimeFormat('ru',{day:'numeric',month:'short',hour:'2-digit',minute:'2-digit'}).format(new Date(item.createdAt))}</time>:<span>Время не сохранено</span>}</header>
      <p className="result-history-excerpt">{item.text??'Текст этого хода не сохранён'}</p>
      <ResultIsland archive receipt={item.receipt} onRead={()=>onOpen(item.receipt.result.postId)} onNewPost={onNewPost} onShare={onShare?()=>onShare(item.receipt.result.postId,item.text):undefined}/>
    </article>)}
  </section>;
}
