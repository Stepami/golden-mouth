import { init, isTMA, miniApp, viewport, backButton } from "@tma.js/sdk";
let active = false;
export async function initializeTelegram() {
  if (!isTMA()) return;
  try {
    init();
    active = true;
    miniApp.mount.ifAvailable();
    miniApp.ready.ifAvailable();
    backButton.mount.ifAvailable();
    if (viewport.mount.isAvailable()) {
      await viewport.mount({ timeout: 5000 });
      viewport.bindCssVars();
      viewport.expand.ifAvailable();
    }
  } catch (error) {
    console.info(
      "Telegram host features unavailable; browser layout remains active.",
      error,
    );
  }
}
export function setTelegramBack(handler?: () => void) {
  if (!active) return () => {};
  if (handler) {
    backButton.show.ifAvailable();
    if (backButton.onClick.isAvailable()) return backButton.onClick(handler);
  } else backButton.hide.ifAvailable();
  return () => {};
}
