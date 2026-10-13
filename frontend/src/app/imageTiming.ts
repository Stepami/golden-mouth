import { event } from './analytics'

/** Only fixed game assets: no image URL or player data enters analytics. */
export function trackImage(image: HTMLImageElement | null, asset: string) {
  if (!/^(welcome_portrait|cover_(evening|sorcerers|houses|it))$/.test(asset)) return
  if (!image) { event('image_failed', { asset }); return }
  const entries = performance.getEntriesByName(image.currentSrc)
  const timing = entries[entries.length - 1] as PerformanceResourceTiming | undefined
  event('image_loaded', { asset, duration_ms: Math.round(timing?.duration ?? 0), transfer_bytes: timing?.transferSize ?? 0, decoded_bytes: timing?.decodedBodySize ?? 0 })
}
