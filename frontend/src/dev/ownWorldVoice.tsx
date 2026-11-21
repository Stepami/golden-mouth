import { useEffect, useRef, useState } from 'react'
import { createRoot } from 'react-dom/client'
import '../theme.css'
import './ownWorldVoice.css'
import { useVoiceFullDraft, VoiceFullEditor, VoiceQuickCharacters, voiceDraftSummary, voiceWorldIssue } from './ownWorldFullEditor'
import { VoiceOwnWorldPlay } from './ownWorldPlayPreview'
import { suggestVoiceCover, VoiceCoverImage, type VoiceCover } from './ownWorldCover'
import { VoiceJourneyHint, VoiceUnlockPreview, VoiceWorldNav, VoiceWorldsPreview } from './ownWorldJourney'

import { exampleDraft as sampleDraft, exampleStory as story } from './ownWorldExample'

const prompts = [
  { icon: '📍', label: 'Место', question: 'Где начинается история?' },
  { icon: '👥', label: 'Герои', question: 'За кого играешь? Кто рядом?' },
  { icon: '⚡', label: 'Завязка', question: 'Что случилось и о чём ты захочешь написать?' },
  { icon: '📜', label: 'Правило', question: 'Что здесь можно, а чего нельзя?' },
]

function release(stream: MediaStream) {
  stream.getTracks().forEach(track => track.stop())
}

function Recorder({ audio, onAudio, onText, onTranscribe }: {
  audio: Blob | null
  onAudio: (audio: Blob) => void
  onText: () => void
  onTranscribe: () => void
}) {
  const recorder = useRef<MediaRecorder | null>(null)
  const mounted = useRef(false)
  const [phase, setPhase] = useState<'idle' | 'permission' | 'recording' | 'stopping'>('idle')
  const [elapsed, setElapsed] = useState(0)
  const [error, setError] = useState('')
  const playback = useRef<HTMLAudioElement>(null)

  useEffect(() => {
    if (!audio || phase !== 'idle' || !playback.current) return
    const url = URL.createObjectURL(audio)
    playback.current.src = url
    return () => URL.revokeObjectURL(url)
  }, [audio, phase])

  useEffect(() => {
    mounted.current = true
    return () => {
      mounted.current = false
      const current = recorder.current
      recorder.current = null
      if (!current) return
      if (current.state !== 'inactive') current.stop()
      release(current.stream)
    }
  }, [])

  useEffect(() => {
    if (phase !== 'recording') return
    const started = Date.now()
    const timer = window.setInterval(() => setElapsed(Math.floor((Date.now() - started) / 1000)), 250)
    return () => window.clearInterval(timer)
  }, [phase])

  async function record() {
    setError('')
    if (!navigator.mediaDevices?.getUserMedia || typeof MediaRecorder === 'undefined') {
      setError('Запись здесь недоступна. Можно напечатать историю или открыть пример.')
      return
    }
    setPhase('permission')
    let stream: MediaStream | undefined
    try {
      stream = await navigator.mediaDevices.getUserMedia({ audio: true })
      if (!mounted.current) {
        release(stream)
        return
      }
      const current = new MediaRecorder(stream)
      const chunks: Blob[] = []
      current.ondataavailable = ({ data }) => { if (data.size) chunks.push(data) }
      current.onerror = () => {
        if (recorder.current !== current) return
        recorder.current = null
        release(current.stream)
        setPhase('idle')
        setError('Запись прервалась. Можно попробовать ещё раз.')
      }
      current.onstop = () => {
        release(current.stream)
        if (recorder.current !== current) return
        recorder.current = null
        setPhase('idle')
        if (chunks.length) {
          onAudio(new Blob(chunks, { type: current.mimeType }))
        } else {
          setError('Запись получилась пустой. Попробуй ещё раз.')
        }
      }
      recorder.current = current
      current.start()
      setElapsed(0)
      setPhase('recording')
    } catch (cause) {
      if (stream) release(stream)
      if (!mounted.current) return
      recorder.current = null
      setPhase('idle')
      setError(cause instanceof DOMException && cause.name === 'NotAllowedError'
        ? 'Разреши доступ к микрофону в браузере или напечатай историю.'
        : 'Микрофон недоступен. Можно напечатать историю или открыть пример.')
    }
  }

  function stop() {
    const current = recorder.current
    if (!current || current.state === 'inactive') return
    setPhase('stopping')
    current.stop()
    release(current.stream)
  }

  const busy = phase === 'permission' || phase === 'stopping'
  const recording = phase === 'recording'
  const clock = [Math.floor(elapsed / 60), elapsed % 60].map(part => String(part).padStart(2, '0')).join(':')
  return <div className="voice-recorder">
    {recording && <div className="card">
      <div className="voice-live">
        <span className="voice-wave" aria-hidden="true"><i /><i /><i /><i /><i /></span>
        <span role="status">Идёт запись</span>
        <time>{clock}</time>
      </div>
    </div>}
    {audio && phase === 'idle' && <>
      <audio ref={playback} controls aria-label="Прослушать запись" />
      <p className="small muted">В демо вместо текста записи откроется пример расшифровки.</p>
    </>}
    {error && <p className="voice-error" role="alert">{error}</p>}
    <button type="button" className="btn"
      disabled={busy}
      onClick={recording ? stop : audio ? onTranscribe : record}
    >
      {!busy && !recording && !audio && <svg className="voice-mic" viewBox="0 0 20 20" aria-hidden="true">
        <rect x="7" y="2" width="6" height="10" rx="3" />
        <path d="M4 9a6 6 0 0 0 12 0M10 15v3M7 18h6" />
      </svg>}
      {phase === 'permission' ? 'Ждём доступ к микрофону…' : phase === 'stopping' ? 'Завершаем запись…'
        : recording ? 'Остановить запись' : audio ? 'Расшифровать запись' : 'Начать запись'}
    </button>
    {phase === 'idle' && <div className="voice-alternatives">
      <button type="button" className="text-link" onClick={onText}>Напечатать историю</button>
      {audio && <button type="button" className="text-link" onClick={record}>Перезаписать</button>}
    </div>}
  </div>
}

