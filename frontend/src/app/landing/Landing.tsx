import { useEffect, useRef, useState } from 'react'
import { api } from '../../api/client'
import { worlds as templates } from '../../api/worldContent'
import type { World } from '../../api/types'
import { Button } from '../../ui'
import { screenView, trackLanding, trackLandingAuth } from '../analytics'
import { assignLanding, requestedVariant, isStoryLanding } from './experiment'
import './landing.css'
import './story-entry.css'

const worlds = [
  { id: 'infobiz', templateId: '5720946a-ea7c-4a10-9c50-51ce3deefc53', title: 'Последние инфобизеры' },
  { id: 'evening', templateId: 'b3a1a522-0263-4796-a197-57a8731fc291', title: 'Вечерело' },
  { id: 'hogwarts', templateId: 'aec9494b-4118-4525-8458-e85d759c8dd9', title: 'Спасти Хогвартс' },
  { id: 'houses', templateId: '8c02a7fb-3b55-4eb0-b957-1b4869ed9834', title: 'Игра домов' },
] as const

export const landingWorldForVariant = (variant: ReturnType<typeof requestedVariant>) => worlds.find(world => world.id === variant)?.templateId

export function Landing({ onContinue, preview = false }: { onContinue: (worldId?: string) => void; preview?: boolean }) {
  const [variant] = useState(() => preview ? requestedVariant() ?? 'worlds' : assignLanding())
  const hero = worlds.find(world => world.id === variant)
  const [selected, setSelected] = useState<string>(hero?.templateId ?? worlds[0].templateId)
  const [available, setAvailable] = useState<World[]>([])
  const [error, setError] = useState(false)
  const [attempt, setAttempt] = useState(0)
  useEffect(() => {
    let active = true
    setError(false)
    void api.loadWorldTemplates().then(() => { if (active) { setAvailable([...templates]); trackLandingAuth('landing_catalog_result', { auth_outcome: templates.length ? 'ready' : 'empty' }) } }).catch(() => { if (active) { setError(true); trackLandingAuth('landing_catalog_result', { auth_outcome: 'error' }) } })
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
  const start = (worldId?: string) => {
    if (clicked.current) return
    clicked.current = true
    onContinue(worldId)
  }
  if (hero && isStoryLanding(variant)) {
    const evening = variant === 'evening'
    const features = ['Веди блог от лица героя', 'Получай ответы персонажей', 'Развивай отношения в комментариях', 'Собирай подписчиков', 'Пробуй другие миры и роли']
    return <main className="story-entry">
      <header><div className="brand"><img src="/android-chrome-192x192.png?v=wave-2" alt="" />Реки</div></header>
      <div className="story-wrap"><section className="hero">
        <img className="cover" src={evening ? '/landing/evening-full.png' : '/landing/hogwarts-960.jpg'} srcSet={evening ? '/landing/evening-480.jpg 480w, /landing/evening-full.png 1024w' : '/landing/hogwarts-480.jpg 480w, /landing/hogwarts-960.jpg 960w'} sizes="(max-width:700px) 100vw, 550px" fetchPriority="high" alt={hero.title} />
        <div><div className="world">{hero.title}</div>
          <h1>{evening ? 'Тебе ответил Эдуард' : 'Тебе ответил Драко'}</h1>
          <p>{evening ? 'Веди блог Изабеллы. После грозы на поляне остались вопросы. Опубликуй свою версию и прочитай, что скажет Эдуард.' : 'Веди блог от лица Гарри Поттера. Хогвартс закрывают, Драко хочет открыть отель. Что ты скажешь ученикам?'}</p>
          <button className="cta" onClick={() => start(hero.templateId)}>{evening ? 'Начать за Изабеллу' : 'Начать за Гарри'}</button>
          <small className="sub">Играть гостем · без регистрации</small>
          <button className="story-login" onClick={() => start()}>Уже есть аккаунт? Войти</button>
        </div>
      </section></div>
      <footer className="features" tabIndex={0} aria-label="Возможности игры"><div className="track">{[0, 1].map(group =>
        <div key={group} className="items" aria-hidden={group === 1 || undefined}>{[...features, ...features].map((feature, i) =>
          <span key={i} aria-hidden={i >= features.length || undefined}>{feature}</span>)}</div>)}</div></footer>
    </main>
  }
  return <main className={`entry-landing entry-landing--${variant}${hero ? ' entry-landing--single' : ''}`} aria-labelledby="landing-title">
    {hero && <img className="landing-hero-art" src={hero.id === 'hogwarts' ? '/landing/hogwarts-960.jpg' : `/landing/${hero.id}-full.png`} srcSet={hero.id === 'hogwarts' ? '/landing/hogwarts-480.jpg 480w, /landing/hogwarts-960.jpg 960w' : hero.id === 'infobiz' ? '/landing/infobiz-full.png 1536w' : `/landing/${hero.id}-480.jpg 480w, /landing/${hero.id}-full.png 1024w`} sizes="(max-width: 430px) 100vw, 430px" alt="" fetchPriority="high" />}
    <header className="landing-brand"><img src="/android-chrome-192x192.png?v=wave-2" width="42" height="42" alt="" /><span>Реки</span></header>
    {variant === 'worlds' ? <>
      <section className="landing-copy"><h1 ref={heading} id="landing-title" tabIndex={-1}>Твой блог.<br />Твоя история.</h1><p>Выбери героя, пиши посты и смотри, как отвечает его мир.</p></section>
      <section className="landing-mosaic" aria-label="Игровые миры">{worlds.map(world => <button type="button" className="landing-world" key={world.id} aria-pressed={selected === world.templateId} onClick={() => { trackLandingAuth('landing_world_action', { world_choice: world.id, auth_action: 'select' }); setSelected(world.templateId) }}><img src={`/landing/${world.id}-480.jpg`} width="480" height="720" alt="" fetchPriority={world.id === 'infobiz' ? 'high' : 'auto'} /><strong>{world.title}</strong></button>)}</section>
      <p className="landing-world-summary" aria-live="polite">{chosen?.summary ?? (error ? '' : available.length ? 'Этот мир сейчас недоступен. Выбери другой.' : 'Загружаем миры…')}</p>
    </> : <>
      <section className="landing-hero-copy"><div className="landing-eyebrow">{hero?.id === 'hogwarts' ? hero.title : 'Мир в «Реках»'}</div><h1 ref={heading} id="landing-title" tabIndex={-1}>{hero?.id === 'hogwarts' ? <>Хогвартс закрыт.<br />Спаси его от банкротства.</> : hero?.title}</h1><p aria-live="polite">{hero?.id === 'hogwarts' ? <>Выбери героя. Веди его блог.<br />Решай, кому верить.</> : chosen?.summary}</p></section>
      <section className="landing-other-worlds" aria-label="Другие игровые миры">{worlds.filter(world => world.id !== variant).map(world => {
        const query = new URLSearchParams(location.search)
        query.set('landing', world.id)
        return <a key={world.id} href={`?${query}`} onClick={() => trackLandingAuth('landing_world_action', { world_choice: world.id, auth_action: 'switch' })}><img src={`/landing/${world.id}-480.jpg`} width="480" height="720" alt="" /><span>{world.id === 'infobiz' ? 'Инфобизеры' : world.title}</span></a>
      })}</section>
    </>}
    <footer className="landing-actions">{error && <div role="alert" className="landing-load-error"><p>Не получилось загрузить миры.</p><Button secondary onClick={() => { trackLandingAuth('landing_catalog_retry'); setAttempt(value => value + 1) }}>Попробовать снова</Button></div>}{!error && hero && !chosen && <p role="status">{available.length ? 'Этот мир сейчас недоступен.' : 'Загружаем мир…'}</p>}<Button onClick={() => { start(selected) }}>Зайти как гость</Button><p className="landing-login">Уже есть аккаунт? <button type="button" className="text-button" onClick={() => start()}>Войти</button></p><p>Твои собеседники — AI-персонажи</p></footer>
  </main>
}
