import { isLandingPreview, landingContext, landingVisitIdentity } from './landing/experiment'

export function reachMetrikaGoal(name: string, params?: Record<string, string | undefined>) {
  if (!import.meta.env.PROD || isLandingPreview()) return
  const ym = (window as Window & { ym?: (id: number, method: string, goal: string, params: Record<string, string | undefined>) => void }).ym
  try {
    const entry = landingVisitIdentity()
    ym?.(114170439, 'reachGoal', name, { ...landingContext(), ...params, ...(entry ? { entry_id: entry.entryId } : {}) })
  } catch { /* Analytics never blocks entry or publishing. */ }
}
