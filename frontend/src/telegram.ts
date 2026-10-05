// Telegram Mini App: init, кнопка «назад», размер. Вне Telegram (браузер, разработка) тихо пропускаем.
import { backButton, expandViewport, init, isTMA, miniApp, retrieveRawInitData, swipeBehavior, viewport } from '@telegram-apps/sdk-react'
import { setInitData } from './api/auth'

export let inTelegram = false

export function initTelegram() {
  try {
    if (!isTMA()) return
    init()
    inTelegram = true
    setInitData(retrieveRawInitData() ?? '')
    backButton.mount.ifAvailable()
    miniApp.mount.ifAvailable()
    miniApp.setHeaderColor.ifAvailable('#0E0E13')
    miniApp.setBackgroundColor.ifAvailable('#0E0E13')
    swipeBehavior.mount.ifAvailable(); swipeBehavior.disableVertical.ifAvailable()
    if (viewport.mount.isAvailable()) viewport.mount().then(() => expandViewport.ifAvailable()).catch(() => {})
    miniApp.ready.ifAvailable()
  } catch { inTelegram = false }
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
