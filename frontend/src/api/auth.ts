let initDataRaw = ''

export const setInitData = (raw: string) => { initDataRaw = raw }

export function createDevelopmentInitData(
  userId: number,
  authDateSeconds = Math.floor(Date.now() / 1000),
): string {
  const user = JSON.stringify({ id: userId, first_name: 'Development player' })
  return new URLSearchParams({
    user,
    auth_date: authDateSeconds.toString(),
    hash: 'development',
  }).toString()
}

export function setDevelopmentInitData(userId: number | undefined): void {
  if (userId !== undefined) setInitData(createDevelopmentInitData(userId))
}

export function telegramAuthHeaders(): Record<string, string> {
  return initDataRaw ? { Authorization: `TMiniApp ${initDataRaw}` } : {}
}
