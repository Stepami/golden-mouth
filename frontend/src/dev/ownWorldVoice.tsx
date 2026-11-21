import { useEffect, useRef, useState } from 'react'
import { createRoot } from 'react-dom/client'
import '../theme.css'
import './ownWorldVoice.css'
import { useVoiceFullDraft, VoiceFullEditor, VoicePortraits, voiceDraftSummary, voiceWorldIssue } from './ownWorldFullEditor'
import { VoiceOwnWorldPlay } from './ownWorldPlayPreview'

const story = 'Я доставляю письма в городе над облаками. Сегодня картограф Лис нашёл посылку с адресом острова, который исчез сто лет назад. Летать между островами можно только на рассвете.'
const prompts = [
  { icon: '🌍', label: 'Место', example: 'Город над облаками' },
  { icon: '👥', label: 'Герои', example: 'Курьер и картограф' },
  { icon: '⚡', label: 'Завязка', example: 'Пришла странная посылка' },
  { icon: '📜', label: 'Правило', example: 'Полёты только на рассвете' },
]
const sampleDraft = {
  title: 'Город над облаками',
  opening: 'Город стоит на летающих островах. Сегодня нашлась посылка с адресом острова, который исчез сто лет назад.',
  people: 'Ты доставляешь письма между островами. Картограф Лис знает старые маршруты и приносит тебе странную посылку.',
  rules: 'Перелетать между островами можно только на рассвете.',
}

function release(stream: MediaStream) {
  stream.getTracks().forEach(track => track.stop())
}

function Recorder({ audio, onAudio, onText, onNext }: {
  audio: string
  onAudio: (url: string) => void
  onText: () => void
  onNext: () => void
}) {
  const recorder = useRef<MediaRecorder | null>(null)
  const mounted = useRef(false)
  const [phase, setPhase] = useState<'idle' | 'permission' | 'recording' | 'stopping'>('idle')
  const [elapsed, setElapsed] = useState(0)
  const [error, setError] = useState('')

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
          onAudio(URL.createObjectURL(new Blob(chunks, { type: current.mimeType })))
        } else {
          setError('Запись получилась пустой. Попробуй ещё раз.')
        }
      }
      recorder.current = current
      current.start()
      onAudio('')
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
    {audio && <audio controls src={audio} aria-label="Прослушать запись" />}
    {error && <p className="voice-error" role="alert">{error}</p>}
    <button type="button" className="btn"
      disabled={busy}
      onClick={recording ? stop : audio ? onNext : record}
    >
      {!busy && !recording && !audio && <svg className="voice-mic" viewBox="0 0 20 20" aria-hidden="true">
        <rect x="7" y="2" width="6" height="10" rx="3" />
        <path d="M4 9a6 6 0 0 0 12 0M10 15v3M7 18h6" />
      </svg>}
      {phase === 'permission' ? 'Ждём доступ к микрофону…' : phase === 'stopping' ? 'Завершаем запись…'
        : recording ? 'Остановить запись' : audio ? 'Далее' : 'Записать голос'}
    </button>
    <div className="voice-alternatives">
      <button type="button" className="text-link" onClick={onText}>Напечатать историю</button>
      {audio
        ? <button type="button" className="text-link" disabled={busy} onClick={record}>Перезаписать</button>
        : <button type="button" className="text-link" onClick={onNext}>Показать пример</button>}
    </div>
    <p className="small muted voice-note">Запись остаётся в этом браузере.</p>
  </div>
}

type Page = 'story' | 'listening' | 'transcript' | 'assembling' | 'draft' | 'full' | 'opening' | 'channel' | 'compose' | 'result'
type LoadingPage = 'listening' | 'assembling' | 'opening'

const loadingSteps: Record<LoadingPage, { title: string; caption: string; next: Page; duration: number }> = {
  listening: { title: 'Слушаем твою историю', caption: 'Следом можно проверить текст.', next: 'transcript', duration: 1400 },
  assembling: { title: 'Собираем твой мир', caption: 'Место, герои, завязка и правила.', next: 'draft', duration: 1900 },
  opening: { title: 'В твоём мире зажигаются огни', caption: 'Готовим канал к первому посту.', next: 'channel', duration: 1200 },
}

