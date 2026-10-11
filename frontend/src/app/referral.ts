let remembered: string | undefined
const KEY = 'reki:pending-referral:v1'
const validCode = (value: string | null | undefined) => !!value && /^ref_[a-f0-9]{32}$/i.test(value)

/** Keep the first launch source through all onboarding steps and page reloads. */
export function rememberReferral(value: string | null | undefined) {
  if (!validCode(value)) return
  if (pendingReferral()) return
  remembered = value!.toLowerCase()
  try { sessionStorage.setItem(KEY, remembered) } catch { /* optional storage */ }
}
export function pendingReferral(): string | undefined {
  try { const value = sessionStorage.getItem(KEY); return validCode(value) ? value! : remembered } catch { return remembered }
}
export function clearReferral() { remembered = undefined; try { sessionStorage.removeItem(KEY) } catch { /* optional storage */ } }
