import { preloadCatalogCover } from '../ui/WorldCover'
import { useEffect, useState, type ComponentType } from 'react'
import { api } from '../api/client'

/** Load templates only when a world creation screen opens. */
export function withWorldTemplates(Screen: ComponentType, catalog = false): ComponentType {
  return function WorldTemplatesScreen() {
    const [state, setState] = useState<'loading' | 'ready' | 'failed'>('loading')
    const [attempt, setAttempt] = useState(0)
    useEffect(() => {
      let active = true
      setState('loading')
      if (catalog) preloadCatalogCover()
      api.loadWorldTemplates().then(
        () => { if (active) setState('ready') },
        () => { if (active) setState('failed') },
      )
      return () => { active = false }
    }, [attempt])
    if (state === 'ready') return <Screen />
    return <div className="screen no-tab"><div className="pad">
      {state === 'loading'
        ? <div role="status">Загружаем миры…</div>
        : <><p role="alert">Не получилось загрузить миры.</p><button className="btn" onClick={() => setAttempt(value => value + 1)}>Попробовать снова</button></>}
    </div></div>
  }
}
