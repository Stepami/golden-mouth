import { createContext, useContext } from 'react'

export type ScreenId = string
export interface Nav {
  current: ScreenId; params: Record<string, unknown>
  push: (id: ScreenId, params?: Record<string, unknown>) => void
  replace: (id: ScreenId, params?: Record<string, unknown>) => void
  back: () => void
  tab: (i: number) => void
  canBack: boolean
}
export const NavContext = createContext<Nav>(null!)
export const useNav = () => useContext(NavContext)

/** Вкладки нижнего меню и их стартовые экраны (коды как в docs/screens). */
export const TABS: { icon: string; title: string; screen: ScreenId }[] = [
  { icon: '▦', title: 'Канал', screen: '03-5_stage' },
  { icon: '≋', title: 'Лента', screen: '03-3_feed' },
  { icon: '✉', title: 'Личка', screen: '03-1_dm' },
  { icon: '☺', title: 'Друзья', screen: '03-4_friends' },
  { icon: '★', title: 'Я', screen: '05-5_collection' },
  { icon: '◈', title: 'Миры', screen: '01-3_world' },
]
