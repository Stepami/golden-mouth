import { SCREENS } from '.'
import { useNav } from '../app/nav'
import { TopBar } from '../ui'

/** Служебный список всех экранов: открыть любой и сравнить с макетом в docs/screens. */
export function Gallery() {
  const nav = useNav()
  return (
    <div className="screen">
      <TopBar title="Экраны Рек" right={`${Object.keys(SCREENS).length - 1}`} />
      <div className="pad">
        {Object.entries(SCREENS).filter(([k]) => k !== 'gallery').sort().map(([k, s]) => (
          <button key={k} className="opt" onClick={() => nav.push(k)}><b>{k}</b> <span className="muted small">{s.title}</span></button>
        ))}
      </div>
    </div>
  )
}
