import { useEffect, useRef, useState, type MouseEvent } from 'react'
import { loadTelegramClientId, type AuthProvider } from '../api/auth'
import { openTelegramLogin, preloadTelegramLogin } from '../api/telegramLogin'
import { renderVkLogin } from '../api/vkLogin'
import { renderYandexLogin } from '../api/yandexLogin'
import { TELEGRAM_APP_URL } from './telegramLinks'
import { runtimeConfig } from './runtimeConfig'

function ProviderMark({ provider }: { provider: 'telegram' | 'vk' }) {
  return provider === 'telegram' ? <svg viewBox="0 0 64 64" aria-hidden="true"><path d="M53.8 10.8 45.7 50c-.6 2.8-2.3 3.5-4.6 2.2L28.8 43l-5.9 5.7c-.7.7-1.2 1.2-2.5 1.2l.9-12.5 22.8-20.6c1-.9-.2-1.4-1.5-.5L14.4 34.1 2.3 30.3c-2.6-.8-2.7-2.6.6-3.9L50.2 8.2c2.2-.8 4.1.5 3.6 2.6Z" /></svg>
    : <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M13.2 18.2C5 18.2.3 12.6.1 3.3h4.1c.2 6.8 3.2 9.7 5.5 10.3V3.3h3.9v5.9c2.3-.3 4.7-2.9 5.5-5.9H23c-.6 3.7-3.3 6.4-5.2 7.5 1.9.9 5 3.3 6.2 7.4h-4.3c-.9-2.8-3.1-5-6.1-5.3v5.3h-.4Z" /></svg>
}

type ProviderError = 'error' | 'cancel' | 'setup_error'

function widgetClick(event: MouseEvent, onSelected?: () => void) {
  if (event.target instanceof Element && event.target.closest('button, a, [role="button"]')) onSelected?.()
}

function VkLogin({ onToken, onError, onSelected }: { onToken: (token: string, expiresIn: number) => void; onError: (outcome: ProviderError) => void; onSelected?: () => void }) {
  const container = useRef<HTMLDivElement>(null)
  const callbacks = useRef({ onToken, onError })
  callbacks.current = { onToken, onError }
  const [loading, setLoading] = useState(true)
  const [failed, setFailed] = useState(false)
  const [attempt, setAttempt] = useState(0)
  useEffect(() => {
    let active = true
    let close: (() => void) | undefined
    void renderVkLogin(container.current!, (token, expiry) => { if (active) callbacks.current.onToken(token, expiry) }, outcome => { if (active) callbacks.current.onError(outcome) }).then(cleanup => {
      if (active) { close = cleanup; setLoading(false) } else cleanup()
    }).catch(() => { if (active) { setLoading(false); setFailed(true); callbacks.current.onError('setup_error') } })
    return () => { active = false; close?.() }
  }, [attempt])
  return <div className="vk-login" aria-label="Войти с VK">{(loading || failed) && <button className="provider-button provider-button--vk" disabled={loading} onClick={() => { setLoading(true); setFailed(false); setAttempt(value => value + 1) }}><ProviderMark provider="vk" />Войти с VK</button>}<div ref={container} onClickCapture={event => widgetClick(event, onSelected)} /></div>
}

let yandexContainerId = 0
function YandexLogin({ onToken, onError, onSelected }: { onToken: (token: string, expiresIn: number) => void; onError: (outcome: ProviderError) => void; onSelected?: () => void }) {
  const container = useRef<HTMLDivElement>(null)
  const callbacks = useRef({ onToken, onError })
  callbacks.current = { onToken, onError }
  const [loading, setLoading] = useState(true)
  const [failed, setFailed] = useState(false)
  const [attempt, setAttempt] = useState(0)
  useEffect(() => {
    let active = true
    let close: (() => void) | undefined
    const target = document.createElement('div')
    target.id = `yandex-login-${++yandexContainerId}`
    container.current!.append(target)
    void renderYandexLogin(target, (token, expiry) => { if (active) callbacks.current.onToken(token, expiry) }, () => { if (active) callbacks.current.onError('error') }).then(cleanup => {
      if (active) { close = cleanup; setLoading(false) } else cleanup()
    }).catch(() => { if (active) { setLoading(false); setFailed(true); callbacks.current.onError('setup_error') } })
    return () => { active = false; target.remove(); close?.() }
  }, [attempt])
  return <div className="yandex-login">{(loading || failed) && <button className="provider-button provider-button--secondary" disabled={loading} onClick={() => { setLoading(true); setFailed(false); setAttempt(value => value + 1) }}>Войти с Яндекс ID</button>}<div ref={container} onClickCapture={event => widgetClick(event, onSelected)} /></div>
}

