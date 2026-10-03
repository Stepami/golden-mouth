import { useState } from 'react'

// Пять вкладок как в макетах: Канал, Лента, Личка, Друзья, Я. Экраны живут в src/screens/<код экрана>.
const TABS = [['▦', 'Канал'], ['≋', 'Лента'], ['✉', 'Личка'], ['☺', 'Друзья'], ['★', 'Я']] as const

export function App() {
  const [tab, setTab] = useState(0)
  return (
    <div className="app">
      <main className="screen"><h1>{TABS[tab][1]}</h1></main>
      <nav className="tabs">
        {TABS.map(([icon, title], i) => (
          <button key={title} className={i === tab ? 'on' : ''} onClick={() => setTab(i)}>
            <b>{icon}</b>{title}
          </button>
        ))}
      </nav>
    </div>
  )
}
