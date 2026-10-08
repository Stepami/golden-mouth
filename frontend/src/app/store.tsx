import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'
import { api } from '../api/client'
import type { Player } from '../api/types'

export type PlayerState = 'loading' | 'ready' | 'missing' | 'failed'
interface Store { player: Player | null; playerState: PlayerState; setPlayer: (p: Player) => void; patch: (p: Partial<Player>) => void; refresh: () => Promise<void> }
const Ctx = createContext<Store>(null!)
export const useStore = () => useContext(Ctx)
export const usePlayer = () => useContext(Ctx).player!

export function StoreProvider({ children }: { children: ReactNode }) {
  const [player, setPlayerValue] = useState<Player | null>(null)
  const [playerState, setPlayerState] = useState<PlayerState>('loading')
  const setPlayer = (value: Player) => { setPlayerValue(value); setPlayerState('ready') }
  /** Reconciles player state with /me. On failure keeps an already ready player untouched. */
  const refresh = useCallback(async () => {
    try {
      setPlayerValue(await api.me())
      setPlayerState('ready')
    } catch (e) {
      setPlayerState(prev => prev === 'ready' ? 'ready' : (e as { status?: number })?.status === 404 ? 'missing' : 'failed')
    }
  }, [])
  useEffect(() => { void refresh() }, [refresh])
  return <Ctx.Provider value={{ player, playerState, setPlayer, patch: p => setPlayerValue(cur => cur && { ...cur, ...p }), refresh }}>{children}</Ctx.Provider>
}
