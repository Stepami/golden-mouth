import { useCallback, useEffect, useMemo, useState, useSyncExternalStore } from 'react'
import { NavContext, TABS, type Nav, type ScreenId } from './app/nav'
import { StoreProvider, useStore } from './app/store'
import { SCREENS, TAB_OF } from './screens'
import { screenView, setExperiments, tap, trackLanding, trackLandingAuth } from './app/analytics'
import { setScreen } from './app/log'
import { TelegramLaunch } from './app/TelegramLaunch'
import { syncBackButton } from './telegram'
import { api } from './api/client'
import { canResumeAuth, clearPendingLogin, hasAccess, hasInitData, hasPendingLogin, loginMiniApp, loginVk, loginWidget, loginYandex, needsRegistration, pendingAuthProvider, startAuth, subscribeAuth, type AuthProvider } from './api/auth'
import { AuthProviders } from './app/AuthProviders'
import { WorldUnlockCelebration } from './app/WorldUnlockCelebration'
import { runtimeConfig } from './app/runtimeConfig'
import { AvatarWorld, AvatarPeople } from './ui/avatar'
import { useAvatarPersonalization } from './app/avatarPersonalization'
import { GameOnboarding, isGameOnboardingPreview } from './app/GameOnboarding'
import { beginGameOnboarding, gameOnboardingActive, useGameOnboarding, restartGameOnboardingPreview } from './app/GameOnboardingState'
import type { WorldId } from './api/types'

import { Landing } from './app/landing/Landing'
import { beginLandingAuth, completeLandingAuth, landingAuthPending, landingEntryPending, isLandingPreview, pendingLandingWorld } from './app/landing/experiment'

type Entry = { id: ScreenId; params: Record<string, unknown> }

/** ?screen=<код> открывает любой экран сразу (сравнение с docs/screens), ?gallery показывает список. */
function startEntry(): Entry {
  if (!hasExplicitStart()) return { id: '01-1_invite', params: {} }
  const q = new URLSearchParams(location.search)
  const s = q.get('screen')
  if (q.has('gallery')) return { id: 'gallery', params: {} }
  if (s && SCREENS[s]) return { id: s, params: q.has('world') ? { world: q.get('world') } : {} }
  return { id: '01-1_invite', params: {} }
}

function hasExplicitStart(): boolean {
  const q = new URLSearchParams(location.search)
  return !!import.meta.env.DEV && (q.has('screen') || q.has('gallery'))
}

