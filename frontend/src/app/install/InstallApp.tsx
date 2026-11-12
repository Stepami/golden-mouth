import {createPortal} from 'react-dom'
import {useEffect,useId,useRef,useState} from 'react'
import {Button,Card,Sheet} from '../../ui'
import {inTelegram} from '../../telegram'
import {dismissInstall,requestInstall,useInstall} from './state'
import './install.css'

function InstallSymbol({kind}:{kind:'share'|'add'|'menu'}){
 return <svg viewBox="0 0 24 24" width="28" height="28" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">{kind==='share'?<><path d="M12 15V3m-4 4 4-4 4 4"/><path d="M7 10H5v11h14V10h-2"/></>:kind==='add'?<><rect x="4" y="4" width="16" height="16" rx="3"/><path d="M12 8v8m-4-4h8"/></>:<><circle cx="12" cy="5" r="1" fill="currentColor"/><circle cx="12" cy="12" r="1" fill="currentColor"/><circle cx="12" cy="19" r="1" fill="currentColor"/></>}</svg>
}

export function InstallApp({entry}:{entry:'settings'|'banner'}){
 const install=useInstall()
 const [preview,setPreview]=useState(()=>import.meta.env.DEV&&new URLSearchParams(location.search).get('preview')==='install')
 const dismiss=()=>{dismissInstall();setPreview(false)}
 const [open,setOpen]=useState(false),[busy,setBusy]=useState(false),[message,setMessage]=useState('')
 const dialog=useRef<HTMLDivElement>(null),heading=useRef<HTMLHeadingElement>(null),titleId=useId()
 const ios=/iPad|iPhone|iPod/.test(navigator.userAgent)||(navigator.platform==='MacIntel'&&navigator.maxTouchPoints>1)
 const android=/Android/.test(navigator.userAgent)
 const yandex=/YaBrowser/i.test(navigator.userAgent)
 const chrome=/Chrome|Chromium|CriOS/i.test(navigator.userAgent)&&!yandex&&!/Edg|OPR|SamsungBrowser/i.test(navigator.userAgent)
 const safari=/Safari/.test(navigator.userAgent)&&!/(Chrome|Chromium|CriOS|Edg|OPR|FxiOS|YaBrowser)/.test(navigator.userAgent)
 const embedded=inTelegram||/FBAN|FBAV|Instagram|\bVK\b|; wv\)|Telegram/i.test(navigator.userAgent)
 const close=()=>{if(busy)return;setOpen(false);if(entry==='banner')dismiss()}
 useEffect(()=>{
  if(!open)return
  const previous=document.activeElement as HTMLElement|null
  heading.current?.focus({preventScroll:true})
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
  return()=>{document.removeEventListener('keydown',key);previous?.focus({preventScroll:true})}
 },[open,busy])
 const start=async()=>{
  setMessage('')
  if(!install.prompt||embedded){setOpen(true);return}
  setBusy(true)
  try{
   const outcome=await requestInstall()
   if(outcome==='accepted'){if(entry==='banner')dismiss();setMessage('Установка запрошена. Заверши её в окне браузера.');setOpen(true)}
   else if(outcome==='dismissed'&&entry==='banner')dismiss()
  }catch{setMessage('Не удалось открыть установку. Попробуй через меню браузера.');setOpen(true)}
  finally{setBusy(false)}
 }
 if(install.installed&&!preview)return null
 if(entry==='banner'&&install.dismissed&&!open&&!preview)return null
 return <>
  {entry==='settings'?<button type="button" className="install-settings" onClick={()=>void start()} disabled={busy}><span>Установить приложение</span><span aria-hidden="true">›</span></button>:<Card style={{marginBottom:12}}><div className="browser-offer install-offer">{preview&&<p className="small muted" role="status">DEV-просмотр. {install.installed?'Обычный показ скрыт: режим приложения.':install.dismissed?'Обычный показ скрыт: предложение уже закрывали.':'Карточка доступна и без предпросмотра.'}</p>}<div className="install-banner-heading"><img src="/android-chrome-192x192.png?v=wave-2" width={32} height={32} alt=""/><div className="browser-offer-copy"><strong>Добавить «Реки» на экран</strong><p className="small muted">Значок «Рек» появится рядом с другими приложениями.</p></div><button type="button" aria-label="Закрыть предложение установки" onClick={dismiss} disabled={busy}>×</button></div><div className="browser-offer-actions"><Button secondary disabled={busy} onClick={()=>void start()}>{busy?'Открываем…':'Установить'}</Button></div></div></Card>}
  {open&&createPortal(<Sheet onClose={close}><div ref={dialog} role="dialog" aria-modal="true" aria-labelledby={titleId} className="install-dialog">
   <h2 ref={heading} id={titleId} tabIndex={-1}>Установить «Реки»</h2>
   {embedded?<><p>Открой игру {ios||android?'в браузере телефона':'в обычном браузере'}.</p>{(ios||android)&&<p>Открой меню окна и выбери «Открыть в браузере».</p>}<p className="small muted">Скопируй адрес ниже, если такого пункта нет.</p></>:ios?<><ol className="install-steps"><li><span className="install-step-symbol"><InstallSymbol kind={yandex?'menu':'share'}/></span><span>{yandex?<>В меню Яндекс Браузера выбери <strong>«Добавить ярлык на телефон»</strong>.</>:<>В {chrome?'Chrome':safari?'Safari':'браузере'} нажми <strong>«Поделиться»</strong>.</>}</span></li><li><span className="install-step-symbol"><InstallSymbol kind="add"/></span><span>Выбери <strong>{yandex?'«Добавить на экран “Домой”»':chrome?'«Добавить на главный экран»':'«На экран “Домой”»'}</strong>.</span></li><li><span className="install-step-label" aria-hidden="true">Добавить</span><span>Нажми <strong>«Добавить»</strong>.</span></li></ol>{yandex&&<p className="small muted">Ярлык будет открывать игру в Яндекс Браузере.</p>}</>:android?<><ol className="install-steps"><li><span className="install-step-symbol"><InstallSymbol kind="menu"/></span><span>Открой меню {yandex?'Яндекс Браузера':chrome?'Chrome':'браузера'}.</span></li><li><span className="install-step-symbol"><InstallSymbol kind="add"/></span><span>{yandex?<>Выбери <strong>«Добавить ярлык на рабочий стол»</strong>, затем «Добавить».</>:<>Выбери <strong>«Установить приложение»</strong> или <strong>«Добавить на главный экран»</strong>.</>}</span></li></ol>{yandex&&<p className="small muted">Ярлык будет открывать игру в Яндекс Браузере.</p>}</>:safari?<p>В меню Safari выбери «Файл» → «Добавить в Dock».</p>:<p>В меню браузера выбери «Установить приложение». В Edge этот пункт находится в разделе «Приложения».</p>}
   {install.prompt&&!embedded&&<Button disabled={busy} onClick={()=>void start()}>{busy?'Открываем установку…':'Открыть установку браузера'}</Button>}
   {embedded&&<><label className="small muted" htmlFor={`${titleId}-url`}>Адрес игры</label><input id={`${titleId}-url`} className="install-address" readOnly value={`${location.origin}/`} onFocus={e=>e.target.select()}/>{navigator.clipboard&&<Button secondary onClick={()=>void navigator.clipboard.writeText(`${location.origin}/`).then(()=>setMessage('Адрес скопирован')).catch(()=>setMessage('Выдели и скопируй адрес из поля выше.'))}>Скопировать адрес</Button>}</>}
   {message&&<p role="status" className="small">{message}</p>}
   <Button secondary disabled={busy} onClick={close}>Понятно</Button>
  </div></Sheet>,document.body)}
 </>
}
