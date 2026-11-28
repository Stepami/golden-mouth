import { useEffect, useRef, useState } from 'react'
import { worldArt } from '../app/worldPrototype'
import { trackImage } from '../app/imageTiming'

import './world-layout.css'

const catalogCoverSizes = '(min-width: 1051px) 310px, (min-width: 621px) 48vw, 86vw'
const fullCoverSizes = '(min-width: 740px) 700px, 100vw'

/** Starts alongside templates, not after the API/React waterfall. */
export function preloadCatalogCover() {
  const image = new Image()
  image.fetchPriority = 'high'
  const art = worldArt['b3a1a522-0263-4796-a197-57a8731fc291']
  image.sizes = catalogCoverSizes
  image.srcset = `${art.smallCover} 480w, ${art.cover} 960w`
  image.src = art.smallCover
}

export function WorldCover({ world, className = '', alt = '', catalog = false, priority = false }: { world: string; className?: string; alt?: string; catalog?: boolean; priority?: boolean }) {
  const root = useRef<HTMLDivElement>(null)
  const [visible, setVisible] = useState(!catalog || priority)
  const [loaded, setLoaded] = useState(false)
  useEffect(() => {
    if (visible || !root.current) return
    if (!('IntersectionObserver' in window)) { setVisible(true); return }
    const observer = new IntersectionObserver(entries => {
      if (entries.some(entry => entry.isIntersecting)) { setVisible(true); observer.disconnect() }
    }, { rootMargin: '80px' })
    observer.observe(root.current)
    return () => observer.disconnect()
  }, [visible])
  const art = worldArt[world]
  if (!art) return null
  return <div ref={root} className={`world-art ${className}`} aria-busy={!loaded}>
    {visible && <img src={art.cover} srcSet={`${art.smallCover} 480w, ${art.cover} 960w`} style={{ objectPosition: art.focus }} sizes={catalog ? catalogCoverSizes : fullCoverSizes} alt={alt} decoding="async" fetchPriority={priority ? 'high' : 'auto'} className={loaded ? 'loaded' : ''} onLoad={e => { setLoaded(true); trackImage(e.currentTarget, `cover_${world}`) }} onError={() => trackImage(null, `cover_${world}`)} />}
  </div>
}