export function AuthProviders({ onTelegram, onVk, onYandex, onMiniApp, onError, onSelected, hiddenProvider, allowMiniAppLink = true, busy = false }: {
  onTelegram: (token: string) => void; onVk: (token: string, expiresIn: number) => void; onYandex: (token: string, expiresIn: number) => void; onError: (provider: AuthProvider, outcome: ProviderError) => void
  onSelected?: (provider: AuthProvider, action?: 'browser' | 'mini_app') => void
  busy?: boolean
  hiddenProvider?: AuthProvider
  allowMiniAppLink?: boolean
  onMiniApp?: () => void
}) {
  const [step, setStep] = useState<'providers' | 'telegram'>('providers')
  const [clientId, setClientId] = useState<number | null>(null)
  const [prepareFailed, setPrepareFailed] = useState(false)
  const [attempt, setAttempt] = useState(0)
  useEffect(() => {
    let active = true
    void Promise.all([preloadTelegramLogin(), loadTelegramClientId()]).then(([, id]) => { if (active) setClientId(id) })
      .catch(() => { if (active) setPrepareFailed(true) })
    return () => { active = false }
  }, [attempt])
  const telegram = () => {
    if (prepareFailed) { setPrepareFailed(false); setAttempt(value => value + 1); return }
    if (clientId !== null) {
      onSelected?.('telegram', 'browser')
      openTelegramLogin(clientId, onTelegram, outcome => onError('telegram', outcome))
    }
  }
  const miniApp = () => { onSelected?.('telegram', 'mini_app'); onMiniApp?.() }
  if (step === 'telegram') return <div className="auth-providers">
    <button className="provider-button provider-button--telegram" disabled={busy || clientId === null && !prepareFailed} onClick={telegram}><ProviderMark provider="telegram" />{prepareFailed ? 'Подготовить вход снова' : clientId === null ? 'Готовим вход…' : 'Войти в браузере'}</button>
    {onMiniApp ? <button className="provider-button provider-button--secondary" disabled={busy} onClick={miniApp}><ProviderMark provider="telegram" />Запустить MiniApp</button> : allowMiniAppLink && <a className="provider-button provider-button--secondary" href={TELEGRAM_APP_URL} onClick={miniApp}><ProviderMark provider="telegram" />Запустить MiniApp</a>}
    <button className="text-button" disabled={busy} onClick={() => setStep('providers')}>Назад</button>
  </div>
  return <div className="auth-providers">
    {hiddenProvider !== 'yandex' && runtimeConfig.yandexClientId && runtimeConfig.yandexRedirectUrl && <div inert={busy}><YandexLogin onToken={onYandex} onError={outcome => onError('yandex', outcome)} onSelected={() => onSelected?.('yandex')} /></div>}
    {hiddenProvider !== 'vk' && runtimeConfig.vkApplicationId !== 0 && runtimeConfig.vkRedirectUrl && <div inert={busy}><VkLogin onToken={onVk} onError={outcome => onError('vk', outcome)} onSelected={() => onSelected?.('vk')} /></div>}
    {hiddenProvider !== 'telegram' && <button className="provider-button provider-button--telegram" disabled={busy} onClick={() => { onSelected?.('telegram'); setStep('telegram') }}><ProviderMark provider="telegram" />Войти с Telegram</button>}
    {busy && <p role="status">Подтверждаем вход…</p>}
  </div>
}
