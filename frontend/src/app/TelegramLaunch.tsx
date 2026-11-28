import type { ReactNode } from 'react'

export function TelegramLaunch({ children, error, pendingProvider, signup = false }: { children: ReactNode; error: string; pendingProvider?: string; signup?: boolean }) {
  return <main className="telegram-launch">
    <div className="telegram-launch__brand">реки</div>
    <section className="telegram-launch__content" aria-labelledby="telegram-launch-title">
      <img className="reki-login-logo" src="/android-chrome-192x192.png" alt="Реки" width="104" height="104" />
      <h1 id="telegram-launch-title">{signup ? 'Регистрация' : 'Твой блог. Твоя история.'}</h1>
      <p>{pendingProvider ? `Подтверждение ${pendingProvider} получено. Проверь соединение с игрой и продолжи вход.` : signup ? 'Выбери способ входа, чтобы сохранить пост и ответить герою.' : 'Пиши посты, знакомься с героями и меняй события своего мира.'}</p>
      {children}
      {error && <p className="auth-error" role="alert">{error}</p>}
    </section>
    <p className="telegram-launch__hint">Фанаты и события виртуальные. Посты остаются внутри игры.</p>
  </main>
}