function Shell() {
  const [stack, setStack] = useState<Entry[]>(() => [startEntry()])
  const top = stack[stack.length - 1]
  const { player, playerState } = useStore()
  const onboarding = useGameOnboarding()
  const learning = gameOnboardingActive(onboarding)
  const [lockedSection, setLockedSection] = useState(false)
  useEffect(() => setLockedSection(false), [top.id, onboarding?.phase])
  const personalizeAvatar = useAvatarPersonalization(player?.id ?? 'onboarding')
  const explicitStart = hasExplicitStart()

  const nav: Nav = useMemo(() => ({
    current: top.id, params: top.params, canBack: stack.length > 1,
    push: (id, params = {}) => { setStack(s => [...s, { id, params }]); window.scrollTo(0, 0) },
    replace: (id, params = {}) => { setStack(s => [...s.slice(0, -1), { id, params }]); window.scrollTo(0, 0) },
    reset: (id, params = {}) => { setStack([{ id, params }]); window.scrollTo(0, 0) },
    back: () => setStack(s => (s.length > 1 ? s.slice(0, -1) : s)),
    tab: i => { setStack([{ id: TABS[i].screen, params: {} }]); window.scrollTo(0, 0) },
  }), [top, stack.length])

  const Screen = SCREENS[top.id]?.component
  const resolvingExistingPlayer = (playerState === 'ready' || playerState === 'missing' && (api.hasPlayer() || !!pendingLandingWorld())) && !explicitStart && top.id === '01-1_invite'
  const visibleScreen = Screen && playerState !== 'loading' && playerState !== 'failed' && !resolvingExistingPlayer ? top.id : undefined
  useEffect(() => {
    if (visibleScreen) { setScreen(visibleScreen); screenView(visibleScreen) }
  }, [visibleScreen])
  useEffect(() => { if (player?.experiments) setExperiments(player.experiments) }, [player?.experiments])
  useEffect(() => {
    if (explicitStart || top.id !== '01-1_invite' || playerState === 'loading' || playerState === 'failed') return
    const world = pendingLandingWorld()
    if (world) setStack([{ id: api.hasPlayer() ? '01-3b_world_overview' : '01-2_age', params: { world, fromLanding: true } }])
    else if (playerState === 'ready' || api.hasPlayer()) setStack([{ id: '01-3_world', params: {} }])
  }, [explicitStart, playerState, top.id])
  const back = useCallback(() => nav.back(), [nav])
  useEffect(() => syncBackButton(stack.length > 1 && top.id !== '02-6_thread', back), [stack.length, top.id, back])

  if (!Screen || playerState === 'loading' || resolvingExistingPlayer) return <div className="app"><div className="screen"><div className="pad" role="status">Загружаем твою игру…</div></div></div>
  if (playerState === 'failed') return <div className="app"><div className="screen"><div className="pad"><p>Не получилось загрузить игру.</p><button className="btn" onClick={() => location.reload()}>Попробовать снова</button></div></div></div>
  const people: React.ContextType<typeof AvatarPeople> = {}
  if (player) {
    people[player.handle] = { address: player.address, ageBand: player.ageBand, personalizeAvatar }
    people[player.id] = people[player.handle]
  }
  const trainingScreen = learning && !top.id.startsWith('06-') && !['01-1_invite', '01-2_age', '01-2b_under14', '05-8_settings'].includes(top.id)
  const tab = trainingScreen ? TAB_OF[top.id] ?? (onboarding?.phase === 'setup' ? 5 : 0) : top.id === '01-3_world' && !player ? undefined : TAB_OF[top.id]
  return (
    <NavContext.Provider value={nav}>
      <AvatarWorld.Provider value={(top.params.world as WorldId) ?? player?.world ?? 'edfccf53-65c6-4eaf-8321-2391bc1758bd'}>
      <AvatarPeople.Provider value={people}><div className="app">
        {isGameOnboardingPreview() && <div className="game-onboarding-preview-label">DEV-обучение · действия сохраняются в игре <button onClick={() => restartGameOnboardingPreview(player?.worldInstanceId ?? player?.world)}>Начать просмотр заново</button><button onClick={() => { restartGameOnboardingPreview(); nav.push('01-3_world') }}>С выбора мира</button></div>}
        <div key={top.id} className={top.id === '01-3_world' ? 'worlds-arrival' : undefined}><Screen /></div><WorldUnlockCelebration /><GameOnboarding lockedSection={lockedSection} />
        {tab !== undefined && (
          <nav className="tabbar" aria-label="Основная навигация">
            {[5, 0, 1, 2, 3, 4].map(i => { const t = TABS[i]; const locked = learning && (!player || i !== 4 && !(i === 5 && top.id === '01-3_world') && !(i === 0 && onboarding?.phase !== 'setup')); return (i === 2 && !runtimeConfig.features.dm) || (i === 3 && !runtimeConfig.features.friends) ? null : <button key={t.title} aria-current={i === tab ? 'page' : undefined} aria-disabled={locked || undefined} className={`${i === tab ? 'on' : ''}${locked ? ' locked' : ''}`} onClick={() => { if (locked) { setLockedSection(true); return } setLockedSection(false); if (top.id === t.screen) return; tap('tab', { to: t.screen }); nav.tab(i) }}><b>{t.icon}</b>{t.title}</button> })}
          </nav>
        )}
      </div>
      </AvatarPeople.Provider></AvatarWorld.Provider>
    </NavContext.Provider>
  )
}

