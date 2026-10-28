import { useCallback } from 'react'
import { api } from '../api/client'
import { contentOf, worldOf } from '../api/worldContent'
import type { FeedItem } from '../api/types'
import { usePlayer } from '../app/store'
import { useNav } from '../app/nav'
import { usePostPages } from '../app/usePostPages'
import { Avatar, Button } from '../ui'
import { GameHeader } from '../ui/GameHeader'
import { WorldCover } from '../ui/WorldCover'
import { FeedPostCard } from './day'
import { register } from '.'

function Participants() {
  const p = usePlayer(); const nav = useNav(); const stars = contentOf(p.world).stars
  return <div className="screen participants-screen"><GameHeader /><section className="participants-list"><h1>В этом мире</h1>
    {stars.map(star => <button key={star.id} onClick={() => nav.push('03-7_npc_profile', { starId: star.id })}><Avatar seed={star.avatarSeed} world={p.world} mode="default" size={44} /><span><strong>{star.name}</strong><small>{star.role}</small></span><span aria-hidden="true">→</span></button>)}
    {!stars.length && <p className="muted">В этом мире пока нет собеседников.</p>}
  </section></div>
}

function NpcProfile() {
  const p = usePlayer(); const nav = useNav()
  const star = contentOf(p.world).stars.find(star => star.id === nav.params.starId)
  const load = useCallback(async (cursor: string | undefined, signal: AbortSignal) => {
    if (!star) return { items: [], nextCursor: null }
    const page = await api.feed(p.worldInstanceId, cursor, signal)
    const found: FeedItem[] = []
    // Feed has no authorId or author filter. Resolve identity from real post details,
    // in small batches, rather than guessing it from a duplicate name/avatar.
    const posts = page.items.filter(item => item.kind === 'post' && item.postId)
    for (let i = 0; i < posts.length; i += 4) {
      if (signal.aborted) throw new DOMException('Aborted', 'AbortError')
      const batch = await Promise.all(posts.slice(i, i + 4).map(async item => ({ item, detail: await api.post(item.postId!) })))
      for (const { item, detail } of batch) if (detail.authorId === `Star_${star?.id}`) found.push({ ...item, starId: star?.id })
    }
    return { items: found, nextCursor: page.nextCursor }
  }, [p.worldInstanceId, star?.id])
  const list = usePostPages<FeedItem>(`npc-posts:${p.id}:${p.worldInstanceId}:${star?.id}`, load)
  if (!star) return <div className="screen"><GameHeader /><section className="pad"><h1>Персонаж недоступен</h1><p>В текущем мире такого собеседника нет.</p><Button onClick={() => nav.replace('03-6_participants')}>Все участники</Button></section></div>
  return <div className="screen identity-screen npc-profile"><GameHeader /><section className="social-profile">
    <div className="profile-cover"><WorldCover world={p.world} alt={`Обложка мира ${worldOf(p.world).title}`} /></div>
    <div className="profile-identity-row"><Avatar seed={star.avatarSeed} mode="default" world={p.world} size={72} /><button className="profile-edit" onClick={() => nav.replace('03-6_participants')}>Все участники</button></div>
    <h1>{star.name}</h1><p className="profile-handle">ИИ-персонаж</p><p className="profile-role">{star.role}</p>{star.about && <p className="profile-bio">{star.about}</p>}<div className="profile-meta"><span>📍 {worldOf(p.world).title}</span></div>
  </section><div className="profile-posts-heading"><strong>Записи</strong><span>{list.ready && !list.hasNext ? list.items.length : 'Из ленты мира'}</span></div>
    {list.items.map(item => <FeedPostCard key={item.postId} item={item} />)}
    {list.pending && <p className="pad muted" role="status">Загружаем записи персонажа…</p>}
    {list.error && <div className="pad" role="alert"><p>Не удалось загрузить записи.</p><Button secondary onClick={() => void (list.error === 'more' ? list.loadMore() : list.refresh())}>Повторить</Button></div>}
    {list.ready && !list.pending && !list.error && !list.items.length && <p className="pad muted">{list.hasNext ? 'Среди загруженных записей пока нет постов этого персонажа.' : 'У этого персонажа пока нет записей.'}</p>}
    {list.hasNext && !list.pending && <div className="pad"><Button secondary onClick={() => void list.loadMore()}>Загрузить ещё записи мира</Button></div>}
  </div>
}

register({ '03-6_participants': { component: Participants, title: 'Участники мира' }, '03-7_npc_profile': { component: NpcProfile, title: 'Профиль персонажа' } }, { '03-6_participants': 1, '03-7_npc_profile': 1 })
