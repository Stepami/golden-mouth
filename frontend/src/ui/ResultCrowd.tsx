import { useEffect, useRef, useState } from 'react'
import type { WorldId } from '../api/types'
import type { CrowdCue } from './resultHaptics'

const comparisonPlaces = [
  { name: 'Териберки', population: 550, year: 2021, source: 'https://www.wikidata.org/wiki/Q4353950#P1082' },
  { name: 'Плёса', population: 1_896, year: 2021, source: 'https://www.wikidata.org/wiki/Q179486#P1082' },
  { name: 'Суздаля', population: 9_286, year: 2021, source: 'https://www.wikidata.org/wiki/Q15757#P1082' },
  { name: 'Великого Устюга', population: 28_670, year: 2021, source: 'https://iep.kolasc.net.ru/journal/wp-content/uploads/10-3-2023_SiR.pdf' },
  { name: 'Выборга', population: 72_530, year: 2021, source: 'https://www.wikidata.org/wiki/Q14657#P1082' },
  { name: 'Сергиева Посада', population: 101_756, year: 2021, source: 'https://rosstat.gov.ru/vpn/2020/Tom1_Chislennost_i_razmeshchenie_naseleniya' },
  { name: 'Сочи', population: 466_078, year: 2021, source: 'https://rosstat.gov.ru/vpn/2020/Tom1_Chislennost_i_razmeshchenie_naseleniya' },
  { name: 'Ярославля', population: 567_443, year: 2024, source: 'https://www.yarduma.ru/upload/iblock/395/mpvhvkrppwu8t0rno8l39xpf3etrd3ht/%D0%9D%D0%B0%D1%81%D0%B5%D0%BB%D0%B5%D0%BD%D0%B8%D0%B5%20%D0%9C%D0%9E%20%D0%AF%D0%9E%20%D0%BD%D0%B0%2001.01.2024.pdf' },
  { name: 'Казани', population: 1_308_660, year: 2021, source: 'https://rosstat.gov.ru/vpn/2020/Tom1_Chislennost_i_razmeshchenie_naseleniya' },
  { name: 'Санкт-Петербурга', population: 5_601_911, year: 2021, source: 'https://rosstat.gov.ru/vpn/2020/Tom1_Chislennost_i_razmeshchenie_naseleniya' },
  { name: 'Москвы', population: 13_010_112, year: 2021, source: 'https://rosstat.gov.ru/vpn/2020/Tom1_Chislennost_i_razmeshchenie_naseleniya' },
]

const fractionLabel = new Intl.NumberFormat('ru-RU', { maximumSignificantDigits: 2 })
const placesKey = 'reki:publication-places'
let placeHistory: Record<string, string> = {}
try {
  const saved = JSON.parse(localStorage.getItem(placesKey) ?? '{}')
  if (saved && typeof saved === 'object' && !Array.isArray(saved)) placeHistory = saved
} catch { /* Use the in-memory history when storage is unavailable. */ }

function comparisonPlace(publication: string, delta: number) {
  const candidates = comparisonPlaces.filter(place => place.population >= delta).slice(0, 3)
  const saved = candidates.find(place => place.name === placeHistory[publication])
  if (saved) return saved
  const recent = Object.values(placeHistory).slice(-2)
  const fresh = candidates.filter(place => !recent.includes(place.name))
  const pool = fresh.length ? fresh : candidates
  const place = pool[Math.floor(Math.random() * pool.length)]
  if (place) {
    placeHistory[publication] = place.name
    try { localStorage.setItem(placesKey, JSON.stringify(placeHistory)) } catch { /* Keep the assignment for this session. */ }
  }
  return place
}
const pulsedPublications = new Set<string>()
let cheerOffset = 0

export function ResultCrowd({ delta, publication, world, avatarStyle, onPulse }: {
  delta: number; publication: string; world: WorldId; avatarStyle?: string; onPulse?: (phase: CrowdCue) => void
}) {
  const canvas = useRef<HTMLCanvasElement>(null)
  const [unavailable, setUnavailable] = useState(false)
  const [place, setPlace] = useState<(typeof comparisonPlaces)[number]>()
  useEffect(() => {
    setPlace(delta > 0 ? comparisonPlace(publication, delta) : undefined)
  }, [publication, delta])

  useEffect(() => {
    const target = canvas.current
    if (!target) return
    const ctx = target.getContext('2d')
    if (!ctx) { setUnavailable(true); return }

    const motion = window.matchMedia('(prefers-reduced-motion: reduce)')
    let still = motion.matches || document.hidden || new URLSearchParams(location.search).has('shot')
    let disposed = false
    let stop: ((settle?: boolean) => void) | undefined
    const finish = () => {
      still = true
      stop?.(true)
    }
    const visibility = () => { if (document.hidden) finish() }
    const motionChange = () => { if (motion.matches) finish() }
    document.addEventListener('visibilitychange', visibility)
    motion.addEventListener('change', motionChange)
    window.addEventListener('pagehide', finish)
    const offset = cheerOffset
    cheerOffset += 7
    void import('./drawResultCrowd').then(async ({ prepareCrowd }) => {
      if (disposed) return
      const play = await prepareCrowd({ ctx, delta, world, avatarStyle, offset })
      if (disposed) return
      const mayPulse = !!onPulse && !still && !pulsedPublications.has(publication)
      if (mayPulse) pulsedPublications.add(publication)
      stop = play(still, phase => { if (mayPulse && !still && !document.hidden) onPulse(phase) })
    }).catch(() => { if (!disposed) setUnavailable(true) })

    return () => {
      disposed = true
      stop?.()
      document.removeEventListener('visibilitychange', visibility)
      motion.removeEventListener('change', motionChange)
      window.removeEventListener('pagehide', finish)
    }
  }, [delta, publication, world, avatarStyle, onPulse])

  const percent = place ? delta / place.population * 100 : 0
  if (!delta || (unavailable && !place)) return null
  return <div className="post-result-crowd">
    {!unavailable && <canvas ref={canvas} aria-hidden="true" />}
    {place && <p className="post-result-fact">Это примерно <b>{percent < 10 ? fractionLabel.format(percent) : Math.round(percent)}% населения {place.name}</b>.</p>}
  </div>
}
