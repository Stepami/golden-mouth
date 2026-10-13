export type ApiMode = 'mock' | 'backend'

export interface RuntimeConfig {
  apiMode: ApiMode
  apiBaseUrl: string
  features: { dm: boolean; friends: boolean; collection: boolean }
}

declare global {
  interface Window {
    __REKI_CONFIG__?: Partial<Omit<RuntimeConfig, 'features'>> & { features?: Partial<RuntimeConfig['features']> }
  }
}

const fallbackConfig: Readonly<RuntimeConfig> = Object.freeze({
  apiMode: 'mock',
  apiBaseUrl: '/api/v1',
  features: { dm: false, friends: false, collection: false },
})

function readRuntimeConfig(): Readonly<RuntimeConfig> {
  const source = window.__REKI_CONFIG__
  const apiMode = source?.apiMode
  const apiBaseUrl = typeof source?.apiBaseUrl === 'string'
    ? source.apiBaseUrl.replace(/\/$/, '')
    : undefined

  if (
    (apiMode !== 'mock' && apiMode !== 'backend') ||
    !apiBaseUrl?.startsWith('/') ||
    apiBaseUrl.startsWith('//')
  ) {
    console.error('[config] Invalid runtime configuration; mock mode is enabled')
    return fallbackConfig
  }

  const features = Object.fromEntries(Object.entries(fallbackConfig.features).map(([key, fallback]) => [key,
    typeof source?.features?.[key as keyof RuntimeConfig['features']] === 'boolean'
      ? source.features[key as keyof RuntimeConfig['features']] : fallback,
  ])) as RuntimeConfig['features']
  return Object.freeze({ apiMode, apiBaseUrl, features: Object.freeze(features) })
}

export const runtimeConfig = readRuntimeConfig()
export const apiBaseUrl = runtimeConfig.apiMode === 'backend'
  ? runtimeConfig.apiBaseUrl
  : undefined
