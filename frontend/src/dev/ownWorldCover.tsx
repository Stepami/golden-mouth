import { useId, useState } from 'react'
import evening from '../assets/evening-960-v4.webp'
import houses from '../assets/houses-960-v4.webp'
import offices from '../assets/it-960-v4.webp'
import { exampleCover } from './ownWorldExample'
import './ownWorldCover.css'

export type VoiceCover = { url: string; x: number; y: number }

const library = [
  { title: 'Вечерний город', url: evening },
  { title: exampleCover.title, url: exampleCover.url, tags: /пхукет|патонг|скутер|мотопрокат|байк/i },
  { title: 'Северный замок', url: houses, tags: /замок|север|снег|королев|остров|магия/i },
  { title: 'Большой офис', url: offices, tags: /офис|стартап|технолог|програм|работа/i },
]

export function suggestVoiceCover(idea: string): VoiceCover {
  const option = library.find(item => item.tags?.test(idea)) ?? library[0]
  return { url: option.url, x: 50, y: 50 }
}

export function VoiceCoverImage({ cover }: { cover: VoiceCover }) {
  return <img key={cover.url} className="voice-world-cover" src={cover.url} alt="" style={{ objectPosition: `${cover.x}% ${cover.y}%` }} />
}

export function VoiceCoverEditor({ cover, title, description, onChange }: {
  cover: VoiceCover | null
  title: string
  description: string
  onChange: (cover: VoiceCover | null) => void
}) {
  const inputId = useId()
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [showLibrary, setShowLibrary] = useState(false)

  async function pick(file: File) {
    setError('')
    setBusy(true)
    const url = URL.createObjectURL(file)
    const image = new Image()
    image.src = url

    try {
      await image.decode()
      onChange({ url, x: 50, y: 50 })
    } catch {
      URL.revokeObjectURL(url)
      setError('Не удалось открыть изображение. Попробуй другой файл.')
    } finally {
      setBusy(false)
    }
  }

  return <div className="voice-cover-editor">
    <figure className="card voice-cover-catalog" aria-label="Карточка в каталоге">
      {cover ? <VoiceCoverImage cover={cover} /> : <div className="voice-cover-empty muted">Без обложки</div>}
      <figcaption className="voice-cover-copy">
        <span className="tiny muted">В каталоге</span>
        <b>{title.trim() || 'Твой мир'}</b>
        {description.trim() && <span className="small muted">{description}</span>}
      </figcaption>
    </figure>
    <div className="voice-cover-actions">
      <button type="button" className="text-button" aria-expanded={showLibrary} onClick={() => setShowLibrary(!showLibrary)}>Из библиотеки</button>
      <label className="voice-cover-file text-button" aria-disabled={busy || undefined} htmlFor={inputId}>
        {cover ? 'Заменить обложку' : 'Загрузить обложку'}
        <input
          id={inputId}
          type="file"
          accept="image/*"
          disabled={busy}
          onChange={event => {
            const file = event.currentTarget.files?.[0]
            event.currentTarget.value = ''
            if (file) void pick(file)
          }}
        />
      </label>
      {cover && <button type="button" className="text-button" disabled={busy} onClick={() => { setError(''); onChange(null) }}>Убрать</button>}
    </div>
    {showLibrary && <div className="voice-cover-library" aria-label="Обложки из библиотеки">
      {library.map(({ title, url }) => <button type="button" className="card" key={title} aria-pressed={cover?.url === url} onClick={() => { setError(''); onChange({ url, x: 50, y: 50 }); setShowLibrary(false) }}>
        <img src={url} alt="" /><span>{title}</span>
      </button>)}
    </div>}
    {error && <p className="small voice-error" role="alert">{error}</p>}
    {cover && <details className="editor-extra voice-cover-adjust">
      <summary>Подогнать кадр</summary>
      <label>По горизонтали<input type="range" min={0} max={100} value={cover.x} onChange={event => onChange({ ...cover, x: Number(event.target.value) })} /></label>
      <label>По вертикали<input type="range" min={0} max={100} value={cover.y} onChange={event => onChange({ ...cover, y: Number(event.target.value) })} /></label>
    </details>}
  </div>
}
