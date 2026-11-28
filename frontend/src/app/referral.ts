let remembered: string | undefined
const KEY = 'reki:pending-referral:v1'
export const isReferralCode = (value: string | null | undefined) => !!value && /^ref_[a-f0-9]{32}$/i.test(value)

/** Keep the first launch source through all onboarding steps and page reloads. */
export function rememberReferral(value: string | null | undefined) {
  if (!isReferralCode(value)) return
  if (pendingReferral()) return
  remembered = value!.toLowerCase()
  try { sessionStorage.setItem(KEY, remembered) } catch { /* optional storage */ }
}
export function pendingReferral(): string | undefined {
  try { const value = sessionStorage.getItem(KEY); return isReferralCode(value) ? value! : remembered } catch { return remembered }
}
export function clearReferral() { remembered = undefined; try { sessionStorage.removeItem(KEY) } catch { /* optional storage */ } }
