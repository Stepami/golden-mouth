import { useEffect, useState } from 'react'
import { api } from '../api/client'
import { usePlayer } from './store'
import { cachedApi, saveApiCache } from './useApi'
import { telegramStartUrl } from './telegramLinks'

export function useReferralLink() {
  const player = usePlayer()
  const key = `referral:${player.id}`
  const [resolved, setResolved] = useState<{ key: string; link?: string }>(() => ({ key, link: cachedApi<string>(key) }))
  const link = resolved.key === key ? resolved.link : cachedApi<string>(key)
  const [error, setError] = useState(false)
  const [retry, setRetry] = useState(0)
  useEffect(() => {
    let alive = true
    setError(false)
    setResolved({ key, link: cachedApi<string>(key) })
    api.referral().then(({ code }) => {
      if (alive) { const url = telegramStartUrl(code); saveApiCache(key, url); setResolved({ key, link: url }) }
    }).catch(() => { if (alive) setError(true) })
    return () => { alive = false }
  }, [key, retry])
  return { link, error, retry: () => setRetry(value => value + 1) }
}