export function App() {
  const authenticated = useSyncExternalStore(subscribeAuth, hasAccess)
  const registration = useSyncExternalStore(subscribeAuth, needsRegistration)
  const pendingLogin = useSyncExternalStore(subscribeAuth, hasPendingLogin)
  const resumable = useSyncExternalStore(subscribeAuth, canResumeAuth)
  const [ready, setReady] = useState(false)
  const [busy, setBusy] = useState(false)
  const [authError, setAuthError] = useState('')
  const explicitStart = hasExplicitStart()
  const [landingAccepted, setLandingAccepted] = useState(landingEntryPending)
  const [landingPreview, setLandingPreview] = useState(isLandingPreview)
  const showLanding = !explicitStart && (landingPreview || ready && !authenticated && !registration && !resumable && !landingAccepted)
  const showAuth = !showLanding && !explicitStart && ready && !authenticated && !registration
  useEffect(() => {
    if (showAuth && landingAuthPending()) trackLandingAuth('landing_auth_view')
  }, [showAuth])
  useEffect(() => { if (registration && !isGameOnboardingPreview()) beginGameOnboarding() }, [registration])
  useEffect(() => {
    if (!isGameOnboardingPreview() && (authenticated || registration)) {
      setLandingAccepted(true)
      if (!landingPreview && completeLandingAuth()) {
        trackLanding('landing_auth_complete')
        trackLandingAuth('landing_auth_verified', { auth_outcome: registration ? 'registration' : 'access' })
      }
    }
  }, [authenticated, registration, landingPreview])
  useEffect(() => {
    if (explicitStart) return
    const provider = pendingAuthProvider()
    if (landingAuthPending() && canResumeAuth()) trackLandingAuth('landing_auth_resume', { auth_provider: provider, auth_action: 'automatic' })
    void startAuth().catch(() => {
      if (landingAuthPending()) trackLandingAuth('landing_auth_exchange_error', { auth_provider: provider })
      setAuthError('Нет соединения с игрой. Проверь подключение и продолжи вход.')
    }).finally(() => setReady(true))
  }, [explicitStart])
  useEffect(() => { if (ready && !authenticated && api.hasPlayer()) api.clearSession() }, [ready, authenticated])
  const finishLogin = (request: Promise<unknown>, provider?: AuthProvider, providerReturned = false) => {
    if (provider && providerReturned && landingAuthPending()) trackLandingAuth('landing_auth_provider_result', { auth_provider: provider, auth_outcome: 'proof' })
    setAuthError(''); setBusy(true)
    void request.catch(() => {
      if (landingAuthPending()) trackLandingAuth('landing_auth_exchange_error', { auth_provider: provider })
      setAuthError(hasPendingLogin()
        ? 'Подтверждение входа сохранено. Проверь соединение с игрой и продолжи вход.'
        : 'Не получилось войти. Проверь подключение и попробуй снова.')
    }).finally(() => setBusy(false))
  }
  if (showLanding) return <Landing preview={landingPreview} onContinue={worldId => {
    if (!landingPreview) {
      beginLandingAuth(worldId)
      if (worldId) trackLanding('landing_continue')
      else trackLandingAuth('landing_login_click')
    }
    setLandingAccepted(true); setLandingPreview(false)
  }} />
  if (!explicitStart && !ready) return <div className="app"><div className="screen"><div className="pad" role="status">Входим в игру…</div></div></div>
  if (showAuth) return <TelegramLaunch error={authError} pendingProvider={pendingLogin ? pendingAuthProvider() === 'yandex' ? 'Яндекс ID' : pendingAuthProvider() === 'vk' ? 'VK' : 'Telegram' : undefined}>
    {resumable ? <div className="auth-providers">
      <button className="btn" disabled={busy} onClick={() => {
        const provider = pendingAuthProvider()
        if (landingAuthPending()) trackLandingAuth('landing_auth_resume', { auth_provider: provider, auth_action: 'manual' })
        finishLogin(startAuth(), provider)
      }}>{busy ? 'Входим…' : 'Продолжить вход'}</button>
      {pendingLogin && <button className="btn secondary" disabled={busy} onClick={() => {
        if (landingAuthPending()) trackLandingAuth('landing_auth_resume', { auth_provider: pendingAuthProvider(), auth_action: 'restart' })
        clearPendingLogin(); setAuthError('')
      }}>Войти заново</button>}
    </div> : <AuthProviders busy={busy}
      onMiniApp={hasInitData() ? () => finishLogin(loginMiniApp(), 'telegram') : undefined}
      onTelegram={token => finishLogin(loginWidget(token), 'telegram', true)}
      onVk={(token, expiry) => finishLogin(loginVk(token, expiry), 'vk', true)}
      onYandex={(token, expiry) => finishLogin(loginYandex(token, expiry), 'yandex', true)}
      onSelected={(provider, action) => {
        if (landingAuthPending()) trackLandingAuth('landing_auth_provider_selected', { auth_provider: provider, auth_action: action })
      }}
      onError={(provider, outcome) => {
        if (landingAuthPending()) trackLandingAuth('landing_auth_provider_result', { auth_provider: provider, auth_outcome: outcome })
        setBusy(false); setAuthError('Вход не завершён. Попробуй ещё раз.')
      }} />}
  </TelegramLaunch>
  return <StoreProvider><Shell /></StoreProvider>
}
