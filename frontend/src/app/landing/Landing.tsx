import { useEffect, useRef, useState } from 'react'
import { Button } from '../../ui'
import { screenView, trackLanding } from '../analytics'
import { assignLanding, requestedVariant } from './experiment'
import './landing.css'

const worlds = [
  { id: 'infobiz', title: 'Последние инфобизеры' },
  { id: 'evening', title: 'Вечерело' },
  { id: 'hogwarts', title: 'Спасти Хогвартс' },
  { id: 'houses', title: 'Игра домов' },
] as const

export function Landing({ onContinue, preview = false }: { onContinue: () => void; preview?: boolean }) {
  const [variant] = useState(() => preview ? requestedVariant() ?? 'worlds' : assignLanding())
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
    if (clicked.current) return
    clicked.current = true
    onContinue()
  }
  return <main className={`entry-landing entry-landing--${variant}`} aria-labelledby="landing-title">
    {variant === 'hogwarts' && <img className="landing-hero-art" src="/landing/hogwarts-960.jpg" srcSet="/landing/hogwarts-480.jpg 480w, /landing/hogwarts-960.jpg 960w" sizes="(max-width: 430px) 100vw, 430px" alt="Озадаченный волшебник перед закрытым замком, ограждённым лентой; туристы фотографируют ворота" fetchPriority="high" />}
    <header className="landing-brand"><img src="/android-chrome-192x192.png?v=wave-2" width="42" height="42" alt="" /><span>Реки</span></header>
    {variant === 'worlds' ? <>
      <section className="landing-copy"><h1 ref={heading} id="landing-title" tabIndex={-1}>Твой блог.<br />Твоя история.</h1><p>Выбери героя, пиши посты и смотри, как отвечает его мир.</p></section>
      <section className="landing-mosaic" aria-label="Игровые миры">{worlds.map(world => <div className="landing-world" key={world.id}><img src={`/landing/${world.id}-480.jpg`} width="480" height="720" alt="" fetchPriority={world.id === 'infobiz' ? 'high' : 'auto'} /><strong>{world.title}</strong></div>)}</section>
    </> : <>
      <section className="landing-hero-copy"><div className="landing-eyebrow">Спасти Хогвартс</div><h1 ref={heading} id="landing-title" tabIndex={-1}>Хогвартс закрыт.<br />Твоя история — нет.</h1><p>Выбери героя. Веди его блог.<br />Решай, кому верить.</p></section>
      <section className="landing-other-worlds" aria-label="Другие игровые миры">{worlds.filter(world => world.id !== 'hogwarts').map(world => <div key={world.id}><img src={`/landing/${world.id}-480.jpg`} width="480" height="720" alt="" /><span>{world.id === 'infobiz' ? 'Инфобизеры' : world.title}</span></div>)}</section>
    </>}
    <footer className="landing-actions"><Button onClick={start}>Начать играть</Button><p>Твои собеседники — AI-персонажи</p></footer>
  </main>
}
