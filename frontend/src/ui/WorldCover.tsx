import { useEffect, useRef, useState } from 'react'
import { worldArt } from '../app/worldPrototype'
import { trackImage } from '../app/imageTiming'

export const catalogCoverSizes = '(min-width: 430px) 342px, calc((100vw - 32px) * 0.86)'
const fullCoverSizes = '(min-width: 430px) 430px, 100vw'
export const coverSrcSet = (world: string) => `${worldArt[world].smallCover} ${worldArt[world].smallCoverWidth ?? 640}w, ${worldArt[world].cover} 960w`

const catalogPhotos: Record<string, string> = {
  'b3a1a522-0263-4796-a197-57a8731fc291': '/landing/evening-480.jpg',
  'aec9494b-4118-4525-8458-e85d759c8dd9': '/landing/hogwarts-480.jpg',
  '8c02a7fb-3b55-4eb0-b957-1b4869ed9834': '/landing/houses-480.jpg',
  '5720946a-ea7c-4a10-9c50-51ce3deefc53': '/landing/infobiz-480.jpg',
}

/** Starts alongside templates, not after the API/React waterfall. */
export function preloadCatalogCover() {
  const image = new Image()
  image.fetchPriority = 'high'
  image.src = catalogPhotos['b3a1a522-0263-4796-a197-57a8731fc291']
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
  const photo = catalog ? catalogPhotos[world] : undefined
  if (!art) return null
  return <div ref={root} className={`world-art ${className}`} aria-busy={!loaded}>
    {visible && <img src={photo ?? art.cover} srcSet={photo ? undefined : coverSrcSet(world)} style={photo === '/landing/infobiz-480.jpg' ? { objectPosition: '20% center' } : undefined} sizes={catalog ? catalogCoverSizes : fullCoverSizes} alt={alt} decoding="async" fetchPriority={priority ? 'high' : 'auto'} className={loaded ? 'loaded' : ''} onLoad={e => { setLoaded(true); trackImage(e.currentTarget, `cover_${world}`) }} onError={() => trackImage(null, `cover_${world}`)} />}
  </div>
}
