import {createPortal} from 'react-dom'
import {useEffect, useId, useRef, useState} from 'react'
import {api} from '../../api/client'
import {assertSession, sessionGeneration} from '../../api/session'
import {inTelegram} from '../../telegram'
import {Button, Sheet} from '../../ui'
import {InstallApp} from '../install/InstallApp'
import {runtimeConfig} from '../runtimeConfig'
import {applicationServerKey, pushRegistration, pushSupport, subscribePush, validateSubscriptionKey} from './browser'

export function PushNotifications({playerId}: {playerId: string}) {
  const [open, setOpen] = useState(false)
  const [permission, setPermission] = useState<NotificationPermission>('default')
  const [saved, setSaved] = useState(false)
  const [subscribed, setSubscribed] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const pending = useRef(false), mounted = useRef(true)
  const dialog = useRef<HTMLDivElement>(null), heading = useRef<HTMLHeadingElement>(null)
  const titleId = useId()
  const support = pushSupport(inTelegram)
  const key = api.webPushAvailable() ? runtimeConfig.webPushPublicKey : ''
  useEffect(() => { mounted.current = true; return () => { mounted.current = false } }, [])

  const connect = async (ask: boolean) => {
    if (pending.current || support !== 'supported') return
    pending.current = true; setBusy(true); setError(''); setSaved(false)
    const generation = sessionGeneration()
    const current = () => { assertSession(generation); if (!mounted.current) throw new DOMException('Closed', 'AbortError') }
    try {
      // Request permission directly from the click, before waiting for the worker or network.
      if (ask) applicationServerKey(key)
      const allowed = ask && Notification.permission === 'default' ? await Notification.requestPermission() : Notification.permission
      current(); setPermission(allowed)
      if (allowed !== 'granted') { setSubscribed(false); return }
      const registration = await pushRegistration()
      current()
      const subscription = ask ? await subscribePush(registration, key) : await registration.pushManager.getSubscription()
      current(); setSubscribed(!!subscription)
      if (!subscription || !key) return
      validateSubscriptionKey(subscription, key)
      const {endpoint, keys} = subscription.toJSON()
      if (!endpoint || !keys?.p256dh || !keys.auth) throw new Error('Incomplete subscription')
      if (api.playerId() !== playerId) throw new DOMException('Account changed', 'AbortError')
      await api.registerWebPush({endpoint, p256dh: keys.p256dh, auth: keys.auth})
      current(); setSaved(true)
    } catch (cause) {
      if (mounted.current && generation === sessionGeneration() && !(cause instanceof DOMException && cause.name === 'AbortError')) {
        setError('Не получилось подключить уведомления. Проверь соединение и попробуй ещё раз.')
      }
    } finally {
      pending.current = false
      if (mounted.current) setBusy(false)
    }
  }

  useEffect(() => {
    if (!open) return
    const refresh = () => { if (document.visibilityState !== 'hidden') void connect(false) }
    refresh()
    window.addEventListener('focus', refresh)
    document.addEventListener('visibilitychange', refresh)
    const previous = document.activeElement as HTMLElement | null
    heading.current?.focus({preventScroll: true})
    const keyboard = (event: KeyboardEvent) => {
      if (event.key === 'Escape') { event.preventDefault(); setOpen(false) }
      if (event.key !== 'Tab') return
      const items = dialog.current?.querySelectorAll<HTMLElement>('button:not(:disabled),a[href],input')
      if (!items?.length) return
      const first = items[0], last = items[items.length - 1]
      if (event.shiftKey && (document.activeElement === first || document.activeElement === heading.current)) { event.preventDefault(); last.focus() }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus() }
    }
    document.addEventListener('keydown', keyboard)
    return () => {
      window.removeEventListener('focus', refresh)
      document.removeEventListener('visibilitychange', refresh)
      document.removeEventListener('keydown', keyboard)
      previous?.focus({preventScroll: true})
    }
  }, [open, playerId])

  const status = busy ? 'Проверяем…' : error ? error : permission === 'denied'
    ? 'Браузер запрещает уведомления. Разреши их для «Рек» в настройках браузера или устройства, затем вернись сюда.'
    : saved ? 'Подписка сохранена. Показ уведомлений зависит от настроек браузера и устройства.'
    : subscribed ? 'Подписка есть в браузере, но её сохранение для этого аккаунта пока не подтверждено.'
    : permission === 'granted' ? 'Разрешение получено. Подключи уведомления для этого аккаунта.'
    : 'Браузер запросит разрешение по нажатию кнопки. Изменить его позже можно в настройках браузера или устройства.'

  return <>
    <button type="button" className="install-settings" onClick={() => setOpen(true)}><span>🔔 Уведомления</span><span aria-hidden="true">›</span></button>
    {open && createPortal(<Sheet onClose={() => setOpen(false)}><div ref={dialog} role="dialog" aria-modal="true" aria-labelledby={titleId} className="install-dialog" style={{overflowWrap: 'anywhere'}}>
      <h2 ref={heading} id={titleId} tabIndex={-1}>Уведомления</h2>
      {support === 'telegram' ? <p>Напоминания будут приходить сообщением от бота в Telegram. Разрешение браузера здесь не нужно. Если бот заблокирован, сообщения не придут.</p>
        : support === 'embedded' ? <p>Для браузерных уведомлений открой «Реки» в обычном браузере.</p>
        : support === 'install' ? <><p>На iPhone и iPad уведомления доступны в установленном приложении на iOS 16.4 и новее. Добавь «Реки» на экран «Домой», открой с иконки и войди в аккаунт.</p><InstallApp entry="settings" /></>
        : support === 'unsupported' ? <p>Этот браузер не поддерживает уведомления для «Рек». Попробуй актуальную версию другого браузера. Подключение должно быть защищённым.</p>
        : <>
          <p role={error ? 'alert' : 'status'} aria-live="polite">{status}</p>
          {!key && <p className="small muted">Подключение уведомлений пока недоступно. Оно появится после обновления сервиса.</p>}
          {key && permission !== 'denied' && !saved && <Button disabled={busy} onClick={() => void connect(true)}>{busy ? 'Подключаем…' : permission === 'granted' ? 'Подключить уведомления' : 'Разрешить уведомления'}</Button>}
        </>}
      <Button secondary onClick={() => setOpen(false)}>Закрыть</Button>
    </div></Sheet>, document.body)}
  </>
}
