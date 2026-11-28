import type { ReactNode } from 'react'
import { optionalWorldTemplate, worldOf } from '../api/worldContent'
import { useGuestSession } from '../app/landing/guestSession'
import { WorldCover } from './WorldCover'

export function WorldContext({ world, children }: { world: string; children?: ReactNode }) {
  const guest = useGuestSession()
  const definition = guest?.world ?? worldOf(world)
  const cover = guest ? world : optionalWorldTemplate(definition)?.id ?? world
  return <aside className="game-context" aria-label="Контекст мира">
    <section className="game-world-summary card">
      <WorldCover world={cover} catalog />
      <div><h2>{definition.title}</h2>{definition.stars.length > 0 && <p className="muted small">{definition.stars.slice(0, 3).map(star => star.name).join(' · ')}</p>}</div>
    </section>
    {children}
  </aside>
}
