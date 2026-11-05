export interface RuntimeConfig {
  apiBaseUrl: string
  appClientId: string
  devLegacyMiniAppAuth: boolean
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
  devLegacyMiniAppAuth: false,
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
  const appClientId = typeof source?.appClientId === 'string' && source.appClientId ? source.appClientId : fallbackConfig.appClientId
  const devLegacyMiniAppAuth = import.meta.env.DEV && source?.devLegacyMiniAppAuth === true
  return Object.freeze({ apiBaseUrl, appClientId, devLegacyMiniAppAuth, features: Object.freeze(features) })
}

export const runtimeConfig = readRuntimeConfig()
export const apiBaseUrl = runtimeConfig.apiBaseUrl
