
import { useCallback, useEffect, useMemo, useState, useSyncExternalStore } from 'react'
import { NavContext, TABS, type Nav, type ScreenId } from './app/nav'

import { UnseenResults } from './ui/UnseenResults'

import { StoreProvider, useStore } from './app/store'
import { SCREENS, TAB_OF } from './screens'
import { screenView, setExperiments, tap } from './app/analytics'
import { setScreen } from './app/log'
import { TelegramLaunch } from './app/TelegramLaunch'
import { inTelegram, syncBackButton } from './telegram'

import { api } from './api/client'
import { canResumeAuth, clearPendingWidgetLogin, hasAccess, hasInitData, hasPendingWidgetLogin, loadTelegramClientId, loginMiniApp, loginWidget, startAuth, subscribeAuth } from './api/auth'
import { openTelegramLogin, preloadTelegramLogin } from './api/telegramLogin'

import { Avatar } from './ui'
import { Icon } from './ui/Icon'

import { WorldUnlockCelebration } from './app/WorldUnlockCelebration'
import { PublicationFloat } from './ui/PublicationFloat'
import { AvatarWorld, AvatarPeople } from './ui/avatar'
import { useAvatarPersonalization } from './app/avatarPersonalization'
import type { WorldId } from './api/types'

type Entry = { id: ScreenId; params: Record<string, unknown> }

/** ?screen=<код> открывает любой экран сразу (сравнение с docs/screens), ?gallery показывает список. */
function startEntry(): Entry {
  const q = new URLSearchParams(location.search)
  const s = q.get('screen')
  if (q.has('gallery')) return { id: 'gallery', params: {} }
  if (s && SCREENS[s]) return { id: s, params: q.has('world') ? { world: q.get('world') } : {} }
  return { id: '01-1_invite', params: {} }
}

function hasExplicitStart(): boolean {
  const q = new URLSearchParams(location.search)
  return q.has('screen') || q.has('gallery')
}

function Shell() {
  const [stack, setStack] = useState<Entry[]>(() => [startEntry()])
  const top = stack[stack.length - 1]
  const { player, playerState } = useStore()
  const personalizeAvatar = useAvatarPersonalization(player?.id ?? 'onboarding')
  const explicitStart = hasExplicitStart()

  const nav: Nav = useMemo(() => ({
    current: top.id, params: top.params, canBack: stack.length > 1,
    push: (id, params = {}) => { setStack(s => [...s, { id, params }]); window.scrollTo(0, 0) },
    replace: (id, params = {}) => { setStack(s => [...s.slice(0, -1), { id, params }]); window.scrollTo(0, 0) },
    reset: (id, params = {}) => { setStack([{ id, params }]); window.scrollTo(0, 0) },
    back: () => setStack(s => (s.length > 1 ? s.slice(0, -1) : s)),
    tab: i => { setStack([{ id: ['03-5_stage', '03-3_feed', '03-1_dm', '03-4_friends', '05-5_collection', '01-3_world'][i], params: {} }]); window.scrollTo(0, 0) },
  }), [top, stack.length])

  useEffect(() => { setScreen(top.id); screenView(top.id) }, [top.id])
  useEffect(() => { if (player?.experiments) setExperiments(player.experiments) }, [player?.experiments])
  useEffect(() => {
    if (playerState !== 'ready' || explicitStart || top.id !== '01-1_invite') return
    setStack([{ id: '01-3_world', params: {} }])
  }, [explicitStart, playerState, top.id])
  const back = useCallback(() => nav.back(), [nav])
  useEffect(() => syncBackButton(stack.length > 1 && top.id !== '02-6_thread', back), [stack.length, top.id, back])

  const Screen = SCREENS[top.id]?.component
  const resolvingExistingPlayer = playerState === 'ready' && !explicitStart && top.id === '01-1_invite'
  if (!Screen || playerState === 'loading' || resolvingExistingPlayer) return <div className="app"><div className="screen app-boot" role="status"><div className="boot-emblem" aria-hidden="true">✦</div><h1>Реки</h1><p>Открываем твои миры</p><div className="boot-track" aria-hidden="true"><i /></div></div></div>
  const publicScreen = ['01-1_invite', '01-2_age', '01-2b_under14', '01-2c_guide', '01-3_world', '01-3b_world_overview', '01-4_channel', '01-4b_world_setup', '01-5_first_star', '05-12_world_advanced'].includes(top.id)
  if (playerState === 'unauthorized' && !publicScreen) return <div className="app"><div className="screen"><div className="pad"><p>Для сохранения прогресса открой Реки в Telegram.</p><button className="btn" onClick={() => nav.replace('01-3_world')}>Посмотреть миры</button></div></div></div>
  if (playerState === 'failed') return <div className="app"><div className="screen"><div className="pad"><p>Не получилось загрузить игру.</p><button className="btn" onClick={() => location.reload()}>Попробовать снова</button></div></div></div>
  const people: React.ContextType<typeof AvatarPeople> = {}
  if (player) {
    people[player.handle] = { address: player.address, ageBand: player.ageBand, personalizeAvatar }
    people[player.id] = people[player.handle]
  }
  const resultScreen = ['02-3_result', '02-4_flop', '02-5_viral'].includes(top.id)
  const ResultBackground = SCREENS['03-5_stage']?.component
  const tab = resultScreen ? 0 : top.id === '01-3_world' && !player ? undefined : TAB_OF[top.id]
  return (
    <NavContext.Provider value={nav}>
      <AvatarWorld.Provider value={(top.params.world as WorldId) ?? player?.world ?? 'edfccf53-65c6-4eaf-8321-2391bc1758bd'}>
      <AvatarPeople.Provider value={people}><div className="app">
        <div key={top.id} className={top.id === '01-3_world' ? 'worlds-arrival' : undefined}>{resultScreen && ResultBackground ? <><ResultBackground /><PublicationFloat /></> : <Screen />}</div><WorldUnlockCelebration />{player && <UnseenResults />}
        {tab !== undefined && (
          <nav className="tabbar" aria-label="Основная навигация">
            <button aria-label="Миры" aria-current={top.id === '01-3_world' ? 'page' : undefined} className={top.id === '01-3_world' ? 'on' : ''} onClick={() => { tap('tab', { to: '01-3_world' }); nav.tab(5) }}><Icon name="home" /></button>
            <button aria-label="Лента" aria-current={['03-3_feed', '03-5_stage', '02-6_thread'].includes(top.id) ? 'page' : undefined} className={['03-3_feed', '03-5_stage', '02-6_thread'].includes(top.id) ? 'on' : ''} onClick={() => { tap('tab', { to: '03-3_feed' }); nav.tab(1) }}><Icon name="message" /></button>
            <button className="tabbar-compose" aria-label="Новая запись" onClick={() => nav.push('02-1_compose')}><Icon name="plus" size={30} /></button>
            <button aria-label="История и навыки" aria-current={top.id === '05-13_progress' ? 'page' : undefined} className={top.id === '05-13_progress' ? 'on' : ''} onClick={() => { tap('tab', { to: '05-13_progress' }); nav.reset('05-13_progress') }}><Icon name="chart" /></button>
            <button aria-label="Мой профиль" aria-current={top.id === '05-5_collection' ? 'page' : undefined} className={top.id === '05-5_collection' ? 'on' : ''} onClick={() => { tap('tab', { to: '05-5_collection' }); nav.tab(4) }}><Avatar seed={player?.handle} label="Я" size={30} /></button>
          </nav>
        )}
      </div>
      </AvatarPeople.Provider></AvatarWorld.Provider>
    </NavContext.Provider>
  )
}

