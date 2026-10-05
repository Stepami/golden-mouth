let initDataRaw = ''

export const setInitData = (raw: string) => { initDataRaw = raw }

export function telegramAuthHeaders(): Record<string, string> {
  return initDataRaw ? { Authorization: `TMiniApp ${initDataRaw}` } : {}
}
