import { useEffect, useRef } from 'react'
import { TABS } from '../app/nav'
import eveningCover from '../assets/evening-640-v4.webp'
import { VoiceCoverImage, type VoiceCover } from './ownWorldCover'
import { exampleCover } from './ownWorldExample'
import './ownWorldJourney.css'

const cues = {
  1: 'Опиши место, героя и событие, с которого всё начинается.',
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
  return <div className="voice-world-feature">
    <figure className="voice-feature-art">
      <img src={exampleCover.url} alt={exampleCover.alt} />
      <div className="voice-feature-record" aria-hidden="true">
        <div className="voice-feature-mic">
          <svg viewBox="0 0 24 24">
            <rect x="9" y="3" width="6" height="11" rx="3" />
            <path d="M5.5 11a6.5 6.5 0 0 0 13 0M12 17.5v4M8.5 21.5h7" />
          </svg>
        </div>
        <div className="voice-feature-waves"><i /><i /><i /><i /><i /></div>
      </div>
      <figcaption>{exampleCover.title}</figcaption>
    </figure>
    <p className="voice-feature-title">Опиши мир голосом. Получишь героев и идею для первого поста.</p>
  </div>
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
        <article className="world-cover-card voice-journey-world-card">
          <div className="world-cover-image">
            <img src={eveningCover} alt="" />
            <span className="world-genre">🌆 Тайны и романтика</span>
          </div>
          <div className="world-cover-copy">
            <h3>Вечерело</h3>
            <p>Исчезнувшие улицы, чужие воспоминания и знакомства после заката.</p>
          </div>
        </article>
      </section>
      {!savedWorld && <section className="card own-world-unlock">
        <div className="row voice-journey-unlock-heading">
          <h2>Свой мир</h2>
          <span className="small muted">{unlocked ? 'Открыт' : '🔒 Закрыт'}</span>
        </div>
        <VoiceWorldFeature />
        {unlocked
          ? <button type="button" className="btn" onClick={onCreate}>Создать свой мир</button>
          : <div className="voice-unlock-details">
            <div>
              <b>Как открыть</b>
              <p className="small muted">Первый пост приглашённого друга или 500 подписчиков в одном мире.</p>
            </div>
            <button type="button" className="text-link" onClick={onUnlock}>Попробовать в демо</button>
          </div>}
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
