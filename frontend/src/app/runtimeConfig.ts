export type ApiMode = 'mock' | 'backend'

export interface RuntimeConfig {
  apiMode: ApiMode
  apiBaseUrl: string
}

declare global {
  interface Window {
    __REKI_CONFIG__?: Partial<RuntimeConfig>
  }
}

const fallbackConfig: Readonly<RuntimeConfig> = Object.freeze({
  apiMode: 'mock',
  apiBaseUrl: '/api/v1',
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

  return Object.freeze({ apiMode, apiBaseUrl })
}

export const runtimeConfig = readRuntimeConfig()
export const apiBaseUrl = runtimeConfig.apiMode === 'backend'
  ? runtimeConfig.apiBaseUrl
  : undefined
