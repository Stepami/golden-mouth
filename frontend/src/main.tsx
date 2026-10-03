import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './screens/all'
import { App } from './App'
import { ErrorBoundary } from './app/ErrorBoundary'
import { installAnalytics } from './app/analytics'
import { installGlobalHandlers } from './app/log'
import { initTelegram } from './telegram'
import './theme.css'

if (new URLSearchParams(location.search).has('shot')) document.documentElement.classList.add('shot')
installGlobalHandlers()
installAnalytics()
initTelegram()
createRoot(document.getElementById('root')!).render(<StrictMode><ErrorBoundary><App /></ErrorBoundary></StrictMode>)
