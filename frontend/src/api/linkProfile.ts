import { runtimeConfig } from '../app/runtimeConfig'
import type { AuthProvider } from './auth'
import type { PlayerResponse } from './generated/reki-api'
import { data, sdk } from './transport'

export async function identifyOtherProfile(provider: AuthProvider, proof: string, miniApp = false): Promise<PlayerResponse> {
  const suffix = provider === 'telegram' ? miniApp ? 'tg/miniapp' : 'tg/widget' : provider
  const grant = provider === 'telegram' ? miniApp ? 'telegram-mini-app' : 'telegram-login' : provider
  const response = await fetch(`${runtimeConfig.apiBaseUrl}/auth/external/${suffix}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded', Authorization: `${miniApp ? 'TMiniApp' : 'Bearer'} ${proof}` },
    body: new URLSearchParams({ client_id: runtimeConfig.appClientId, grant_type: `urn:reki:grant:${grant}` }),
    signal: AbortSignal.timeout(15_000),
  })
  if (!response.ok) throw new Error('Other login failed')
  const tokens = await response.json() as { access_token: string }
  const profile = await fetch(`${runtimeConfig.apiBaseUrl}/me`, {
    headers: { Authorization: `Bearer ${tokens.access_token}` }, signal: AbortSignal.timeout(15_000),
  })
  if (!profile.ok) throw new Error('Other profile unavailable')
  const result = await profile.json() as PlayerResponse
  if (!result.id) throw new Error('Other profile unavailable')
  return result
}

export async function mergeProfile(fromPlayerId: string): Promise<void> {
  await data(sdk.player.mergePlayers({ fromPlayerId }, AbortSignal.timeout(15_000)))
}
