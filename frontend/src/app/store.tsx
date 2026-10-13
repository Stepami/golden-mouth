import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'
import { selectDraftOwner } from './worldDraft'
import { api } from '../api/client'
import type { Player } from '../api/types'

export type PlayerState = 'loading' | 'ready' | 'missing' | 'failed'
interface Store { player: Player | null; playerState: PlayerState; setPlayer: (p: Player) => void; patch: (p: Partial<Player>) => void; refresh: () => Promise<Player | undefined> }
const Ctx = createContext<Store>(null!)
export const useStore = () => useContext(Ctx)
export const usePlayer = () => useContext(Ctx).player!

export function StoreProvider({ children }: { children: ReactNode }) {
  const [player, setPlayerValue] = useState<Player | null>(null)
  const [playerState, setPlayerState] = useState<PlayerState>('loading')
  const setPlayer = (value: Player) => { selectDraftOwner(value.id); setPlayerValue(value); setPlayerState('ready') }
  /** Reconciles player state with /me. On failure keeps an already ready player untouched. */
  const refresh = useCallback(async () => {
    try {
      const next = await api.me()
      selectDraftOwner(next.id)
      setPlayerValue(next)
      setPlayerState('ready')
      return next
    } catch (e) {
      selectDraftOwner(api.playerId() ?? 'onboarding')
      setPlayerState(prev => prev === 'ready' ? 'ready' : (e as { status?: number })?.status === 404 ? 'missing' : 'failed')
    }
  }, [])
  useEffect(() => { void refresh() }, [refresh])
  return <Ctx.Provider value={{ player, playerState, setPlayer, patch: p => setPlayerValue(cur => cur && { ...cur, ...p }), refresh }}>{children}</Ctx.Provider>
}
