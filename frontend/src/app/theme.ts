import { miniApp } from '@tma.js/sdk'
import { inTelegram } from '../telegram'
export type Theme = 'light' | 'dark'
export type ThemePreference = Theme | 'system'
const systemTheme = matchMedia('(prefers-color-scheme: dark)')
export function savedTheme(): ThemePreference {
  try {
    const saved = localStorage.getItem('reki:theme')
    if (saved === 'light' || saved === 'system') return saved
  } catch { /* device preference is optional */ }
  return 'dark'
}
function apply(theme: ThemePreference): Theme {
  const resolved = theme === 'system' ? (systemTheme.matches ? 'dark' : 'light') : theme
  document.documentElement.dataset.theme = resolved
  if (inTelegram) {
    const color = resolved === 'light' ? '#ffffff' : '#000000'
    miniApp.setHeaderColor.ifAvailable(color); miniApp.setBgColor.ifAvailable(color)
  }
  return resolved
}
export function applySavedTheme(): Theme { return apply(savedTheme()) }
export function saveTheme(theme: ThemePreference) {
  apply(theme)
  try { localStorage.setItem('reki:theme', theme) } catch { /* current session still works */ }
}
systemTheme.addEventListener('change', () => { if (savedTheme() === 'system') apply('system') })
