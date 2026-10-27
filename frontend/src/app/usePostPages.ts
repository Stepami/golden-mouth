import { useCallback, useEffect, useRef, useState } from 'react'
import type { PostsPage } from '../api/types'
import { sessionGeneration } from '../api/session'
import { cachedApi, saveApiCache } from './useApi'

type State<T> = { key: string; page?: PostsPage<T>; pending: 'first' | 'more' | null; error?: 'first' | 'more' }

/** Keeps pagination and stale-request protection shared by the feed and channel. */
export function usePostPages<T extends { postId?: string }>(key: string, load: (cursor: string | undefined, signal: AbortSignal) => Promise<PostsPage<T>>) {
  const initial = (): State<T> => ({ key, page: cachedApi<PostsPage<T>>(key), pending: 'first' })
  const [state, setState] = useState<State<T>>(initial)
  const latest = useRef(state)
  const request = useRef<AbortController | null>(null)
  const current = state.key === key ? state : initial()

  const fetchPage = useCallback(async (append: boolean): Promise<PostsPage<T> | undefined> => {
    if (request.current || latest.current.key !== key) return
    const previous = latest.current.page
    if (append && !previous?.nextCursor) return
    const cursor = append ? previous?.nextCursor ?? undefined : undefined
    const controller = new AbortController()
    const session = sessionGeneration()
    request.current = controller
    const pending: State<T> = { key, page: previous, pending: append ? 'more' : 'first' }
    latest.current = pending
    setState(pending)
    try {
      const page = await load(cursor, controller.signal)
      if (controller.signal.aborted || session !== sessionGeneration()) return
      const seen = new Set<string>()
      const items = [...(append ? previous?.items ?? [] : []), ...page.items].filter(item => {
        if (!item.postId) return true
        if (seen.has(item.postId)) return false
        seen.add(item.postId)
        return true
      })
      const next = { ...page, items }
      saveApiCache(key, next)
      latest.current = { key, page: next, pending: null }
      setState(latest.current)
      return page
    } catch {
      if (controller.signal.aborted || session !== sessionGeneration()) return
      latest.current = { key, page: previous, pending: null, error: append ? 'more' : 'first' }
      setState(latest.current)
    } finally {
      if (request.current === controller) request.current = null
    }
  }, [key, load])

  useEffect(() => {
    latest.current = { key, page: cachedApi<PostsPage<T>>(key), pending: null }
    setState(latest.current)
    void fetchPage(false)
    return () => {
      request.current?.abort()
      request.current = null
    }
  }, [key, fetchPage])

  return {
    items: current.page?.items ?? [],
    hasNext: !!current.page?.nextCursor,
    nextCursor: current.page?.nextCursor ?? null,
    ready: !!current.page,
    pending: current.pending,
    error: current.error,
    refresh: useCallback(() => fetchPage(false), [fetchPage]),
    loadMore: useCallback(() => fetchPage(true), [fetchPage]),
  }
}
