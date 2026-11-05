import { StrictMode, lazy, Suspense } from 'react';
import { createRoot } from 'react-dom/client';
import AuthenticatedApp from './v7/AuthenticatedApp';
import BackendApp from './v7/BackendApp';
import { runtimeConfig } from './app/runtimeConfig';
import { ErrorBoundary } from './app/ErrorBoundary';
import { installAnalytics } from './app/analytics';
import { installGlobalHandlers } from './app/log';
import { initTelegram } from './telegram';
import './v7/theme.css';
import './v7/ui/ui.css';
import './v7/app.css';
import './v7/integration.css';

// The scenario prototype is an explicit development preview, never an API fallback.
const DemoApp = import.meta.env.DEV ? lazy(() => import('./v7/DemoApp')) : null;
document.documentElement.dataset.theme = 'light';
if (initTelegram()) { installGlobalHandlers(); installAnalytics(); }
const demo = import.meta.env.DEV && new URLSearchParams(location.search).has('v7demo');
createRoot(document.getElementById('root')!).render(<StrictMode><ErrorBoundary><Suspense fallback={<p>Открываем Реки…</p>}>{demo && DemoApp ? <DemoApp /> : runtimeConfig.devLegacyMiniAppAuth ? <BackendApp /> : <AuthenticatedApp />}</Suspense></ErrorBoundary></StrictMode>);
