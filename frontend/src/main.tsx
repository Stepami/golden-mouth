import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './screens/all'
import { App } from './App'
import { ErrorBoundary } from './app/ErrorBoundary'
import { installAnalytics } from './app/analytics'
import { installGlobalHandlers } from './app/log'
import { initTelegram } from './telegram'
import { rememberReferral } from './app/referral'
import './theme.css'

const callbackParams = new URLSearchParams(location.hash.slice(1))
const yandexCallback = (callbackParams.has('access_token') || callbackParams.has('error')) && (window.opener || window.parent !== window)
if (yandexCallback) {
  const script = document.createElement('script')
  script.src = 'https://yastatic.net/s3/passport-sdk/autofill/v1/sdk-suggest-token-with-polyfills-latest.js'
  script.onload = () => window.YaSendSuggestToken?.(window.location.origin)
  document.head.append(script)
} else {
  const query = new URLSearchParams(location.search)
  if (query.has('shot')) document.documentElement.classList.add('shot')
  rememberReferral(query.get('ref'))
  initTelegram()
  installGlobalHandlers()
  installAnalytics()
  createRoot(document.getElementById('root')!).render(<StrictMode><ErrorBoundary><App /></ErrorBoundary></StrictMode>)
}
