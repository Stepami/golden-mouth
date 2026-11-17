type GuestHandoff = { worldId: string; hero?: string; text: string; reply: string }
const key = 'reki:guest-handoff:v1'
let memory: GuestHandoff | undefined

export function saveGuestHandoff(value: GuestHandoff) {
  memory = value
  try { localStorage.setItem(key, JSON.stringify(value)) } catch { /* Same-tab fallback. */ }
}

export function readGuestHandoff(): GuestHandoff | undefined {
  if (memory) return memory
  try {
    const value = JSON.parse(localStorage.getItem(key) ?? 'null') as GuestHandoff | null
    if (value && typeof value.worldId === 'string' && typeof value.text === 'string' && typeof value.reply === 'string') return value
  } catch { /* Invalid or unavailable storage. */ }
}

export function clearGuestHandoff() {
  memory = undefined
  try { localStorage.removeItem(key) } catch { /* Same-tab fallback. */ }
}

const consentKey = 'reki:landing:registration-consent'
let consent = false
export function acceptedRegistrationConsent() {
  try { return consent || sessionStorage.getItem(consentKey) === '1' || localStorage.getItem(consentKey) === '1' } catch { return consent }
}

export function setRegistrationConsent(accepted: boolean) {
  consent = accepted
  try {
    if (accepted) localStorage.setItem(consentKey, '1')
    else { localStorage.removeItem(consentKey); sessionStorage.removeItem(consentKey) }
  } catch {
    try { if (accepted) sessionStorage.setItem(consentKey, '1') } catch { /* Same-tab fallback. */ }
  }
}