type Page = 'worlds' | 'unlock' | 'story' | 'listening' | 'transcript' | 'assembling' | 'draft' | 'full' | 'opening' | 'channel' | 'compose' | 'result'
type LoadingPage = 'listening' | 'assembling' | 'opening'
type SavedWorld = {
  draft: ReturnType<typeof useVoiceFullDraft>['previewDraft']
  cover: VoiceCover | null
  postDraft: string | null
  posts: string[]
}

const loadingSteps: Record<LoadingPage, { title: string; caption: string; next: Page; duration: number }> = {
  listening: { title: 'Готовим пример расшифровки', caption: 'Следом можно изменить текст.', next: 'transcript', duration: 1400 },
  assembling: { title: 'Собираем твой мир', caption: 'В демо часть полей заполняется из примера.', next: 'draft', duration: 1900 },
  opening: { title: 'В твоём мире зажигаются огни', caption: 'Готовим канал к первому посту.', next: 'channel', duration: 1200 },
}

const pageTitle: Record<Page, string> = {
  worlds: 'Миры', unlock: 'Свой мир', story: 'Свой мир', listening: 'Расшифровка', transcript: 'Расшифровка', assembling: 'Черновик мира',
  draft: 'Черновик мира', full: 'Настройка мира', opening: 'Твой мир', channel: 'Твой канал',
  compose: 'Новый пост', result: 'Твой канал',
}

