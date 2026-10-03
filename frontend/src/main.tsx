import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './screens/all'
import { App } from './App'
import { initTelegram } from './telegram'
import './theme.css'

if (new URLSearchParams(location.search).has('shot')) document.documentElement.classList.add('shot')
initTelegram()
createRoot(document.getElementById('root')!).render(<StrictMode><App /></StrictMode>)
