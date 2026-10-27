import { useEffect, useState } from 'react'

/** Decorative reveal. Assistive technology receives the complete sentence once. */
export function Typewriter({ text, cursor = false }: { text: string; cursor?: boolean }) {
  const [length, setLength] = useState(() => matchMedia('(prefers-reduced-motion: reduce)').matches ? text.length : 0)
  useEffect(() => {
    if (matchMedia('(prefers-reduced-motion: reduce)').matches || new URLSearchParams(location.search).has('shot')) { setLength(text.length); return }
    setLength(0)
    const start = performance.now()
    let frame = 0
    const tick = (now: number) => {
      const count = Math.min(text.length, Math.ceil((now - start) / (cursor ? 28 : 7)))
      setLength(count)
      if (count < text.length) frame = requestAnimationFrame(tick)
    }
    frame = requestAnimationFrame(tick)
    return () => cancelAnimationFrame(frame)
  }, [text, cursor])
  return <span className="typewriter"><span className="sr-only">{text}</span><span aria-hidden="true">{text.slice(0, length)}{cursor && length < text.length && <i className="typing-cursor" />}<span className="typewriter-reserve">{text.slice(length)}</span></span></span>
}
