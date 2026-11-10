import {LoadingIndicator} from './v7/ui/LoadingIndicator';
import { StrictMode, lazy, Suspense } from 'react';
import { createRoot } from 'react-dom/client';
import AuthenticatedApp from './v7/AuthenticatedApp';
import { ErrorBoundary } from './app/ErrorBoundary';
import { installAnalytics } from './app/analytics';
import { installGlobalHandlers } from './app/log';
import { initTelegram } from './telegram';
import './v7/theme.css';
import './v7/ui/ui.css';
import './v7/app.css';
import './v7/integration.css';
import './v7/world-entry.css';
import './v7/night.css';
import './v7/ui/motion.css';

// The scenario prototype is an explicit development preview, never an API fallback.
const VisualReview = import.meta.env.DEV ? lazy(() => import('./v7/review/VisualReview')) : null;
const visualReview = import.meta.env.DEV && new URLSearchParams(location.search).has('visual-review');
document.documentElement.dataset.theme = 'dark';
const keyboardInput = () => { document.documentElement.dataset.input = 'keyboard'; };
const pointerInput = () => { document.documentElement.dataset.input = 'pointer'; };
document.addEventListener('keydown', keyboardInput, {capture:true});
document.addEventListener('pointerdown', pointerInput, {capture:true});
import.meta.hot?.dispose(() => {
  document.removeEventListener('keydown', keyboardInput, {capture:true});
  document.removeEventListener('pointerdown', pointerInput, {capture:true});
});
const callbackParams = new URLSearchParams(location.hash.slice(1));
const yandexCallback = (callbackParams.has('access_token') || callbackParams.has('error')) && (window.opener || window.parent !== window);
if (yandexCallback) {
  const script = document.createElement('script');
  script.src = 'https://yastatic.net/s3/passport-sdk/autofill/v1/sdk-suggest-token-with-polyfills-latest.js';
  script.onload = () => window.YaSendSuggestToken?.(window.location.origin);
  document.head.append(script);
} else {
if (initTelegram()) { installGlobalHandlers(); installAnalytics(); }
createRoot(document.getElementById('root')!).render(<StrictMode><ErrorBoundary><Suspense fallback={<div className="app"><div className="loading-state" role="status"><LoadingIndicator/><p>Открываем Реки</p></div></div>}>{visualReview && VisualReview ? <VisualReview /> : <AuthenticatedApp />}</Suspense></ErrorBoundary></StrictMode>);

}
