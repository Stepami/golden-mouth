import { useEffect, useRef, type ComponentProps, type ReactNode } from 'react'
import { VoicePortraits, voicePortraitSeed } from './ownWorldFullEditor'
import { exampleReaction } from './ownWorldExample'
import './ownWorldPlayPreview.css'
import { VoiceCoverImage, type VoiceCover } from './ownWorldCover'

type PlayProps = {
  step: 'channel' | 'compose' | 'result'
  draft: ComponentProps<typeof VoicePortraits>['draft']
  cover: VoiceCover | null
  postText: string
  posts: string[]
  publishedText: string
  hint?: ReactNode
  onText: (text: string) => void
  onCompose: () => void
  onPublish: () => void
  onChannel: () => void
  onRead: (index: number) => void
}

export function VoiceOwnWorldPlay({ step, draft, cover, postText, posts, publishedText, hint, onText, onCompose, onPublish, onChannel, onRead }: PlayProps) {
  const heading = useRef<HTMLHeadingElement>(null)
  useEffect(() => { heading.current?.focus({ preventScroll: true }) }, [step])
  const firstPost = posts.length === 0

  if (step === 'channel') return <main className="pad voice-play">
    {cover && <VoiceCoverImage cover={cover} />}
    <h1 ref={heading} tabIndex={-1}>{draft.title.trim() || 'Мой мир'}</h1>
    <div className="voice-play-cast"><VoicePortraits draft={draft} /></div>
    {firstPost && <>
      {draft.firstContext.trim() && <section className="card first-story voice-play-context">
        <h2>Сегодня всё начинается</h2>
        <p className="first-story-context">{draft.firstContext}</p>
      </section>}
      {hint}
    </>}
    <button className="btn" type="button" onClick={onCompose}>{firstPost ? 'Написать первый пост' : 'Написать пост'}</button>
    {posts.map((text, index) => <article className="card voice-play-card" key={index}>
      <h2>{index === 0 ? 'Твой первый пост' : `Пост ${index + 1}`}</h2>
      <p className="voice-published-text">{text}</p>
      <button className="text-link" type="button" onClick={() => onRead(index)}>Открыть пост</button>
    </article>)}
  </main>

  if (step === 'compose') return <main className="pad voice-play">
    <h1 ref={heading} tabIndex={-1}>{firstPost ? 'Первый пост' : 'Новый пост'}</h1>
    {firstPost && draft.firstPrompt.trim() && <aside className="compose-topic voice-play-prompt">
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
      <p>{hero.id === 'mai' ? exampleReaction : 'На записи лица не видно. Я бы сначала спросил у тех, кто был на улице.'}</p>
    </section>}
    <button className="btn" type="button" onClick={onChannel}>К каналу</button>
  </main>
}
