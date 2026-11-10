import {useEffect,useState,useSyncExternalStore} from 'react';
import BackendApp from './BackendApp';
import {hasAccess,needsRegistration,startAuth,subscribeAuth,hasInitData,loginMiniApp,loginWidget,loginVk,loginYandex} from '../api/auth';
import {AuthProviders} from '../app/AuthProviders';
import {TelegramLaunch} from '../app/TelegramLaunch';
export default function AuthenticatedApp() {
 const authenticated=useSyncExternalStore(subscribeAuth,hasAccess);
 const registration=useSyncExternalStore(subscribeAuth,needsRegistration);
 const [ready,setReady]=useState(false);
 const [busy,setBusy]=useState(false);
 const [error,setError]=useState('');
 useEffect(()=>{void startAuth().catch(()=>setError('Не удалось войти')).finally(()=>setReady(true));},[]);
 const finish=(request:Promise<unknown>)=>{setBusy(true);void request.catch(()=>setError('Не удалось войти')).finally(()=>setBusy(false));};
 if(!ready)return <div className="loading-state">Входим в игру</div>;
 if(!authenticated&&!registration)return <TelegramLaunch error={error}><AuthProviders busy={busy} onMiniApp={hasInitData()?()=>finish(loginMiniApp()):undefined} onTelegram={token=>finish(loginWidget(token))} onVk={(token,expiry)=>finish(loginVk(token,expiry))} onYandex={(token,expiry)=>finish(loginYandex(token,expiry))} onError={()=>setError('Вход не завершён')}/></TelegramLaunch>;
 return <BackendApp/>;
}