const pageTitle: Record<Page, string> = {
  story: 'Свой мир', listening: 'Расшифровка', transcript: 'Расшифровка', assembling: 'Черновик мира',
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
  const [page, setPage] = useState<Page>('story')
  const [typing, setTyping] = useState(false)
  const [text, setText] = useState('')
  const [audio, setAudio] = useState('')
  const [transcript, setTranscript] = useState(story)
  const [draft, setDraft] = useState(sampleDraft)
  const [postDraft, setPostDraft] = useState<string | null>(null)
  const [publishedText, setPublishedText] = useState('')
  const { draft: fullDraft, previewDraft: playWorld, setDraft: setFullDraft, openEditor } = useVoiceFullDraft(draft)
  const returnToDraft = (updated: typeof sampleDraft) => {
    setDraft(updated)
    setPage('draft')
  }
  const startStory = (updated: typeof sampleDraft) => {
    setDraft(updated)
    setPage('opening')
  }
  const phase = page === 'listening' || page === 'assembling' || page === 'opening' ? page : undefined
  const worldIssue = voiceWorldIssue(playWorld)
  const postText = postDraft ?? fullDraft.postIdeas.find(idea => idea.trim()) ?? ''
  const previousPage: Partial<Record<Page, Page>> = {
    assembling: typing ? 'story' : 'transcript', draft: typing ? 'story' : 'transcript',
    opening: 'draft', channel: 'draft', compose: 'channel', result: 'channel',
  }
  const heading = useRef<HTMLHeadingElement>(null)
  const textInput = useRef<HTMLTextAreaElement>(null)

  useEffect(() => {
    if (!audio) return
    return () => URL.revokeObjectURL(audio)
  }, [audio])

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
    const timer = window.setTimeout(() => setPage(step.next), reduced ? 300 : step.duration)
    return () => window.clearTimeout(timer)
  }, [phase])

  return <div className="app voice-preview">
    <header className="top">
      {page !== 'story' && <button
        type="button"
        className="back"
        aria-label="Назад"
        onClick={() => page === 'full' ? returnToDraft(voiceDraftSummary(fullDraft)) : setPage(previousPage[page] ?? 'story')}
      >‹</button>}
      <b className="top-title">{pageTitle[page]}</b>
      <span className="pill">Превью</span>
    </header>
    {phase && <LoadingScreen phase={phase} />}
    {page === 'story' && <main className="pad voice-story">
      <h1>Расскажи —<br />и окажись внутри</h1>
      <ul className="voice-prompts" aria-label="Что рассказать о мире">
        {prompts.map(({ icon, label, example }) => <li className="card" key={label}>
          <span className="voice-prompt-icon" aria-hidden="true">{icon}</span>
          <b>{label}</b>
          <span className="small muted">{example}</span>
        </li>)}
      </ul>
      {typing ? <div className="voice-recorder">
        <label className="voice-field">
          Твоя история
          <textarea ref={textInput} rows={3} value={text} onChange={event => setText(event.target.value)} placeholder="Всё начинается в…" />
        </label>
        <button type="button" className="btn" disabled={!text.trim()} onClick={() => setPage('assembling')}>Собрать черновик</button>
        <button type="button" className="text-link voice-choose-mode" onClick={() => setTyping(false)}>Записать голосом</button>
      </div> : <Recorder audio={audio} onAudio={setAudio} onText={() => setTyping(true)} onNext={() => setPage('listening')} />}
    </main>}
    {page === 'transcript' && <main className="pad voice-review">
      <div className="voice-intro">
        <h1 ref={heading} tabIndex={-1}>Проверь, что получилось</h1>
        <p className="muted">Это пример расшифровки. Здесь можно поправить имена и детали.</p>
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
        <p className="muted">Пример. Детали можно менять.</p>
      </div>
      <div className="card">
        <div className="voice-fields">
          <label className="voice-field">
            Название
            <input value={draft.title} onChange={event => setDraft({ ...draft, title: event.target.value })} />
          </label>
          <label className="voice-field">
            Мир и завязка
            <textarea rows={3} value={draft.opening} onChange={event => setDraft({ ...draft, opening: event.target.value })} />
          </label>
          <VoicePortraits draft={fullDraft} />
          <label className="voice-field">
            Правила мира
            <textarea rows={2} value={draft.rules} onChange={event => setDraft({ ...draft, rules: event.target.value })} />
          </label>
        </div>
      </div>
      <button type="button" className="btn secondary" onClick={() => { openEditor(); setPage('full') }}>Полный редактор</button>
      {worldIssue && <p className="small muted">{worldIssue}</p>}
      <button type="button" className="btn" disabled={!!worldIssue} onClick={() => { openEditor(); startStory(draft) }}>Начать историю</button>
    </main>}
    {page === 'full' && <VoiceFullEditor draft={fullDraft} onChange={setFullDraft} onClose={returnToDraft} onPreview={startStory} />}
    {(page === 'channel' || page === 'compose' || page === 'result') && <VoiceOwnWorldPlay
      step={page}
      draft={playWorld}
      postText={postText}
      publishedText={publishedText}
      onText={setPostDraft}
      onCompose={() => setPage('compose')}
      onPublish={() => {
        if (!postText.trim()) return
        setPublishedText(postText.trim())
        setPage('result')
      }}
      onChannel={() => setPage('channel')}
      onRead={() => setPage('result')}
    />}
  </div>
}

if (import.meta.env.DEV) {
  createRoot(document.getElementById('root')!).render(<App />)
}
