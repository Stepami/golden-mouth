import { useEffect } from 'react'
import { useNav } from '../app/nav'
import { usePlayer } from '../app/store'
import { markResultSeen, resultHistory } from '../app/resultHistory'
import type { PostResult } from '../api/types'
import { PublicationResult } from './PublicationResult'
import { Icon } from './Icon'

/** Non-modal confirmation above the real channel; reading and navigation remain available. */
export function PublicationFloat({ savedResult, onClose }: { savedResult?: PostResult; onClose?: () => void } = {}) {
  const nav = useNav(); const p = usePlayer()
  const result = savedResult ?? nav.params.result as PostResult | undefined
  const close = onClose ?? (() => nav.replace('03-5_stage'))
  useEffect(() => { if (result) markResultSeen(p, result.postId) }, [p.id, p.worldInstanceId, result?.postId])
  if (!result) return null
  const saved = resultHistory(p).find(item => item.post.id === result.postId)
  return <aside className={`publication-result-float publication-dialog results-peek${result.delta < 0 ? ' negative' : ''}`} aria-label="Результат публикации" onKeyDown={e => { if (e.key === 'Escape') close() }}>
    <div className="result-float-heading"><span role="status">Запись опубликована</span><button aria-label="Скрыть результат" className="icon-button" onClick={close}><Icon name="close" size={20} /></button></div>
    <PublicationResult result={result} skills={saved?.skills} />
  </aside>
}
