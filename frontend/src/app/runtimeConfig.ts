export interface RuntimeConfig {
  apiBaseUrl: string
  appClientId: string
  webPushPublicKey: string
  vkApplicationId: number
  vkRedirectUrl: string
  yandexClientId: string
  yandexRedirectUrl: string
  features: { dm: boolean; friends: boolean; collection: boolean }
}

declare global {
  interface Window {
    __REKI_CONFIG__?: Partial<Omit<RuntimeConfig, 'features'>> & { features?: Partial<RuntimeConfig['features']> }
  }
}

const fallbackConfig: Readonly<RuntimeConfig> = Object.freeze({
  apiBaseUrl: '/api/v1',
  appClientId: 'reki-app',
  webPushPublicKey: '',
  vkApplicationId: 0,
  vkRedirectUrl: '',
  yandexClientId: '',
  yandexRedirectUrl: '',
  features: { dm: false, friends: false, collection: false },
})

function readRuntimeConfig(): Readonly<RuntimeConfig> {
  const source = window.__REKI_CONFIG__
  const apiBaseUrl = typeof source?.apiBaseUrl === 'string'
    ? source.apiBaseUrl.replace(/\/$/, '')
    : undefined

  if (
    !apiBaseUrl?.startsWith('/') ||
    apiBaseUrl.startsWith('//')
  ) {
    console.error('[config] Invalid runtime configuration; using default API URL')
    return fallbackConfig
  }

  const features = Object.fromEntries(Object.entries(fallbackConfig.features).map(([key, fallback]) => [key,
    typeof source?.features?.[key as keyof RuntimeConfig['features']] === 'boolean'
      ? source.features[key as keyof RuntimeConfig['features']] : fallback,
  ])) as RuntimeConfig['features']
  const webPushPublicKey = typeof source?.webPushPublicKey === 'string' ? source.webPushPublicKey : fallbackConfig.webPushPublicKey
  const appClientId = typeof source?.appClientId === 'string' && source.appClientId ? source.appClientId : fallbackConfig.appClientId
  const vkApplicationId = typeof source?.vkApplicationId === 'number' ? source.vkApplicationId : fallbackConfig.vkApplicationId
  const vkRedirectUrl = typeof source?.vkRedirectUrl === 'string' ? source.vkRedirectUrl : fallbackConfig.vkRedirectUrl
  const yandexClientId = typeof source?.yandexClientId === 'string' ? source.yandexClientId : fallbackConfig.yandexClientId
  const yandexRedirectUrl = typeof source?.yandexRedirectUrl === 'string' ? source.yandexRedirectUrl : fallbackConfig.yandexRedirectUrl
  return Object.freeze({ apiBaseUrl, appClientId, webPushPublicKey, vkApplicationId, vkRedirectUrl, yandexClientId, yandexRedirectUrl, features: Object.freeze(features) })
}

export const runtimeConfig = readRuntimeConfig()
export const apiBaseUrl = runtimeConfig.apiBaseUrl
