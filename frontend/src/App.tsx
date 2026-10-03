import { useCallback, useEffect, useMemo, useState } from 'react'
import { NavContext, TABS, type Nav, type ScreenId } from './app/nav'
import { StoreProvider, useStore } from './app/store'
import { SCREENS, TAB_OF } from './screens'
import { syncBackButton } from './telegram'

type Entry = { id: ScreenId; params: Record<string, unknown> }

/** ?screen=<код> открывает любой экран сразу (сравнение с docs/screens), ?gallery показывает список. */
function startEntry(): Entry {
  const q = new URLSearchParams(location.search)
  const s = q.get('screen')
  if (q.has('gallery')) return { id: 'gallery', params: {} }
  if (s && SCREENS[s]) return { id: s, params: {} }
  return { id: '01-1_invite', params: {} }
}

function Shell() {
  const [stack, setStack] = useState<Entry[]>(() => [startEntry()])
  const top = stack[stack.length - 1]
  const { player } = useStore()

  const nav: Nav = useMemo(() => ({
    current: top.id, params: top.params, canBack: stack.length > 1,
    push: (id, params = {}) => { setStack(s => [...s, { id, params }]); window.scrollTo(0, 0) },
    replace: (id, params = {}) => { setStack(s => [...s.slice(0, -1), { id, params }]); window.scrollTo(0, 0) },
    back: () => setStack(s => (s.length > 1 ? s.slice(0, -1) : s)),
    tab: i => { setStack([{ id: TABS[i].screen, params: {} }]); window.scrollTo(0, 0) },
  }), [top, stack.length])

  const back = useCallback(() => nav.back(), [nav])
  useEffect(() => syncBackButton(stack.length > 1, back), [stack.length, back])

  const Screen = SCREENS[top.id]?.component
  if (!player || !Screen) return <div className="app" />
  const tab = TAB_OF[top.id]
  return (
    <NavContext.Provider value={nav}>
      <div className="app">
        <Screen />
        {tab !== undefined && (
          <nav className="tabbar">
            {TABS.map((t, i) => <button key={t.title} className={i === tab ? 'on' : ''} onClick={() => nav.tab(i)}><b>{t.icon}</b>{t.title}</button>)}
          </nav>
        )}
      </div>
    </NavContext.Provider>
  )
}

export function App() {
  return <StoreProvider><Shell /></StoreProvider>
}
