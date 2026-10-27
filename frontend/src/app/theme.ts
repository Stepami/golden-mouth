import { miniApp } from '@tma.js/sdk'
import { inTelegram } from '../telegram'
export type Theme = 'light' | 'dark'
export function applySavedTheme(): Theme {
  let theme: Theme = 'dark'
  try { if (localStorage.getItem('reki:theme') === 'light') theme = 'light' } catch { /* device preference is optional */ }
  document.documentElement.dataset.theme = theme
  return theme
}
export function saveTheme(theme: Theme) {
  document.documentElement.dataset.theme = theme
  if (inTelegram) {
    const color = theme === 'light' ? '#ffffff' : '#000000'
    miniApp.setHeaderColor.ifAvailable(color); miniApp.setBgColor.ifAvailable(color)
  }
  try { localStorage.setItem('reki:theme', theme) } catch { /* current session still works */ }
}
