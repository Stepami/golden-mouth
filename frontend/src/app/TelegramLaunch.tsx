import { inTelegram } from '../telegram'
import { hasInitData } from '../api/auth'
import { TELEGRAM_APP_URL } from './telegramLinks'

function TelegramMark() {
  return (
    <svg className="telegram-launch__mark" viewBox="0 0 64 64" aria-hidden="true">
      <path d="M53.8 10.8 45.7 50c-.6 2.8-2.3 3.5-4.6 2.2L28.8 43l-5.9 5.7c-.7.7-1.2 1.2-2.5 1.2l.9-12.5 22.8-20.6c1-.9-.2-1.4-1.5-.5L14.4 34.1 2.3 30.3c-2.6-.8-2.7-2.6.6-3.9L50.2 8.2c2.2-.8 4.1.5 3.6 2.6Z" />
    </svg>
  )
}

export function TelegramLaunch({ onLogin, onRestart, pending, resumable, prepareFailed, busy, ready, error }: {
  onLogin: () => void; onRestart: () => void; pending: boolean; resumable: boolean; prepareFailed: boolean; busy: boolean; ready: boolean; error: string
}) {
  return (
    <main className="telegram-launch">
      <div className="telegram-launch__brand">реки</div>
      <section className="telegram-launch__content" aria-labelledby="telegram-launch-title">
        <div className="telegram-launch__icon"><TelegramMark /></div>
        <h1 id="telegram-launch-title">Твой блог. Твоя история.</h1>
        <p>{pending ? 'Подтверждение Telegram получено. Если используешь VPN, отключи его и продолжи вход.' : 'Пиши посты, знакомься с героями и меняй события своего мира.'}</p>
        <button className="telegram-launch__button" disabled={busy || !ready} onClick={onLogin}>{busy ? 'Входим…' : pending ? 'Продолжить вход' : resumable ? 'Повторить вход' : prepareFailed ? 'Подготовить вход снова' : inTelegram && hasInitData() ? 'Войти снова' : 'Играть в браузере'}</button>
        {pending && <button className="telegram-launch__button telegram-launch__button--secondary" disabled={busy} onClick={onRestart}>Войти заново</button>}
        {!inTelegram && <a className="telegram-launch__button telegram-launch__button--secondary" href={TELEGRAM_APP_URL}>Открыть в Telegram</a>}
        {error && <p role="alert">{error}</p>}
      </section>
      <p className="telegram-launch__hint">Фанаты и события виртуальные. Посты остаются внутри игры.</p>
    </main>
  )
}
