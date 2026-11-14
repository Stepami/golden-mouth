import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './screens/all'
import { App } from './App'
import { ErrorBoundary } from './app/ErrorBoundary'
import { installAnalytics } from './app/analytics'
import { installGlobalHandlers } from './app/log'
import { initTelegram } from './telegram'
import './theme.css'

const callbackParams = new URLSearchParams(location.hash.slice(1))
const yandexCallback = (callbackParams.has('access_token') || callbackParams.has('error')) && (window.opener || window.parent !== window)
if (yandexCallback) {
  const script = document.createElement('script')
  script.src = 'https://yastatic.net/s3/passport-sdk/autofill/v1/sdk-suggest-token-with-polyfills-latest.js'
  script.onload = () => window.YaSendSuggestToken?.(window.location.origin)
  document.head.append(script)
} else {
  if (new URLSearchParams(location.search).has('shot')) document.documentElement.classList.add('shot')
  initTelegram()
  installGlobalHandlers()
  installAnalytics()
  createRoot(document.getElementById('root')!).render(<StrictMode><ErrorBoundary><App /></ErrorBoundary></StrictMode>)
}
