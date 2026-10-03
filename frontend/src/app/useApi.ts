import { useEffect, useState } from 'react'

/** Загрузка данных экрана через api: пока грузится, отдаёт fallback (тот же mock), чтобы экран не мигал. */
export function useApi<T>(load: () => Promise<T>, fallback: T, deps: unknown[] = []): T {
  const [v, setV] = useState<T>(fallback)
  useEffect(() => { let alive = true; load().then(x => { if (alive) setV(x) }).catch(() => {}); return () => { alive = false } }, deps) // eslint-disable-line react-hooks/exhaustive-deps
  return v
}
