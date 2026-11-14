export type LandingVariant = 'worlds' | 'hogwarts' | 'infobiz' | 'evening' | 'houses'
type Assignment = { variant: LandingVariant; source: 'link' | 'direct'; utm: Record<string, string>; firstPost?: boolean; initialVariant?: LandingVariant; switched?: boolean; worldId?: string }
const key = 'reki:landing:entry-v1'
const authKey = `${key}:auth`
const worldKey = `${key}:world`
let worldFallback: string | undefined
let fallback: Assignment | undefined
const valid = (value: unknown): value is LandingVariant => value === 'worlds' || value === 'hogwarts' || value === 'infobiz' || value === 'evening' || value === 'houses'
export const isLandingPreview = () => !!import.meta.env?.DEV && new URLSearchParams(location.search).get('preview') === 'landing'
export function requestedVariant(): LandingVariant | undefined {
  const value = new URLSearchParams(location.search).get('landing')
  return valid(value) ? value : undefined
}
function read(): Assignment | undefined {
  if (fallback) return fallback
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
  const variant = requested ?? 'worlds'
  const source = requested ? 'link' : 'direct'
  if (saved && variant === saved.variant && source === saved.source) return saved.variant
  const query = new URLSearchParams(location.search)
  const utm: Record<string, string> = {}
  for (const name of ['utm_source', 'utm_medium', 'utm_campaign', 'utm_content']) {
    const value = query.get(name)
    if (value && /^[a-zA-Z0-9_.-]{1,64}$/.test(value)) utm[name] = value
  }
  const assignment: Assignment = { variant, source, utm, firstPost: saved?.firstPost, initialVariant: saved?.initialVariant ?? saved?.variant ?? variant, switched: saved?.switched || !!saved && saved.variant !== variant }
  clearLandingWorld()
  authFallback = false
  try { localStorage.removeItem(authKey) } catch { /* Optional storage. */ }
  try { sessionStorage.removeItem(authKey) } catch { /* A new landing starts a new entry attempt. */ }
  save(assignment)
  return assignment.variant
}
export function landingContext(): Record<string, string> | undefined {
  const value = read()
  return value ? { ...value.utm, landing_experiment: 'entry_v1', landing_variant: value.variant, landing_initial_variant: valid(value.initialVariant) ? value.initialVariant : value.variant, landing_switched: value.switched ? 'true' : 'false', landing_source: value.source, ...(value.worldId ? { landing_world_id: value.worldId } : {}) } : undefined
}
let authFallback = false
export function landingAuthPending(): boolean {
  const requested = requestedVariant()
  if (requested && requested !== read()?.variant) return false
  try { if (localStorage.getItem(authKey) === '1') return true } catch { /* Try the same-tab fallback. */ }
  try { return sessionStorage.getItem(authKey) === '1' || authFallback } catch { return authFallback }
}
export function beginLandingAuth(worldId: string) {
  worldFallback = worldId
  // OAuth can return in another tab, where sessionStorage is empty.
  try {
    localStorage.setItem(worldKey, worldId)
    localStorage.setItem(authKey, '1')
    sessionStorage.removeItem(worldKey)
    sessionStorage.removeItem(authKey)
  } catch {
    try { sessionStorage.setItem(worldKey, worldId); sessionStorage.setItem(authKey, '1') } catch { /* Keep this visit usable. */ }
  }
  const value = read()
  if (value) save({ ...value, worldId })
  authFallback = true
}
export function completeLandingAuth(): boolean {
  if (isLandingPreview() || !landingAuthPending()) return false
  authFallback = false
  try { localStorage.removeItem(authKey) } catch { /* Optional storage. */ }
  try { sessionStorage.removeItem(authKey) } catch { /* In-memory state is enough for this visit. */ }
  return true
}
export function completeLandingPost(): boolean {
  const value = read()
  if (isLandingPreview() || !value || value.firstPost) return false
  save({ ...value, firstPost: true })
  return true
}

export function pendingLandingWorld(): string | undefined {
  if (worldFallback) return worldFallback
  try { const world = localStorage.getItem(worldKey); if (world) return world } catch { /* Try the same-tab fallback. */ }
  try { return sessionStorage.getItem(worldKey) ?? undefined } catch { return undefined }
}
export function landingEntryPending(): boolean {
  const requested = requestedVariant()
  return (!requested || requested === read()?.variant) && (landingAuthPending() || !!pendingLandingWorld())
}
export function clearLandingWorld() {
  worldFallback = undefined
  try { localStorage.removeItem(worldKey) } catch { /* Optional storage. */ }
  try { sessionStorage.removeItem(worldKey) } catch { /* In-memory state is enough for this visit. */ }
}

export function loginFromWorldPreview(worldId: string | null) {
  if (worldId) beginLandingAuth(worldId)
  const url = new URL(location.href)
  for (const key of ['screen', 'gallery', 'world', 'shot', 'preview', 'landing']) url.searchParams.delete(key)
  location.assign(url.pathname + url.search)
}
