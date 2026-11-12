export type PushSupport = 'telegram' | 'embedded' | 'install' | 'unsupported' | 'supported'

export function pushSupport(telegram: boolean): PushSupport {
  if (telegram) return 'telegram'
  if (/FBAN|FBAV|Instagram|\bVK\b|; wv\)/i.test(navigator.userAgent)) return 'embedded'
  const ios = /iPad|iPhone|iPod/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)
  if (ios && !matchMedia('(display-mode: standalone)').matches && !(navigator as Navigator & { standalone?: boolean }).standalone) return 'install'
  return window.isSecureContext && 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window ? 'supported' : 'unsupported'
}

export function applicationServerKey(key: string): Uint8Array<ArrayBuffer> {
  const bytes = Uint8Array.from(atob(key.replace(/-/g, '+').replace(/_/g, '/')), c => c.charCodeAt(0))
  if (bytes.length !== 65 || bytes[0] !== 4) throw new Error('Invalid Web Push public key')
  return bytes
}

export async function pushRegistration(): Promise<ServiceWorkerRegistration> {
  const registration = await navigator.serviceWorker.register('/push-sw.js', { scope: '/', updateViaCache: 'none' })
  // Bound waiting: an unavailable worker must leave a retryable UI, not an endless spinner.
  let timer: ReturnType<typeof setTimeout> | undefined
  try { return await Promise.race([
    navigator.serviceWorker.ready.then(ready => {
      if (ready.scope !== registration.scope) throw new Error('Unexpected service worker scope')
      return ready
    }),
    new Promise<never>((_, reject) => { timer = setTimeout(() => reject(new Error('Service worker timeout')), 15000) }),
  ]) } finally { clearTimeout(timer) }
}

export function validateSubscriptionKey(subscription: PushSubscription, key: string): void {
  const bytes = applicationServerKey(key)
  const previous = subscription.options.applicationServerKey
  if (previous && (previous.byteLength !== bytes.length || !new Uint8Array(previous).every((value, i) => value === bytes[i]))) throw new Error('Web Push key changed')
}

export async function subscribePush(registration: ServiceWorkerRegistration, key: string): Promise<PushSubscription> {
  const bytes = applicationServerKey(key)
  const existing = await registration.pushManager.getSubscription()
  if (existing) {
    validateSubscriptionKey(existing, key)
    return existing
  }
  return registration.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: bytes })
}
