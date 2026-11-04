import {useEffect,useState,useSyncExternalStore} from 'react';
import BackendApp from './BackendApp';
import {api} from '../api/client';
import {canResumeAuth,clearPendingWidgetLogin,hasAccess,hasInitData,hasPendingWidgetLogin,loadTelegramClientId,loginMiniApp,loginWidget,startAuth,subscribeAuth} from '../api/auth';
import {openTelegramLogin,preloadTelegramLogin} from '../api/telegramLogin';
import {TelegramLaunch} from '../app/TelegramLaunch';
import {inTelegram} from '../telegram';
import './auth.css';
// Keep retry/resume behavior in sync with master auth (#237/#238).
export default function AuthenticatedApp() {
  const authenticated = useSyncExternalStore(subscribeAuth, hasAccess)
  const pendingLogin = useSyncExternalStore(subscribeAuth, hasPendingWidgetLogin)
  const resumable = useSyncExternalStore(subscribeAuth, canResumeAuth)
  const [ready, setReady] = useState(false)
  const [telegramClientId, setTelegramClientId] = useState<number | null>(null)
  const [prepareAttempt, setPrepareAttempt] = useState(0)
  const [prepareFailed, setPrepareFailed] = useState(false)
  const [busy, setBusy] = useState(false)
  const [authError, setAuthError] = useState('')
  const explicitStart = false
  useEffect(() => {
    if (explicitStart) return
    void startAuth().catch(() => { setAuthError('Нет соединения с игрой. Проверь подключение и продолжи вход.') }).finally(() => setReady(true))
  }, [explicitStart])
  useEffect(() => {
    if (!ready || authenticated || resumable || inTelegram && hasInitData() || explicitStart) return
    let active = true
    setPrepareFailed(false)
    void Promise.all([preloadTelegramLogin(), loadTelegramClientId()]).then(([, clientId]) => {
      if (active) setTelegramClientId(clientId)
    }).catch(() => {
      if (active) { setPrepareFailed(true); setAuthError('Не удалось подготовить вход Telegram. Проверь подключение и попробуй снова.') }
    })
    return () => { active = false }
  }, [ready, authenticated, resumable, explicitStart, prepareAttempt])
  useEffect(() => { if (ready && !authenticated && api.hasPlayer()) api.clearSession() }, [ready, authenticated])
  const finishLogin = (request: Promise<unknown>) => {
    void request.catch(() => {
      setAuthError(hasPendingWidgetLogin()
        ? 'Подтверждение Telegram сохранено. Проверь соединение с игрой и продолжи вход.'
        : 'Не получилось войти. Проверь подключение и попробуй снова.')
    }).finally(() => setBusy(false))
  }
  const onLogin = () => {
    if (resumable) {
      setAuthError(''); setBusy(true)
      finishLogin(startAuth())
      return
    }
    if (prepareFailed) {
      setAuthError(''); setTelegramClientId(null); setPrepareAttempt(value => value + 1)
      return
    }
    if (!(inTelegram && hasInitData()) && telegramClientId === null) return
    setAuthError('')
    setBusy(true)
    if (inTelegram && hasInitData()) {
      finishLogin(loginMiniApp())
    } else {
      openTelegramLogin(telegramClientId!,
        idToken => finishLogin(loginWidget(idToken)),
        () => { setBusy(false); setAuthError('Вход через Telegram не завершён.') },
      )
    }
  }
  const restartLogin = () => { clearPendingWidgetLogin(); setAuthError('') }
  if (!explicitStart && !ready) return <div className="app"><div className="screen"><div className="pad" role="status">Входим в игру…</div></div></div>
  if (!explicitStart && !authenticated) return <TelegramLaunch onLogin={onLogin} onRestart={restartLogin} pending={pendingLogin} resumable={resumable} prepareFailed={prepareFailed} busy={busy} ready={resumable || prepareFailed || inTelegram && hasInitData() || telegramClientId !== null} error={authError} />
  return <BackendApp/>
}
