import {useEffect, useState} from 'react';
import {api} from '../../api/client';
import {event} from '../../app/analytics';
import {telegramStartUrl} from '../../app/telegramLinks';
import {Button, Dialog, Notice, TextField} from '../ui';

export function InviteFriend({onClose}:{onClose:()=>void}) {
  const [link,setLink]=useState(''),[error,setError]=useState(''),[copied,setCopied]=useState(false),[attempt,setAttempt]=useState(0);
  useEffect(()=>{let active=true;setError('');void api.referral().then(({code})=>{if(active)setLink(telegramStartUrl(code));}).catch(()=>{if(active)setError('Не удалось получить ссылку.');});return()=>{active=false;};},[attempt]);
  const copy=async()=>{try{await navigator.clipboard.writeText(link);setCopied(true);setError('');}catch{setError('Не удалось скопировать. Можно выделить ссылку ниже.');}};
  const share=async()=>{event('invite_sent',{to:'chat'});if(navigator.share){try{await navigator.share({title:'Реки',text:'Сыграем в Реки?',url:link});}catch(e){if(!(e instanceof DOMException&&e.name==='AbortError'))setError('Не удалось отправить. Можно скопировать ссылку.');}}else window.open(`https://t.me/share/url?url=${encodeURIComponent(link)}&text=${encodeURIComponent('Сыграем в Реки?')}`,'_blank','noopener,noreferrer');};
  return <Dialog title="Пригласить друга" onClose={onClose}><div className="stack invite-friend">
    <p>Отправь свою ссылку. Когда друг опубликует первый пост, ты получишь 3 кристалла.</p>
    {link?<TextField label="Твоя ссылка" value={link} readOnly onFocus={e=>e.target.select()}/>:<p role="status">Загружаем ссылку…</p>}
    {error&&<Notice title="Ссылка для друга" tone="error">{error}</Notice>}
    {!link&&error&&<Button onClick={()=>setAttempt(n=>n+1)}>Повторить</Button>}
    <Button disabled={!link} onClick={()=>void share()}>Отправить</Button>
    <Button variant="secondary" disabled={!link} onClick={()=>void copy()}>{copied?'Ссылка скопирована':'Скопировать'}</Button>
  </div></Dialog>;
}
