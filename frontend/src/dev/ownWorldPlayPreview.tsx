import { useEffect, useRef, type ComponentProps } from 'react'
import { VoicePortraits, voicePortraitSeed } from './ownWorldFullEditor'
import './ownWorldPlayPreview.css'
import { VoiceCoverImage, type VoiceCover } from './ownWorldCover'

type PlayProps = {
  step: 'channel' | 'compose' | 'result'
  draft: ComponentProps<typeof VoicePortraits>['draft']
  cover: VoiceCover | null
  postText: string
  publishedText: string
  onText: (text: string) => void
  onCompose: () => void
  onPublish: () => void
  onChannel: () => void
  onRead: () => void
}

export function VoiceOwnWorldPlay({ step, draft, cover, postText, publishedText, onText, onCompose, onPublish, onChannel, onRead }: PlayProps) {
  const heading = useRef<HTMLHeadingElement>(null)
  useEffect(() => { heading.current?.focus({ preventScroll: true }) }, [step])

  if (step === 'channel') return <main className="pad voice-play">
    {cover && <VoiceCoverImage cover={cover} />}
    <h1 ref={heading} tabIndex={-1}>{draft.title.trim() || 'Мой мир'}</h1>
    <div className="voice-play-cast"><VoicePortraits draft={draft} /></div>
    {publishedText ? <>
      <article className="card voice-play-card">
        <h2>Твой первый пост</h2>
        <p className="voice-published-text">{publishedText}</p>
      </article>
      <button className="btn" type="button" onClick={onRead}>Открыть пост</button>
    </> : <>
      {draft.firstContext.trim() && <section className="card first-story voice-play-context">
        <h2>Сегодня всё начинается</h2>
        <p className="first-story-context">{draft.firstContext}</p>
      </section>}
      <button className="btn" type="button" onClick={onCompose}>Написать первый пост</button>
    </>}
  </main>

  if (step === 'compose') return <main className="pad voice-play">
    <h1 ref={heading} tabIndex={-1}>Первый пост</h1>
    {draft.firstPrompt.trim() && <aside className="compose-topic voice-play-prompt">
      <b className="small">О чём написать</b>
      <p>{draft.firstPrompt}</p>
    </aside>}
    <form className="voice-play-form" onSubmit={event => {
      event.preventDefault()
      if (postText.trim()) onPublish()
    }}>
      <div className="card">
        <label className="voice-field">
          Текст поста
          <textarea rows={6} value={postText} onChange={event => onText(event.target.value)} placeholder="Расскажи, что случилось…" />
        </label>
      </div>
      <p className="small muted">Пост появится только в этом демо.</p>
      <button type="submit" className="btn" disabled={!postText.trim()}>Опубликовать в демо</button>
    </form>
  </main>

  const hero = draft.stars.find(person => person.name.trim() && person.role.trim() && person.about.trim() && person.voice.trim())
  return <main className="pad voice-play">
    <div className="row">
      <span className="post-result-check" aria-hidden="true">✓</span>
      <h1 ref={heading} tabIndex={-1}>Пост в канале</h1>
    </div>
    <article className="card voice-play-card">
      <h2>{draft.name.trim() || 'Твой персонаж'}</h2>
      <p className="voice-published-text">{publishedText}</p>
    </article>
    {hero && <section className="post-result-reaction voice-play-reaction">
      <h2>Пример реакции</h2>
      <VoicePortraits draft={{ ...draft, name: hero.name, role: hero.role, avatar: hero.avatarSeed || hero.id, portraitSeeds: { player: voicePortraitSeed(draft, hero.id, hero.avatarSeed || hero.id) }, stars: [] }} />
      <p>Зацепило. Если решишь разобраться, дай знать. Мне тоже интересно, что там на самом деле.</p>
    </section>}
    <button className="btn" type="button" onClick={onChannel}>К каналу</button>
  </main>
}
