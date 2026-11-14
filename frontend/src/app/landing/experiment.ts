export type LandingVariant = 'worlds' | 'hogwarts'
type Assignment = { variant: LandingVariant; source: 'link' | 'direct'; utm: Record<string, string>; firstPost?: boolean; initialVariant?: LandingVariant; switched?: boolean }
const key = 'reki:landing:entry-v1'
const authKey = `${key}:auth`
let fallback: Assignment | undefined
const valid = (value: unknown): value is LandingVariant => value === 'worlds' || value === 'hogwarts'
export const isLandingPreview = () => !!import.meta.env?.DEV && new URLSearchParams(location.search).get('preview') === 'landing'
export function requestedVariant(): LandingVariant | undefined {
  const value = new URLSearchParams(location.search).get('landing')
  return valid(value) ? value : undefined
}
function read(): Assignment | undefined {
  try {
    const value = JSON.parse(localStorage.getItem(key) ?? 'null') as Assignment | null
    if (value && valid(value.variant) && (value.source === 'link' || value.source === 'direct')) return value
  } catch { /* Storage can be unavailable in private browsing. */ }
  return fallback
}
function save(value: Assignment) {
  fallback = value
  try { localStorage.setItem(key, JSON.stringify(value)) } catch { /* Keep this visit usable. */ }
}
export function assignLanding(): LandingVariant {
  const saved = read()
  const requested = requestedVariant()
  if (saved && (!requested || requested === saved.variant)) return saved.variant
  const query = new URLSearchParams(location.search)
  const utm: Record<string, string> = {}
  for (const name of ['utm_source', 'utm_medium', 'utm_campaign', 'utm_content']) {
    const value = query.get(name)
    if (value && /^[a-zA-Z0-9_.-]{1,64}$/.test(value)) utm[name] = value
  }
  const assignment: Assignment = { variant: requested ?? 'worlds', source: requested ? 'link' : 'direct', utm, firstPost: saved?.firstPost, initialVariant: saved?.initialVariant ?? saved?.variant ?? requested ?? 'worlds', switched: saved?.switched || !!saved && saved.variant !== requested }
  authFallback = false
  try { sessionStorage.removeItem(authKey) } catch { /* A new landing starts a new entry attempt. */ }
  save(assignment)
  return assignment.variant
}
export function landingContext(): Record<string, string> | undefined {
  const value = read()
  return value ? { ...value.utm, landing_experiment: 'entry_v1', landing_variant: value.variant, landing_initial_variant: valid(value.initialVariant) ? value.initialVariant : value.variant, landing_switched: value.switched ? 'true' : 'false', landing_source: value.source } : undefined
}
let authFallback = false
export function landingAuthPending(): boolean {
  const requested = requestedVariant()
  if (requested && requested !== read()?.variant) return false
  try { return sessionStorage.getItem(authKey) === '1' || authFallback } catch { return authFallback }
}
export function beginLandingAuth() {
  authFallback = true
  try { sessionStorage.setItem(authKey, '1') } catch { /* Keep the button working. */ }
}
export function completeLandingAuth(): boolean {
  if (isLandingPreview() || !landingAuthPending()) return false
  authFallback = false
  try { sessionStorage.removeItem(authKey) } catch { /* In-memory state is enough for this visit. */ }
  return true
}
export function completeLandingPost(): boolean {
  const value = read()
  if (isLandingPreview() || !value || value.firstPost) return false
  save({ ...value, firstPost: true })
  return true
}
