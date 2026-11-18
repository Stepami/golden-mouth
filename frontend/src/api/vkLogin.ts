import { runtimeConfig } from '../app/runtimeConfig'

interface VkProof { access_token: string; expires_in: number }
interface VkWidget {
  render(options: { container: HTMLElement; showAlternativeLogin: boolean; fastAuthEnabled: boolean; styles: { height: 56; borderRadius: number } }): VkWidget
  on(event: string, callback: (payload: { code: string; device_id: string; text?: string }) => void): VkWidget
  close(): void
}
interface VkSdk {
  Config: { init(options: { app: number; redirectUrl: string; responseMode: number; source: string; scope: string }): void }
  ConfigResponseMode: { Callback: number }
  ConfigSource: { LOWCODE: string }
  OneTap: new () => VkWidget
  WidgetEvents: { ERROR: string }
  OneTapInternalEvents: { LOGIN_SUCCESS: string }
  Auth: { exchangeCode(code: string, deviceId: string): Promise<VkProof> }
}
declare global { interface Window { VKIDSDK?: VkSdk } }

let loading: Promise<VkSdk> | undefined
function loadVkSdk(): Promise<VkSdk> {
  if (window.VKIDSDK) return Promise.resolve(window.VKIDSDK)
  return loading ??= new Promise<VkSdk>((resolve, reject) => {
    const script = document.createElement('script')
    const timer = setTimeout(() => { script.remove(); reject(new Error('VK ID SDK timed out')) }, 15_000)
    script.src = 'https://unpkg.com/@vkid/sdk@2.6.1/dist-sdk/umd/index.js'
    script.async = true
    script.onload = () => {
      clearTimeout(timer)
      if (window.VKIDSDK) resolve(window.VKIDSDK)
      else reject(new Error('VK ID SDK unavailable'))
    }
    script.onerror = () => { clearTimeout(timer); script.remove(); reject(new Error('VK ID SDK failed to load')) }
    document.head.append(script)
  }).catch(error => { loading = undefined; throw error })
}

export async function renderVkLogin(container: HTMLElement, onToken: (token: string, expiresIn: number) => void, onError: (outcome: 'error' | 'cancel') => void): Promise<() => void> {
  const VKID = await loadVkSdk()
  if (!container.isConnected) return () => {}
  VKID.Config.init({
    app: runtimeConfig.vkApplicationId, redirectUrl: runtimeConfig.vkRedirectUrl,
    responseMode: VKID.ConfigResponseMode.Callback, source: VKID.ConfigSource.LOWCODE, scope: '',
  })
  let active = true
  const oneTap = new VKID.OneTap()
  oneTap.render({ container, showAlternativeLogin: true, fastAuthEnabled: false, styles: { height: 56, borderRadius: 14 } })
    .on(VKID.WidgetEvents.ERROR, payload => {
      // OneTap 2.6.1 forwards the SDK's NewTabHasBeenClosed error as text.
      if (active) onError(payload?.text === 'New tab has been closed' ? 'cancel' : 'error')
    })
    .on(VKID.OneTapInternalEvents.LOGIN_SUCCESS, payload => {
      void VKID.Auth.exchangeCode(payload.code, payload.device_id)
        .then(data => { if (active) onToken(data.access_token, data.expires_in) })
        .catch(() => { if (active) onError('error') })
    })
  return () => { active = false; oneTap.close() }
}
