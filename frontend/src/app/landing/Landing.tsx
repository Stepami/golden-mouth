import { useEffect, useRef, useState } from 'react'
import { api } from '../../api/client'
import { worlds as templates } from '../../api/worldContent'
import type { World } from '../../api/types'
import { Button } from '../../ui'
import { screenView, trackLanding } from '../analytics'
import { assignLanding, requestedVariant } from './experiment'
import './landing.css'

const worlds = [
  { id: 'infobiz', templateId: '5720946a-ea7c-4a10-9c50-51ce3deefc53', title: 'Последние инфобизеры' },
  { id: 'evening', templateId: 'b3a1a522-0263-4796-a197-57a8731fc291', title: 'Вечерело' },
  { id: 'hogwarts', templateId: 'aec9494b-4118-4525-8458-e85d759c8dd9', title: 'Спасти Хогвартс' },
  { id: 'houses', templateId: '8c02a7fb-3b55-4eb0-b957-1b4869ed9834', title: 'Игра домов' },
] as const

export function Landing({ onContinue, preview = false }: { onContinue: (worldId: string) => void; preview?: boolean }) {
  const [variant] = useState(() => preview ? requestedVariant() ?? 'hogwarts' : assignLanding())
  const [selected, setSelected] = useState<string>(variant === 'hogwarts' ? worlds[2].templateId : worlds[0].templateId)
  const [available, setAvailable] = useState<World[]>([])
  const [error, setError] = useState(false)
  const [attempt, setAttempt] = useState(0)
  useEffect(() => {
    let active = true
    setError(false)
    void api.loadWorldTemplates().then(() => { if (active) setAvailable([...templates]) }).catch(() => { if (active) setError(true) })
    return () => { active = false }
  }, [attempt])
  const chosen = available.find(world => world.id === selected)
  const viewed = useRef(false)
  const clicked = useRef(false)
  const heading = useRef<HTMLHeadingElement>(null)
  useEffect(() => {
    heading.current?.focus({ preventScroll: true })
    if (preview || viewed.current) return
    viewed.current = true
    screenView(`landing_${variant}`)
    trackLanding('landing_view')
  }, [variant, preview])
  const start = () => {
    if (clicked.current || !chosen) return
    clicked.current = true
    onContinue(chosen.id)
  }
  return <main className={`entry-landing entry-landing--${variant}`} aria-labelledby="landing-title">
    {variant === 'hogwarts' && <img className="landing-hero-art" src="/landing/hogwarts-960.jpg" srcSet="/landing/hogwarts-480.jpg 480w, /landing/hogwarts-960.jpg 960w" sizes="(max-width: 430px) 100vw, 430px" alt="Озадаченный волшебник перед закрытым замком, ограждённым лентой; туристы фотографируют ворота" fetchPriority="high" />}
    <header className="landing-brand"><img src="/android-chrome-192x192.png?v=wave-2" width="42" height="42" alt="" /><span>Реки</span></header>
    {variant === 'worlds' ? <>
      <section className="landing-copy"><h1 ref={heading} id="landing-title" tabIndex={-1}>Твой блог.<br />Твоя история.</h1><p>Выбери героя, пиши посты и смотри, как отвечает его мир.</p></section>
      <section className="landing-mosaic" aria-label="Игровые миры">{worlds.map(world => <button type="button" className="landing-world" key={world.id} aria-pressed={selected === world.templateId} onClick={() => setSelected(world.templateId)}><img src={`/landing/${world.id}-480.jpg`} width="480" height="720" alt="" fetchPriority={world.id === 'infobiz' ? 'high' : 'auto'} /><strong>{world.title}</strong></button>)}</section>
      <p className="landing-world-summary" aria-live="polite">{chosen?.summary ?? (error ? '' : available.length ? 'Этот мир сейчас недоступен. Выбери другой.' : 'Загружаем миры…')}</p>
    </> : <>
      <section className="landing-hero-copy"><div className="landing-eyebrow">Спасти Хогвартс</div><h1 ref={heading} id="landing-title" tabIndex={-1}>Хогвартс закрыт.<br />Спаси его от банкротства.</h1><p>Выбери героя. Веди его блог.<br />Решай, кому верить.</p></section>
      <section className="landing-other-worlds" aria-label="Другие игровые миры">{worlds.filter(world => world.id !== 'hogwarts').map(world => <div key={world.id}><img src={`/landing/${world.id}-480.jpg`} width="480" height="720" alt="" /><span>{world.id === 'infobiz' ? 'Инфобизеры' : world.title}</span></div>)}</section>
    </>}
    <footer className="landing-actions">{error && <div role="alert" className="landing-load-error"><p>Не получилось загрузить миры.</p><Button secondary onClick={() => setAttempt(value => value + 1)}>Попробовать снова</Button></div>}{!error && variant === 'hogwarts' && !chosen && <p role="status">{available.length ? 'Этот мир сейчас недоступен.' : 'Загружаем мир…'}</p>}<Button onClick={start} disabled={!chosen}>Начать играть</Button><p>Твои собеседники — AI-персонажи</p></footer>
  </main>
}
