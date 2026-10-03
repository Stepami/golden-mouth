import type { ComponentType } from 'react'
import { Gallery } from './Gallery'

export interface ScreenDef { component: ComponentType; title: string }
/** Реестр экранов: ключ = код макета из docs/screens. Экраны добавляются по issues #4–#7, #12–#15. */
export const SCREENS: Record<string, ScreenDef> = {
  gallery: { component: Gallery, title: 'Все экраны' },
}
/** Экраны с нижним меню: код → номер вкладки. */
export const TAB_OF: Record<string, number> = {}

export function register(defs: Record<string, ScreenDef>, tabs: Record<string, number> = {}) {
  Object.assign(SCREENS, defs); Object.assign(TAB_OF, tabs)
}
