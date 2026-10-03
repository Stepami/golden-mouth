import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { api } from '../api/client'
import type { Player } from '../api/types'

interface Store { player: Player | null; setPlayer: (p: Player) => void; patch: (p: Partial<Player>) => void }
const Ctx = createContext<Store>(null!)
export const useStore = () => useContext(Ctx)
export const usePlayer = () => useContext(Ctx).player!

export function StoreProvider({ children }: { children: ReactNode }) {
  const [player, setPlayer] = useState<Player | null>(null)
  useEffect(() => { api.me().then(setPlayer).catch(() => {}) }, [])
  return <Ctx.Provider value={{ player, setPlayer, patch: p => setPlayer(cur => cur && { ...cur, ...p }) }}>{children}</Ctx.Provider>
}
