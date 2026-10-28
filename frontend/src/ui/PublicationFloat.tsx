import { useNav } from '../app/nav'
import { usePlayer } from '../app/store'
import { resultHistory } from '../app/resultHistory'
import type { PostResult } from '../api/types'
import { PublicationResult } from './PublicationResult'
import { Icon } from './Icon'

/** Non-modal confirmation above the real channel; reading and navigation remain available. */
export function PublicationFloat() {
  const nav = useNav(); const p = usePlayer()
  const result = nav.params.result as PostResult | undefined
  if (!result) return null
  const saved = resultHistory(p).find(item => item.post.id === result.postId)
  return <aside className="publication-result-float" aria-label="Результат публикации" onKeyDown={e => { if (e.key === 'Escape') nav.replace('03-5_stage') }}>
    <div className="result-float-heading"><span role="status">Запись опубликована</span><button aria-label="Скрыть результат" className="icon-button" onClick={() => nav.replace('03-5_stage')}><Icon name="close" size={20} /></button></div>
    <PublicationResult result={result} skills={saved?.skills} />
  </aside>
}
