import {useEffect,useId,useRef,useState} from 'react'
import {Button,Card,Sheet} from '../../ui'
import {inTelegram} from '../../telegram'
import {dismissInstall,publishedPostFor,requestInstall,useInstall} from './state'
import './install.css'

export function InstallApp({entry,playerId}:{entry:'settings'|'banner';playerId?:string}){
 const install=useInstall()
 const [open,setOpen]=useState(false),[busy,setBusy]=useState(false),[message,setMessage]=useState('')
 const dialog=useRef<HTMLDivElement>(null),heading=useRef<HTMLHeadingElement>(null),titleId=useId()
 const ios=/iPad|iPhone|iPod/.test(navigator.userAgent)||(navigator.platform==='MacIntel'&&navigator.maxTouchPoints>1)
 const android=/Android/.test(navigator.userAgent)
 const safari=/Safari/.test(navigator.userAgent)&&!/(Chrome|Chromium|CriOS|Edg|OPR)/.test(navigator.userAgent)
 const embedded=inTelegram||/FBAN|FBAV|Instagram|\bVK\b|; wv\)|Telegram/i.test(navigator.userAgent)
 const close=()=>{if(busy)return;setOpen(false);if(entry==='banner')dismissInstall()}
 useEffect(()=>{
  if(!open)return
  const previous=document.activeElement as HTMLElement|null
  heading.current?.focus()
  const key=(event:KeyboardEvent)=>{
   if(event.key==='Escape'){event.preventDefault();close()}
   if(event.key==='Tab'){
    const items=dialog.current?.querySelectorAll<HTMLElement>('button:not(:disabled),input,a[href]')
    if(!items?.length)return
    const first=items[0],last=items[items.length-1]
    if(event.shiftKey&&(document.activeElement===first||document.activeElement===heading.current)){event.preventDefault();last.focus()}
    else if(!event.shiftKey&&document.activeElement===last){event.preventDefault();first.focus()}
   }
  }
  document.addEventListener('keydown',key)
  return()=>{document.removeEventListener('keydown',key);previous?.focus()}
 },[open,busy])
 const start=async()=>{
  setMessage('')
  if(!install.prompt||embedded){setOpen(true);return}
  setBusy(true)
  try{
   const outcome=await requestInstall()
   if(outcome==='accepted'){if(entry==='banner')dismissInstall();setMessage('Установка запрошена. Заверши её в окне браузера.');setOpen(true)}
   else if(outcome==='dismissed'&&entry==='banner')dismissInstall()
  }catch{setMessage('Не удалось открыть установку. Попробуй через меню браузера.');setOpen(true)}
  finally{setBusy(false)}
 }
 if(install.installed)return null
 if(entry==='banner'&&(!publishedPostFor(playerId)||install.dismissed)&&!open)return null
 return <>
  {entry==='settings'?<button type="button" className="install-settings" onClick={()=>void start()} disabled={busy}><span>Установить приложение</span><span aria-hidden="true">›</span></button>:<Card style={{marginBottom:12}}><div className="install-banner-heading"><img src="/android-chrome-192x192.png?v=wave-2" width={36} height={36} alt=""/><strong>Добавить «Реки» на экран</strong><button type="button" aria-label="Закрыть предложение установки" onClick={dismissInstall} disabled={busy}>×</button></div><p className="small muted">Значок «Рек» появится рядом с другими приложениями.</p><Button disabled={busy} onClick={()=>void start()}>{busy?'Открываем установку…':'Установить приложение'}</Button></Card>}
  {open&&<Sheet onClose={close}><div ref={dialog} role="dialog" aria-modal="true" aria-labelledby={titleId} className="install-dialog">
   <h2 ref={heading} id={titleId} tabIndex={-1}>Установить «Реки»</h2>
   {embedded?<><p>Открой игру {ios?'в Safari':android?'в Chrome':'в обычном браузере'}.</p>{(ios||android)&&<p>Нажми ⋯ вверху окна и выбери «Открыть в браузере».</p>}<p className="small muted">{ios||android?'Нет такого пункта? Скопируй адрес ниже и вставь его в браузер.':'Скопируй адрес ниже и вставь его в Chrome, Edge или Safari.'}</p></>:ios?<ol><li>В Safari нажми «Поделиться».</li><li>Выбери «На экран “Домой”».</li><li>Нажми «Добавить».</li></ol>:android?<ol><li>В Chrome нажми ⋮.</li><li>Выбери «Установить приложение» или «Добавить на главный экран».</li></ol>:safari?<p>В меню Safari выбери «Файл» → «Добавить в Dock».</p>:<p>В меню браузера выбери «Установить приложение». В Edge этот пункт находится в разделе «Приложения».</p>}
   {install.prompt&&!embedded&&<Button disabled={busy} onClick={()=>void start()}>{busy?'Открываем установку…':'Открыть установку браузера'}</Button>}
   {embedded&&<><label className="small muted" htmlFor={`${titleId}-url`}>Адрес игры</label><input id={`${titleId}-url`} className="install-address" readOnly value={`${location.origin}/`} onFocus={e=>e.target.select()}/>{navigator.clipboard&&<Button secondary onClick={()=>void navigator.clipboard.writeText(`${location.origin}/`).then(()=>setMessage('Адрес скопирован')).catch(()=>setMessage('Выдели и скопируй адрес из поля выше.'))}>Скопировать адрес</Button>}</>}
   {message&&<p role="status" className="small">{message}</p>}
   <Button secondary disabled={busy} onClick={close}>Понятно</Button>
  </div></Sheet>}
 </>
}