function LoadingScreen({ phase }: { phase: LoadingPage }) {
  const step = loadingSteps[phase]
  return <main className="pad voice-wait" data-phase={phase}>
    <div className="voice-magic" aria-hidden="true">
      {phase === 'listening' ? <div className="voice-magic-wave"><i /><i /><i /><i /><i /><i /><i /></div>
        : phase === 'assembling' ? <div className="voice-magic-pieces">{prompts.map(({ icon, label }) => <span key={label}>{icon}</span>)}</div>
        : <div className="voice-magic-stars"><span>✦</span><i>✧</i><i>✦</i><i>✧</i></div>}
    </div>
    <div className="voice-wait-copy" role="status" aria-live="polite">
      <h1>{step.title}</h1>
      <p className="muted">{step.caption}</p>
    </div>
    <div className="voice-wait-light" aria-hidden="true"><span /></div>
  </main>
}

function App() {
  const [page, setPage] = useState<Page>('worlds')
  const [unlocked, setUnlocked] = useState(false)
  const [worlds, setWorlds] = useState<SavedWorld[]>([])
  const [worldIndex, setWorldIndex] = useState(0)
  const [postIndex, setPostIndex] = useState(0)
  const [pendingWorld, setPendingWorld] = useState<SavedWorld | null>(null)
  const activeWorld = worlds[worldIndex]
  const [typing, setTyping] = useState(false)
  const [text, setText] = useState('')
  const [audio, setAudio] = useState<Blob | null>(null)
  const [transcript, setTranscript] = useState(story)
  const [draft, setDraft] = useState(sampleDraft)
  const [cover, setCover] = useState<VoiceCover | null>(null)
  const coverUrls = useRef(new Set<string>())
  const coverEdited = useRef(false)
  const draftIndex = useRef(0)
  const [assembledText, setAssembledText] = useState(story)
  const changeCover = (next: VoiceCover | null) => {
    if (worlds.length !== draftIndex.current) {
      if (next?.url.startsWith('blob:')) URL.revokeObjectURL(next.url)
      return
    }
    if (next?.url.startsWith('blob:')) coverUrls.current.add(next.url)
    coverEdited.current = true
    setCover(next)
  }
  const { draft: fullDraft, previewDraft: playWorld, setDraft: setFullDraft, openEditor } = useVoiceFullDraft(draft)
  const initialFullDraft = useRef(fullDraft)
  const returnToDraft = (updated: typeof sampleDraft) => {
    setDraft(updated)
    setPage('draft')
  }
  const startStory = (updated: typeof sampleDraft, definition: SavedWorld['draft']) => {
    setDraft(updated)
    setPendingWorld({ draft: definition, cover, posts: [], postDraft: null })
    setPage('opening')
  }
  const updateActiveWorld = (update: (world: SavedWorld) => SavedWorld) => {
    setWorlds(current => current.map((world, index) => index === worldIndex ? update(world) : world))
  }
  const phase = page === 'listening' || page === 'assembling' || page === 'opening' ? page : undefined
  const worldIssue = voiceWorldIssue(playWorld)
  const postText = activeWorld?.postDraft
    ?? (activeWorld?.posts.length === 0 ? activeWorld.draft.postIdeas.find(idea => idea.trim()) : undefined)
    ?? ''
  const previousPage: Partial<Record<Page, Page>> = {
    story: 'worlds', unlock: 'worlds', assembling: typing ? 'story' : 'transcript', draft: typing ? 'story' : 'transcript',
    opening: 'draft', channel: 'worlds', compose: 'channel', result: 'channel',
  }
  const heading = useRef<HTMLHeadingElement>(null)
  const textInput = useRef<HTMLTextAreaElement>(null)

  useEffect(() => {
    const usedUrls = new Set([cover?.url, pendingWorld?.cover?.url, ...worlds.map(world => world.cover?.url)])
    for (const url of coverUrls.current) {
      if (usedUrls.has(url)) continue
      URL.revokeObjectURL(url)
      coverUrls.current.delete(url)
    }
  }, [cover, pendingWorld, worlds])

  useEffect(() => {
    const urls = coverUrls.current
    return () => urls.forEach(url => URL.revokeObjectURL(url))
  }, [])

  useEffect(() => {
    window.scrollTo(0, 0)
    if (page !== 'story') heading.current?.focus()
  }, [page])

  useEffect(() => {
    if (page === 'story' && typing) textInput.current?.focus()
  }, [page, typing])

  useEffect(() => {
    if (!phase) return
    const step = loadingSteps[phase]
    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches
    const timer = window.setTimeout(() => {
      if (phase === 'assembling') {
        const source = typing ? text : transcript
        if (!coverEdited.current) setCover(suggestVoiceCover(source))
        if (assembledText !== source) {
          setAssembledText(source)
          setDraft(previous => ({ ...previous, opening: source === story ? sampleDraft.opening : source }))
          setFullDraft(previous => ({
            ...previous,
            summary: source === story ? initialFullDraft.current.summary : source.trim().slice(0, 200),
            description: source === story ? initialFullDraft.current.description : source.trim().slice(0, 2000),
          }))
        }
      }
      if (phase === 'opening' && pendingWorld) {
        setWorlds(current => [...current, pendingWorld])
        setWorldIndex(draftIndex.current)
        draftIndex.current += 1
        setPendingWorld(null)
        setTyping(false)
        setText('')
        setAudio(null)
        setTranscript(story)
        setDraft(sampleDraft)
        setFullDraft(initialFullDraft.current)
        setCover(null)
        coverEdited.current = false
        setAssembledText(story)
      }
      setPage(step.next)
    }, reduced ? 300 : step.duration)
    return () => window.clearTimeout(timer)
  }, [assembledText, pendingWorld, phase, setFullDraft, text, transcript, typing])

  return <div className="app voice-preview">
    <header className="top">
      {page !== 'worlds' && <button
        type="button"
        className="back"
        aria-label="Назад"
        onClick={() => {
          if (page === 'full') {
            returnToDraft(voiceDraftSummary(fullDraft))
            return
          }
          if (page === 'opening') setPendingWorld(null)
          setPage(previousPage[page] ?? 'story')
        }}
      >‹</button>}
      <b className="top-title">{pageTitle[page]}</b>
      <span className="pill">Превью</span>
    </header>
    {page === 'worlds' && <VoiceWorldsPreview
      unlocked={unlocked}
      savedWorlds={worlds}
      onUnlock={() => { setUnlocked(true); setPage('unlock') }}
      onCreate={() => setPage('story')}
      onOpenWorld={index => { setWorldIndex(index); setPage('channel') }}
      onChannel={activeWorld ? () => setPage('channel') : undefined}
    />}
    {page === 'unlock' && <VoiceUnlockPreview onCreate={() => setPage('story')} onLater={() => setPage('worlds')} />}
    {phase && <LoadingScreen phase={phase} />}
    {page === 'story' && <main className="pad voice-story">
      <h1>Расскажи про свой мир</h1>
      <VoiceJourneyHint step={1} />
      <ul className="voice-prompts" aria-label="Что рассказать о мире">
        {prompts.map(({ icon, label, question }) => <li className="card" key={label}>
          <span className="voice-prompt-icon" aria-hidden="true">{icon}</span>
          <b>{label}</b>
          <span className="small muted">{question}</span>
        </li>)}
      </ul>
      <details className="voice-story-example">
        <summary className="text-link">Пример рассказа</summary>
        <p>{story}</p>
        {!text.trim() && <button type="button" className="text-link" onClick={() => { setText(story); setTyping(true) }}>Вставить в текст истории</button>}
      </details>
      {typing ? <div className="voice-recorder">
        <label className="voice-field">
          Твоя история
          <textarea ref={textInput} rows={3} value={text} onChange={event => setText(event.target.value)} placeholder="Всё начинается в…" />
        </label>
        <p className="small muted">Твой текст станет завязкой. Остальные поля заполнятся из примера.</p>
        <button type="button" className="btn" disabled={!text.trim()} onClick={() => setPage('assembling')}>Собрать черновик</button>
        <button type="button" className="text-link voice-choose-mode" onClick={() => setTyping(false)}>Перейти к записи</button>
      </div> : <Recorder audio={audio} onAudio={setAudio} onText={() => setTyping(true)} onTranscribe={() => setPage('listening')} />}
    </main>}
    {page === 'transcript' && <main className="pad voice-review">
      <div className="voice-intro">
        <h1 ref={heading} tabIndex={-1}>Проверь, что получилось</h1>
        <p className="muted">Это пример расшифровки. Замени его своим текстом, чтобы он стал завязкой.</p>
      </div>
      <div className="card">
        <label className="voice-field">
          Текст истории
          <textarea rows={7} value={transcript} onChange={event => setTranscript(event.target.value)} />
        </label>
      </div>
      <button type="button" className="btn" disabled={!transcript.trim()} onClick={() => setPage('assembling')}>Собрать черновик</button>
    </main>}
    {page === 'draft' && <main className="pad voice-review">
      <div className="voice-intro">
        <h1 ref={heading} tabIndex={-1}>Проверь черновик</h1>
        <p className="muted">{assembledText === story
          ? 'Черновик заполнен данными примера. Детали можно менять.'
          : 'Завязка и описание взяты из твоего текста. Часть полей заполнена данными примера.'}</p>
      </div>
      <div className="card">
        <div className="voice-fields">
          {cover && <VoiceCoverImage cover={cover} />}
          <label className="voice-field">
            Название
            <input value={draft.title} onChange={event => setDraft({ ...draft, title: event.target.value })} />
          </label>
          <label className="voice-field">
            Мир и завязка
            <textarea rows={3} value={draft.opening} onChange={event => setDraft({ ...draft, opening: event.target.value })} />
          </label>
          <VoiceQuickCharacters draft={fullDraft} onChange={setFullDraft} />
          <label className="voice-field">
            Правила мира
            <textarea rows={2} value={draft.rules} onChange={event => setDraft({ ...draft, rules: event.target.value })} />
          </label>
        </div>
      </div>
      <VoiceJourneyHint step={2} />
      <button type="button" className="btn secondary" onClick={() => { openEditor(); setPage('full') }}>Полный редактор</button>
      {worldIssue && <p className="small muted">{worldIssue}</p>}
      <button type="button" className="btn" disabled={!!worldIssue} onClick={() => startStory(draft, playWorld)}>Начать историю</button>
    </main>}
    {page === 'full' && <VoiceFullEditor draft={fullDraft} cover={cover} onCoverChange={changeCover} onChange={setFullDraft} onClose={returnToDraft} onPreview={updated => startStory(updated, fullDraft)} />}
    {activeWorld && (page === 'channel' || page === 'compose' || page === 'result') && <VoiceOwnWorldPlay
      step={page}
      draft={activeWorld.draft}
      cover={activeWorld.cover}
      postText={postText}
      posts={activeWorld.posts}
      publishedText={activeWorld.posts[postIndex]}
      hint={<VoiceJourneyHint step={3} />}
      onText={text => updateActiveWorld(world => ({ ...world, postDraft: text }))}
      onCompose={() => setPage('compose')}
      onPublish={() => {
        if (!postText.trim()) return
        updateActiveWorld(world => ({ ...world, posts: [...world.posts, postText.trim()], postDraft: null }))
        setPostIndex(activeWorld.posts.length)
        setPage('result')
      }}
      onChannel={() => setPage('channel')}
      onRead={index => { setPostIndex(index); setPage('result') }}
    />}
    {(page === 'channel' || page === 'result') && <VoiceWorldNav active="channel" onWorlds={() => setPage('worlds')} onChannel={() => setPage('channel')} />}
  </div>
}

if (import.meta.env.DEV) {
  createRoot(document.getElementById('root')!).render(<App />)
}
