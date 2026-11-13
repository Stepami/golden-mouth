import {useEffect,useState} from 'react';
import {shareStory,shareURL} from '@tma.js/sdk';
import {api} from '../../api/client';
import {event} from '../../app/analytics';
import {apiBaseUrl} from '../../app/runtimeConfig';
import {telegramStartUrl} from '../../app/telegramLinks';
import {inTelegram} from '../../telegram';
import {Button,Dialog,Notice,TextField} from '../ui';

export function SharePost({playerId,postId,text,onClose}:{playerId:string;postId:string;text?:string;onClose:()=>void}){
 const [link,setLink]=useState(''),[error,setError]=useState(''),[attempt,setAttempt]=useState(0);
 useEffect(()=>{let active=true;setError('');void api.referral().then(({code})=>{if(active)setLink(telegramStartUrl(code));}).catch(()=>{if(active)setError('Не удалось получить ссылку.');});return()=>{active=false;};},[playerId,attempt]);
 const shareText=text?`${text}\n\nМой пост в Реках`:'Мой пост в Реках';
 const share=async()=>{event('share',{to:'chat'});try{if(inTelegram)shareURL.ifAvailable(link,shareText);else if(navigator.share)await navigator.share({title:'Реки',text:shareText,url:link});else window.open(`https://t.me/share/url?url=${encodeURIComponent(link)}&text=${encodeURIComponent(shareText)}`,'_blank','noopener,noreferrer');}catch(cause){if(!(cause instanceof DOMException&&cause.name==='AbortError'))setError('Не получилось открыть отправку. Можно скопировать ссылку.');}};
 const story=()=>{event('share',{to:'story'});const url=new URL(`${apiBaseUrl}/cards/story/${encodeURIComponent(playerId)}/${encodeURIComponent(postId)}.png`,location.origin).href;shareStory.ifAvailable(url,{text:shareText,widgetLink:{url:link,name:'Играть'}});};
 return <Dialog title="Поделиться постом" onClose={onClose}><div className="stack">{text&&<p className="share-post-text">{text}</p>}{link?<TextField label="Ссылка на игру" value={link} readOnly onFocus={e=>e.target.select()}/>:<p role="status">Загружаем ссылку…</p>}{error&&<Notice title="Поделиться" tone="error">{error}</Notice>}{!link&&error&&<Button onClick={()=>setAttempt(v=>v+1)}>Повторить</Button>}<Button disabled={!link} onClick={()=>void share()}>Отправить</Button>{inTelegram&&shareStory.isAvailable()&&<Button variant="secondary" disabled={!link} onClick={story}>В сторис</Button>}<Button variant="ghost" disabled={!link} onClick={()=>void navigator.clipboard.writeText(link).catch(()=>setError('Не удалось скопировать. Выдели ссылку в поле.'))}>Скопировать ссылку</Button></div></Dialog>;
}
