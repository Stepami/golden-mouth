import { useEffect, useState } from 'react'
const cache = new Map<string, unknown>()
export const cachedApi = <T,>(key: string): T | undefined => cache.get(key) as T | undefined
export const saveApiCache = (key: string, value: unknown) => cache.set(key, value)
export const clearApiCache = () => cache.clear()

/** Загрузка данных экрана через api: сохраняет данные по ключу при повторном открытии экрана. */
export function useApi<T>(load: () => Promise<T>, fallback: T, deps: unknown[] = [], key?: string): T {
  const [activeKey, setActiveKey] = useState(key)
  const [v, setV] = useState<T>(() => key ? cachedApi<T>(key) ?? fallback : fallback)
  if (activeKey !== key) { setActiveKey(key); setV(key ? cachedApi<T>(key) ?? fallback : fallback) }
  useEffect(() => { let alive = true; load().then(x => { if (alive) { if (key) cache.set(key, x); setV(x) } }).catch(() => {}); return () => { alive = false } }, [...deps, key]) // eslint-disable-line react-hooks/exhaustive-deps
  return v
}
