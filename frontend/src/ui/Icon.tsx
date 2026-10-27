import type { CSSProperties } from 'react'

const paths = {
  home: 'm3 10 9-7 9 7v11h-6v-7H9v7H3Z',
  message: 'M21 4H3v14h5v4l5-4h8Z',
  plus: 'M12 4v16M4 12h16',
  chart: 'M4 20V12h4v8m2 0V7h4v13m2 0V3h4v17',
  back: 'm15 5-7 7 7 7',
  close: 'm6 6 12 12M18 6 6 18',
  settings: 'M4 6h16M4 12h16M4 18h16M8 3v6m8 0v6m-6 0v6',
  arrow: 'M5 12h14m-6-6 6 6-6 6',
  heart: 'M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1.1-1.1a5.5 5.5 0 0 0-7.8 7.8L12 21l8.8-8.6a5.5 5.5 0 0 0 0-7.8Z',
  views: 'M4 20v-5m5 5V9m5 11V4m5 16V11',
  check: 'm5 12 4 4L19 6',
} as const
export function Icon({ name, size = 24, style }: { name: keyof typeof paths; size?: number; style?: CSSProperties }) {
  return <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" style={style}><path d={paths[name]} /></svg>
}
