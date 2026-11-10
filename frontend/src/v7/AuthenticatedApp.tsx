import {useEffect, useState, useSyncExternalStore} from 'react';
import BackendApp from './BackendApp';
import {
  hasAccess, needsRegistration, startAuth, subscribeAuth,
  hasInitData, loginMiniApp, loginWidget, loginVk, loginYandex,
} from '../api/auth';
import {AuthProviders} from '../app/AuthProviders';
import {TelegramLaunch} from '../app/TelegramLaunch';

export default function AuthenticatedApp() {
  const authenticated = useSyncExternalStore(subscribeAuth, hasAccess);
  const registration = useSyncExternalStore(subscribeAuth, needsRegistration);
  const [ready, setReady] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    void startAuth().catch(() => setError('Не удалось войти')).finally(() => setReady(true));
  }, []);

  const finishLogin = (request: Promise<unknown>) => {
    setError('');
    setBusy(true);
    void request.catch(() => setError('Не удалось войти')).finally(() => setBusy(false));
  };

  if (!ready) return <div className="loading-state">Входим в игру</div>;
  if (!authenticated && !registration) {
    return (
      <TelegramLaunch error={error}>
        <AuthProviders
          busy={busy}
          onMiniApp={hasInitData() ? () => finishLogin(loginMiniApp()) : undefined}
          onTelegram={token => finishLogin(loginWidget(token))}
          onVk={(token, expiry) => finishLogin(loginVk(token, expiry))}
          onYandex={(token, expiry) => finishLogin(loginYandex(token, expiry))}
          onError={() => setError('Вход не завершён')}
        />
      </TelegramLaunch>
    );
  }
  return <BackendApp />;
}
