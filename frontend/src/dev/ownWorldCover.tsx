import { useId, useState } from 'react'
import './ownWorldCover.css'

export type VoiceCover = { url: string; x: number; y: number }

export function VoiceCoverImage({ cover }: { cover: VoiceCover }) {
  return <img className="voice-world-cover" src={cover.url} alt="" style={{ objectPosition: `${cover.x}% ${cover.y}%` }} />
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
    {error && <p className="small voice-error" role="alert">{error}</p>}
    {cover && <details className="editor-extra voice-cover-adjust">
      <summary>Подогнать кадр</summary>
      <label>По горизонтали<input type="range" min={0} max={100} value={cover.x} onChange={event => onChange({ ...cover, x: Number(event.target.value) })} /></label>
      <label>По вертикали<input type="range" min={0} max={100} value={cover.y} onChange={event => onChange({ ...cover, y: Number(event.target.value) })} /></label>
    </details>}
  </div>
}
