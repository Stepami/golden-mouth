import { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from 'react'
import { selectDraftOwner } from './worldDraft'
import { clearResultHistory } from './resultHistory'
import { api } from '../api/client'
import type { Player } from '../api/types'

export type PlayerState = 'loading' | 'ready' | 'missing' | 'unauthorized' | 'failed'
interface Store { player: Player | null; playerState: PlayerState; setPlayer: (p: Player) => void; resetPlayer: () => void; patch: (p: Partial<Player>) => void; refresh: () => Promise<Player | undefined>; unlockCelebration: boolean; dismissUnlockCelebration: () => void }
const unlockSeen = new Set<string>()
const Ctx = createContext<Store>(null!)
export const useStore = () => useContext(Ctx)
export const usePlayer = () => useContext(Ctx).player!

export function StoreProvider({ children }: { children: ReactNode }) {
  const generation = useRef(0)
  const [player, setPlayerValue] = useState<Player | null>(null)
  const [playerState, setPlayerState] = useState<PlayerState>('loading')
  const [unlockPlayerId, setUnlockPlayerId] = useState<string | null>(null)
  const resetPlayer = useCallback(() => {
    generation.current++
    clearResultHistory()
    if (player) unlockSeen.delete(player.id)
    selectDraftOwner('onboarding')
    setPlayerValue(null); setPlayerState('missing'); setUnlockPlayerId(null)
  }, [player])
  const setPlayer = useCallback((value: Player) => {
    selectDraftOwner(value.id); setPlayerValue(value); setPlayerState('ready')
    let seen = unlockSeen.has(value.id)
    try { seen ||= localStorage.getItem(`reki:own-world-unlock-seen:${value.id}`) === '1' } catch { /* session fallback */ }
    setUnlockPlayerId(value.hitOwnWorldMilestone && !seen ? value.id : null)
  }, [])
  const dismissUnlockCelebration = () => {
    if (unlockPlayerId) {
      unlockSeen.add(unlockPlayerId)
      try { localStorage.setItem(`reki:own-world-unlock-seen:${unlockPlayerId}`, '1') } catch { /* session fallback */ }
    }
    setUnlockPlayerId(null)
  }
  /** Reconciles player state with /me. On failure keeps an already ready player untouched. */
  const refresh = useCallback(async () => {
    const started = generation.current
    try {
      const next = await api.me()
      if (started !== generation.current) return
      setPlayer(next)
      return next
    } catch (e) {
      if (started !== generation.current) return
      selectDraftOwner(api.playerId() ?? 'onboarding')
      setPlayerState(prev => prev === 'ready' ? 'ready' : (e as { status?: number })?.status === 404 ? 'missing' : (e as { status?: number })?.status === 401 ? 'unauthorized' : 'failed')
    }
  }, [setPlayer])
  useEffect(() => { void refresh() }, [refresh])
  useEffect(() => {
    const check = () => { if (!document.hidden && api.hasPlayer()) void refresh() }
    window.addEventListener('focus', check); document.addEventListener('visibilitychange', check)
    const timer = window.setInterval(check, 30000)
    return () => { clearInterval(timer); window.removeEventListener('focus', check); document.removeEventListener('visibilitychange', check) }
  }, [refresh])
  return <Ctx.Provider value={{ player, playerState, setPlayer, resetPlayer, patch: p => setPlayerValue(cur => cur && { ...cur, ...p }), refresh, unlockCelebration: !!unlockPlayerId, dismissUnlockCelebration }}>{children}</Ctx.Provider>
}
