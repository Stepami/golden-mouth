import { useEffect, useRef } from 'react'
import { TABS } from '../app/nav'
import { worldArt } from '../app/worldArt'
import hogwartsPreviewCover from '../../public/landing/hogwarts-480.jpg?url'
import { VoiceCoverImage, type VoiceCover } from './ownWorldCover'
import { exampleCover } from './ownWorldExample'
import './ownWorldJourney.css'

const presetPreviews = [
  { id: 'b3a1a522-0263-4796-a197-57a8731fc291', title: 'Вечерело', emoji: '🌲' },
  { id: 'aec9494b-4118-4525-8458-e85d759c8dd9', title: 'Спасти Хогвартс', emoji: '🏰', cover: hogwartsPreviewCover },
  { id: '8c02a7fb-3b55-4eb0-b957-1b4869ed9834', title: 'Игра домов', emoji: '👑' },
  { id: '5720946a-ea7c-4a10-9c50-51ce3deefc53', title: 'Последние инфобизеры', emoji: '💻' },
]

const cues = {
  1: 'Хватит нескольких фраз.',
  2: 'Проверь завязку, персонажей и правила своего мира.',
  3: 'Дополни готовую идею или напиши первый пост своими словами.',
}

export function VoiceJourneyHint({ step }: { step: 1 | 2 | 3 }) {
  return <aside className="game-onboarding-progress is-inline voice-journey-hint" aria-label="Подсказка">
    <div className="game-onboarding-meta">
      <span>Шаг {step} из 3</span>
      <span className="game-onboarding-segments" aria-hidden="true">
        {[1, 2, 3].map(segment => <i key={segment} className={segment <= step ? 'on' : ''} />)}
      </span>
    </div>
    <p>{cues[step]}</p>
  </aside>
}

export function VoiceWorldNav({ active, onWorlds, onChannel }: {
  active: 'worlds' | 'channel'
  onWorlds?: () => void
  onChannel?: () => void
}) {
  return <nav className="tabbar voice-journey-tabs" aria-label="Разделы игры">
    {[5, 0, 1, 4].map(index => TABS[index]).map(({ title: label, icon }) => {
      const tab = label === 'Миры' ? 'worlds' : label === 'Канал' ? 'channel' : undefined
      const action = tab === 'worlds' ? onWorlds : tab === 'channel' ? onChannel : undefined
      const current = tab === active
      return <button
        type="button"
        key={label}
        className={current ? 'on' : undefined}
        aria-current={current ? 'page' : undefined}
        disabled={current || !action}
        onClick={action}
      ><b aria-hidden="true">{icon}</b>{label}</button>
    })}
  </nav>
}

function VoiceWorldFeature() {
  return <figure className="voice-feature-art">
    <img src={exampleCover.url} alt={exampleCover.alt} />
    <figcaption>
      <span>Собери свой мир</span>
      <h3>На Патонге пропал скутер.</h3>
      <p>На видео — твоя куртка.</p>
    </figcaption>
  </figure>
}

export function VoiceWorldsPreview({ unlocked, savedWorld, cover, onUnlock, onCreate, onOpenWorld }: {
  unlocked: boolean
  savedWorld: { title: string; summary?: string } | null
  cover: VoiceCover | null
  onUnlock: () => void
  onCreate: () => void
  onOpenWorld: () => void
}) {
  return <>
    <main className="pad worlds-home voice-worlds-catalog">
      {savedWorld && <section>
        <h2>Мои истории</h2>
        <article className="world-cover-card voice-journey-world-card">
          <div className="world-cover-image">
            {cover ? <VoiceCoverImage cover={cover} /> : <div className="voice-journey-cover-empty" aria-hidden="true">🌍</div>}
          </div>
          <div className="world-cover-copy">
            <h3>{savedWorld.title.trim() || 'Мой мир'}</h3>
            {savedWorld.summary?.trim() && <p>{savedWorld.summary}</p>}
            <button type="button" className="text-link" onClick={onOpenWorld}>Открыть мир</button>
          </div>
        </article>
      </section>}
      <section>
        <h2>Выбери новую историю</h2>
        <div className="world-catalog voice-preset-catalog" role="region" aria-label="Готовые миры" tabIndex={0}>
          {presetPreviews.map(({ id, title, emoji, cover }) => {
            const art = worldArt[id]
            return <article className="world-cover-card voice-journey-world-card" key={id}>
              <div className="world-cover-image">
                <img src={cover ?? art.smallCover} alt="" />
                <span className="world-genre">{emoji} {art.genre}</span>
              </div>
              <div className="world-cover-copy">
                <h3>{title}</h3>
                <p>{art.pitch}</p>
              </div>
            </article>
          })}
        </div>
      </section>
      {!savedWorld && <section className="card own-world-unlock">
        <div className="row voice-journey-unlock-heading">
          <h2>Свой мир</h2>
          <span className="small muted voice-world-access">
            {!unlocked && <svg viewBox="0 0 16 16" width="13" height="14" aria-hidden="true">
              <rect x="3" y="7" width="10" height="7" rx="2" />
              <path d="M5 7V5a3 3 0 0 1 6 0v2" />
            </svg>}
            {unlocked ? 'Открыт' : 'Закрыт'}
          </span>
        </div>
        <VoiceWorldFeature />
        <div className="voice-feature-actions">
          <button type="button" className="btn voice-feature-cta" onClick={unlocked ? onCreate : onUnlock}>
            <svg className="voice-mic" viewBox="0 0 20 20" aria-hidden="true">
              <rect x="7" y="2" width="6" height="10" rx="3" />
              <path d="M4 9a6 6 0 0 0 12 0M10 15v3M7 18h6" />
            </svg>
            {unlocked ? 'Создать свой мир' : 'Попробовать на примере'}
          </button>
          <p className="small voice-feature-pitch">Надиктуй любую задумку. ИИ соберёт из неё игровой мир.</p>
          {!unlocked && <details className="voice-unlock-details">
            <summary>Как открыть свой мир</summary>
            <p>Пригласи друга, который опубликует первый пост, или набери 500 подписчиков в одном мире.</p>
          </details>}
        </div>
      </section>}
    </main>
    <VoiceWorldNav active="worlds" onChannel={savedWorld ? onOpenWorld : undefined} />
  </>
}

export function VoiceUnlockPreview({ onCreate, onLater }: { onCreate: () => void; onLater: () => void }) {
  const heading = useRef<HTMLHeadingElement>(null)
  useEffect(() => { heading.current?.focus({ preventScroll: true }) }, [])

  return <main className="pad voice-journey-celebration">
    <section className="sheet unlock-celebration">
      <div className="unlock-sparkles" aria-hidden="true">✨ 🌍 ✨</div>
      <h1 ref={heading} tabIndex={-1}>Свой мир открыт!</h1>
      <p>Придумай место, героев и историю. Создать мир можно и позже в разделе «Миры».</p>
      <button type="button" className="btn" onClick={onCreate}>Создать свой мир</button>
      <button type="button" className="btn secondary" onClick={onLater}>Позже</button>
    </section>
  </main>
}
