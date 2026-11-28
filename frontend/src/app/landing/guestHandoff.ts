export type GuestDialogue = { starId: string; first: string; final?: string }
export type GuestHandoff = {
  id?: string
  worldId: string
  hero?: string
  text: string
  reply: string
  dialogue?: GuestDialogue
  playerId?: string
  worldPending?: boolean
  worldInstanceId?: string
  postPending?: boolean
  postId?: string
  initialCommentId?: string
  playerCommentId?: string
  finalCommentId?: string
  claim?: 'pending' | 'confirmed' | 'declined'
}
const key = 'reki:guest-handoff:v1'

export function saveGuestHandoff(value: GuestHandoff): boolean {
  try {
    localStorage.setItem(key, JSON.stringify(value))
    return true
  } catch { return false }
}

export function readGuestHandoff(): GuestHandoff | undefined {
  try {
    const value = JSON.parse(localStorage.getItem(key) ?? 'null') as GuestHandoff | null
    if (value && typeof value.worldId === 'string' && typeof value.text === 'string' && typeof value.reply === 'string') return value
  } catch { /* Invalid or unavailable storage. */ }
}

export function clearGuestHandoff() {
  try { localStorage.removeItem(key) } catch { /* Optional cleanup after the persisted import completed. */ }
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
