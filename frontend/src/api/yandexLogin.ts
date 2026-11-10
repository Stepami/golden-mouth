import { runtimeConfig } from '../app/runtimeConfig'

interface YandexProof { access_token: string; expires_in: number }
interface YandexSuggest {
  init(oauth: { client_id: string; response_type: 'token'; redirect_uri: string }, tokenPageOrigin: string, options: {
    view: 'button'; parentId: string; buttonView: 'main'; buttonTheme: 'light'; buttonSize: 'm'; buttonBorderRadius: number; buttonIcon: 'ya'
  }): Promise<{ handler(): Promise<YandexProof> }>
}
declare global { interface Window { YaAuthSuggest?: YandexSuggest; YaSendSuggestToken?: (origin: string) => void } }

let loading: Promise<YandexSuggest> | undefined
function loadYandexSdk(): Promise<YandexSuggest> {
  if (window.YaAuthSuggest) return Promise.resolve(window.YaAuthSuggest)
  return loading ??= new Promise<YandexSuggest>((resolve, reject) => {
    const script = document.createElement('script')
    const timer = setTimeout(() => { script.remove(); reject(new Error('Yandex ID SDK timed out')) }, 15_000)
    script.src = 'https://yastatic.net/s3/passport-sdk/autofill/v1/sdk-suggest-with-polyfills-latest.js'
    script.async = true
    script.onload = () => {
      clearTimeout(timer)
      if (window.YaAuthSuggest) resolve(window.YaAuthSuggest)
      else reject(new Error('Yandex ID SDK unavailable'))
    }
    script.onerror = () => { clearTimeout(timer); script.remove(); reject(new Error('Yandex ID SDK failed to load')) }
    document.head.append(script)
  }).catch(error => { loading = undefined; throw error })
}

export async function renderYandexLogin(container: HTMLElement, onToken: (token: string, expiresIn: number) => void, onError: () => void): Promise<() => void> {
  const sdk = await loadYandexSdk()
  if (!container.isConnected) return () => {}
  let active = true
  const result = await sdk.init(
    { client_id: runtimeConfig.yandexClientId, response_type: 'token', redirect_uri: runtimeConfig.yandexRedirectUrl },
    new URL(runtimeConfig.yandexRedirectUrl).origin,
    { view: 'button', parentId: container.id, buttonView: 'main', buttonTheme: 'light', buttonSize: 'm', buttonBorderRadius: 8, buttonIcon: 'ya' },
  )
  void result.handler().then(data => { if (active) onToken(data.access_token, data.expires_in) }).catch(() => { if (active) onError() })
  return () => { active = false; container.replaceChildren() }
}
