import { useCallback, useEffect, useMemo, useState } from 'react'
import { NavContext, TABS, type Nav, type ScreenId } from './app/nav'
import { StoreProvider, useStore } from './app/store'
import { SCREENS, TAB_OF } from './screens'
import { screenView, setExperiments, tap } from './app/analytics'
import { setScreen } from './app/log'
import { TelegramLaunch } from './app/TelegramLaunch'
import { inTelegram, syncBackButton } from './telegram'
import { api } from './api/client'
import { runtimeConfig } from './app/runtimeConfig'
import { AvatarWorld } from './ui/avatar'
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
  const explicitStart = hasExplicitStart()

  const nav: Nav = useMemo(() => ({
    current: top.id, params: top.params, canBack: stack.length > 1,
    push: (id, params = {}) => { setStack(s => [...s, { id, params }]); window.scrollTo(0, 0) },
    replace: (id, params = {}) => { setStack(s => [...s.slice(0, -1), { id, params }]); window.scrollTo(0, 0) },
    back: () => setStack(s => (s.length > 1 ? s.slice(0, -1) : s)),
    tab: i => { setStack([{ id: TABS[i].screen, params: {} }]); window.scrollTo(0, 0) },
  }), [top, stack.length])

  useEffect(() => { setScreen(top.id); screenView(top.id) }, [top.id])
  useEffect(() => { if (player?.experiments) setExperiments(player.experiments) }, [player?.experiments])
  useEffect(() => {
    if (playerState !== 'ready' || explicitStart || top.id !== '01-1_invite') return
    let current = true
    api.channelPosts()
      .then(posts => { if (current) setStack([{ id: posts.length === 0 ? '01-6_home' : '03-5_stage', params: {} }]) })
      .catch(() => { if (current) setStack([{ id: '03-5_stage', params: {} }]) })
    return () => { current = false }
  }, [explicitStart, playerState, top.id])
  const back = useCallback(() => nav.back(), [nav])
  useEffect(() => syncBackButton(stack.length > 1, back), [stack.length, back])

  const Screen = SCREENS[top.id]?.component
  const resolvingExistingPlayer = playerState === 'ready' && !explicitStart && top.id === '01-1_invite'
  if (!Screen || playerState === 'loading' || resolvingExistingPlayer) return <div className="app"><div className="screen"><div className="pad" role="status">Загружаем твою игру…</div></div></div>
  if (playerState === 'failed') return <div className="app"><div className="screen"><div className="pad"><p>Не получилось загрузить игру.</p><button className="btn" onClick={() => location.reload()}>Попробовать снова</button></div></div></div>
  const tab = TAB_OF[top.id]
  return (
    <NavContext.Provider value={nav}>
      <AvatarWorld.Provider value={(top.params.world as WorldId) ?? player?.world ?? 'stage'}>
      <div className="app">
        <Screen />
        {tab !== undefined && (
          <nav className="tabbar">
            {TABS.map((t, i) => (i === 2 && !runtimeConfig.features.dm) || (i === 3 && !runtimeConfig.features.friends) ? null : <button key={t.title} className={i === tab ? 'on' : ''} onClick={() => { tap('tab', { to: t.screen }); nav.tab(i) }}><b>{t.icon}</b>{t.title}</button>)}
          </nav>
        )}
      </div>
      </AvatarWorld.Provider>
    </NavContext.Provider>
  )
}

export function App() {
  if (!inTelegram && !hasExplicitStart()) return <TelegramLaunch />
  return <StoreProvider><Shell /></StoreProvider>
}
