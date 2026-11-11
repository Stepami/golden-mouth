import {EmptyState} from '../ui';
import {PublicationResult} from './PublicationCards';
import type {SavedResult} from './savedResults';
import './resultHistory.css';

export function ResultHistory({items,onOpen}:{items:SavedResult[];onOpen:(postId:string)=>void}) {
  return <section className="result-history" aria-label="История результатов">
    <p className="caption muted result-history-note">История на этом устройстве</p>
    {!items.length&&<EmptyState title="Результаты появятся после отправки">Здесь останутся итоги твоих постов и комментариев.</EmptyState>}
    {items.map(item=><article className="result-history-entry" key={item.id} data-action-id={item.id}>
      <header><strong>{item.receipt.actionKind==='comment'?'Комментарий':'Пост'}</strong>{item.createdAt?<time dateTime={item.createdAt}>{new Intl.DateTimeFormat('ru',{day:'numeric',month:'short',hour:'2-digit',minute:'2-digit'}).format(new Date(item.createdAt))}</time>:<span>Время не сохранено</span>}</header>
      <p className="result-history-excerpt">{item.text??'Текст этого хода не сохранён'}</p>
      <PublicationResult receipt={item.receipt} onRead={()=>onOpen(item.receipt.result.postId)}/>
    </article>)}
  </section>;
}
