export const TELEGRAM_BOT_USERNAME = 'reki_appbot'
export const TELEGRAM_BOT_URL = `https://t.me/${TELEGRAM_BOT_USERNAME}`
export const TELEGRAM_APP_URL = `${TELEGRAM_BOT_URL}?startapp`

export const telegramStartUrl = (payload: string) => `${TELEGRAM_BOT_URL}?startapp=${encodeURIComponent(payload)}`