export function App() {
  const authenticated = useSyncExternalStore(subscribeAuth, hasAccess)
  const pendingLogin = useSyncExternalStore(subscribeAuth, hasPendingWidgetLogin)
  const resumable = useSyncExternalStore(subscribeAuth, canResumeAuth)
  const [ready, setReady] = useState(false)
  const [telegramClientId, setTelegramClientId] = useState<number | null>(null)
  const [prepareAttempt, setPrepareAttempt] = useState(0)
  const [prepareFailed, setPrepareFailed] = useState(false)
  const [busy, setBusy] = useState(false)
  const [authError, setAuthError] = useState('')
  const explicitStart = hasExplicitStart()
  useEffect(() => {
    if (explicitStart) return
    void startAuth().catch(() => { setAuthError('Нет соединения с игрой. Проверь подключение и продолжи вход.') }).finally(() => setReady(true))
  }, [explicitStart])
  useEffect(() => {
    if (!ready || authenticated || resumable || inTelegram && hasInitData() || explicitStart) return
    let active = true
    setPrepareFailed(false)
    void Promise.all([preloadTelegramLogin(), loadTelegramClientId()]).then(([, clientId]) => {
      if (active) setTelegramClientId(clientId)
    }).catch(() => {
      if (active) { setPrepareFailed(true); setAuthError('Не удалось подготовить вход Telegram. Проверь подключение и попробуй снова.') }
    })
    return () => { active = false }
  }, [ready, authenticated, resumable, explicitStart, prepareAttempt])
  useEffect(() => { if (ready && !authenticated && api.hasPlayer()) api.clearSession() }, [ready, authenticated])
  const finishLogin = (request: Promise<unknown>) => {
    void request.catch(() => {
      setAuthError(hasPendingWidgetLogin()
        ? 'Подтверждение Telegram сохранено. Проверь соединение с игрой и продолжи вход.'
        : 'Не получилось войти. Проверь подключение и попробуй снова.')
    }).finally(() => setBusy(false))
  }
  const onLogin = () => {
    if (resumable) {
      setAuthError(''); setBusy(true)
      finishLogin(startAuth())
      return
    }
    if (prepareFailed) {
      setAuthError(''); setTelegramClientId(null); setPrepareAttempt(value => value + 1)
      return
    }
    if (!(inTelegram && hasInitData()) && telegramClientId === null) return
    setAuthError('')
    setBusy(true)
    if (inTelegram && hasInitData()) {
      finishLogin(loginMiniApp())
    } else {
      openTelegramLogin(telegramClientId!,
        idToken => finishLogin(loginWidget(idToken)),
        () => { setBusy(false); setAuthError('Вход через Telegram не завершён.') },
      )
    }
  }
  const restartLogin = () => { clearPendingWidgetLogin(); setAuthError('') }
  if (!explicitStart && !ready) return <div className="app"><div className="screen"><div className="pad" role="status">Входим в игру…</div></div></div>
  if (!explicitStart && !authenticated) return <TelegramLaunch onLogin={onLogin} onRestart={restartLogin} pending={pendingLogin} resumable={resumable} prepareFailed={prepareFailed} busy={busy} ready={resumable || prepareFailed || inTelegram && hasInitData() || telegramClientId !== null} error={authError} />
  return <StoreProvider><Shell /></StoreProvider>
}
