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
const DemoApp = import.meta.env.DEV ? lazy(() => import('./v7/DemoApp')) : null;
document.documentElement.dataset.theme = 'dark';
const keyboardInput = () => { document.documentElement.dataset.input = 'keyboard'; };
const pointerInput = () => { document.documentElement.dataset.input = 'pointer'; };
document.addEventListener('keydown', keyboardInput, {capture:true});
document.addEventListener('pointerdown', pointerInput, {capture:true});
import.meta.hot?.dispose(() => {
  document.removeEventListener('keydown', keyboardInput, {capture:true});
  document.removeEventListener('pointerdown', pointerInput, {capture:true});
});
if (initTelegram()) { installGlobalHandlers(); installAnalytics(); }
const demo = import.meta.env.DEV && new URLSearchParams(location.search).has('v7demo');
createRoot(document.getElementById('root')!).render(<StrictMode><ErrorBoundary><Suspense fallback={<div className="app"><div className="loading-state" role="status"><LoadingIndicator/><p>Открываем Реки</p></div></div>}>{visualReview && VisualReview ? <VisualReview /> : demo && DemoApp ? <DemoApp /> : <AuthenticatedApp />}</Suspense></ErrorBoundary></StrictMode>);
