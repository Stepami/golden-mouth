import {useEffect,useState,useSyncExternalStore} from 'react';
import BackendApp from './BackendApp';
import {api} from '../api/client';
import {hasAccess,hasInitData,loadTelegramClientId,loginMiniApp,loginWidget,startAuth,subscribeAuth} from '../api/auth';
import {openTelegramLogin,preloadTelegramLogin} from '../api/telegramLogin';
import {TelegramLaunch} from '../app/TelegramLaunch';
import {inTelegram} from '../telegram';
import './auth.css';

/** Preserve the current master authentication flow before mounting API consumers. */
export default function AuthenticatedApp(){
 const authenticated=useSyncExternalStore(subscribeAuth,hasAccess);
 const [ready,setReady]=useState(false),[clientId,setClientId]=useState<number|null>(null),[busy,setBusy]=useState(false),[error,setError]=useState('');
 useEffect(()=>{void startAuth().catch(()=>setError('Не получилось войти. Попробуй ещё раз.')).finally(()=>setReady(true));},[]);
 useEffect(()=>{if(inTelegram&&hasInitData())return;void Promise.all([preloadTelegramLogin(),loadTelegramClientId()]).then(([,id])=>setClientId(id)).catch(()=>setError('Не удалось подготовить вход Telegram. Обнови страницу.'));},[]);
 useEffect(()=>{if(ready&&!authenticated&&api.hasPlayer())api.clearSession();},[ready,authenticated]);
 const login=()=>{if(!(inTelegram&&hasInitData())&&clientId===null)return;setError('');setBusy(true);if(inTelegram&&hasInitData())void loginMiniApp().catch(()=>setError('Не получилось войти. Попробуй ещё раз.')).finally(()=>setBusy(false));else openTelegramLogin(clientId!,token=>{void loginWidget(token).catch(()=>setError('Не получилось войти. Попробуй ещё раз.')).finally(()=>setBusy(false));},()=>{setBusy(false);setError('Вход через Telegram не завершён.');});};
 if(!ready)return <div className="v7-stage"><div className="app"><p className="section" role="status">Входим в игру…</p></div></div>;
 if(!authenticated)return <TelegramLaunch onLogin={login} busy={busy} ready={inTelegram&&hasInitData()||clientId!==null} error={error}/>;
 return <BackendApp/>;
}
