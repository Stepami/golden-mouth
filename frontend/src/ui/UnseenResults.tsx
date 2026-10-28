import { useEffect, useState } from 'react'
import { usePlayer } from '../app/store'
import { useNav } from '../app/nav'
import { resultHistory, type SavedResult } from '../app/resultHistory'
import { PublicationFloat } from './PublicationFloat'

/** Device-local confirmations only. Do not interrupt a form, sheet or onboarding. */
export function UnseenResults() {
  const p = usePlayer(); const nav = useNav(); const worldKey = `${p.id}:${p.worldInstanceId ?? p.world}`
  const [version, setVersion] = useState(0)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [active, setActive] = useState<{ key: string; item: SavedResult }>()
  const eligible = ['03-3_feed','03-5_stage','05-13_progress','05-13b_results','05-5_collection','03-6_participants','03-7_npc_profile'].includes(nav.current)
  useEffect(() => {
    const change = () => setVersion(v => v + 1)
    const dialog = () => setDialogOpen(!!document.querySelector('[role="dialog"],dialog[open]'))
    const observer = new MutationObserver(dialog); observer.observe(document.body, { childList: true, subtree: true })
    window.addEventListener('reki:results-changed', change); window.addEventListener('storage', change); dialog()
    return () => { observer.disconnect(); window.removeEventListener('reki:results-changed', change); window.removeEventListener('storage', change) }
  }, [])
  useEffect(() => {
    if (!eligible || dialogOpen) { if (active) setActive(undefined); return }
    if (active && active.key !== worldKey) { setActive(undefined); return }
    if (active?.key === worldKey) return
    // Older history without seen metadata was already delivered; do not reopen it.
    const item = resultHistory(p).filter(item => item.seen === false).at(-1)
    if (item) setActive({ key: worldKey, item })
  }, [eligible, dialogOpen, version, worldKey, active])
  if (!eligible || dialogOpen || active?.key !== worldKey) return null
  return <PublicationFloat savedResult={active.item.result} onClose={() => { setActive(undefined); setVersion(v => v + 1) }} />
}
