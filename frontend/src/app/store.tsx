import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { ApiError, api } from '../api/client'
import type { Player } from '../api/types'

export type PlayerState = 'loading' | 'ready' | 'missing' | 'failed'
interface Store { player: Player | null; playerState: PlayerState; setPlayer: (p: Player) => void; patch: (p: Partial<Player>) => void }
const Ctx = createContext<Store>(null!)
export const useStore = () => useContext(Ctx)
export const usePlayer = () => useContext(Ctx).player!

export function StoreProvider({ children }: { children: ReactNode }) {
  const [player, setPlayerValue] = useState<Player | null>(null)
  const [playerState, setPlayerState] = useState<PlayerState>('loading')
  const setPlayer = (value: Player) => { setPlayerValue(value); setPlayerState('ready') }
  useEffect(() => {
    api.me()
      .then(setPlayer)
      .catch(e => setPlayerState(e instanceof ApiError && e.status === 404 ? 'missing' : 'failed'))
  }, [])
  return <Ctx.Provider value={{ player, playerState, setPlayer, patch: p => setPlayerValue(cur => cur && { ...cur, ...p }) }}>{children}</Ctx.Provider>
}
