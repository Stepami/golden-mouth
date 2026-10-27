// Telegram Mini App: init, кнопка «назад», размер. Вне Telegram (браузер, разработка) тихо пропускаем.
import { backButton, init, isTMA, miniApp, retrieveRawInitData, retrieveLaunchParams, swipeBehavior, viewport } from '@tma.js/sdk'
import { rememberReferral } from './app/referral'
import { setInitData } from './api/auth'

export let inTelegram = false

export function initTelegram(): boolean {
  try {
    if (!isTMA()) return false
    init()
    inTelegram = true
    const raw = retrieveRawInitData() ?? ''
    setInitData(raw)
    rememberReferral(new URLSearchParams(raw).get('start_param') ?? retrieveLaunchParams().tgWebAppStartParam)
    backButton.mount.ifAvailable()
    miniApp.mount.ifAvailable()
    miniApp.setHeaderColor.ifAvailable(document.documentElement.dataset.theme === 'light' ? '#ffffff' : '#000000')
    miniApp.setBgColor.ifAvailable(document.documentElement.dataset.theme === 'light' ? '#ffffff' : '#000000')
    swipeBehavior.mount.ifAvailable(); swipeBehavior.disableVertical.ifAvailable()
    if (viewport.mount.isAvailable()) viewport.mount().then(() => viewport.expand.ifAvailable()).catch(() => {})
    miniApp.ready.ifAvailable()
  } catch { inTelegram = false }
  return inTelegram
}

/** Системная кнопка «назад» Telegram вместо своей стрелки. */
export function syncBackButton(visible: boolean, onBack: () => void): () => void {
  if (!inTelegram) return () => {}
  try {
    if (visible) backButton.show.ifAvailable(); else backButton.hide.ifAvailable()
    const off = backButton.onClick.isAvailable() ? backButton.onClick(onBack) : undefined
    return () => { off?.() }
  } catch { return () => {} }
}
