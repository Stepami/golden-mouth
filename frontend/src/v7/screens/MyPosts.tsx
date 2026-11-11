import { useEffect, useState, type ReactNode } from 'react';
import { api } from '../../api/client';
import type { ChannelPost } from '../../api/types';
import { errorText, formatDate } from '../backend';
import type { Person, Post } from '../domain/types';
import { Button, EmptyState, Notice } from '../ui';

export function MyPosts({ worldId, author, renderPost, onCompose }: {
  worldId: string;
  author: Person;
  renderPost: (post: Post, author: Person, count: number) => ReactNode;
  onCompose: () => void;
}) {
  const [posts, setPosts] = useState<ChannelPost[]>([]);
  const [cursor, setCursor] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  async function load(more?: string, signal?: AbortSignal) {
    setLoading(true); setError('');
    try {
      const page = await api.channelPosts(worldId, more, signal);
      if (signal?.aborted) return;
      setPosts(old => more ? [...old, ...page.items.filter(item => !old.some(p => p.postId === item.postId))] : page.items);
      setCursor(page.nextCursor);
    } catch (cause) {
      if (!signal?.aborted) setError(errorText(cause));
    } finally {
      if (!signal?.aborted) setLoading(false);
    }
  }

  useEffect(() => {
    const controller = new AbortController();
    void load(undefined, controller.signal);
    return () => controller.abort();
  }, [worldId]);

  return <>
    {error && <section className="section"><Notice title="Не удалось загрузить твои посты" tone="error">{error}</Notice></section>}
    {!posts.length && !loading && !error && <EmptyState title="Здесь появятся твои посты" action={<Button onClick={onCompose}>Написать пост</Button>}>Твои публикации в этом прохождении — от новых к старым.</EmptyState>}
    {posts.map(item => renderPost({ id: item.postId, authorId: author.id, text: item.text, createdAt: formatDate(item.createdAtUtc), likes: item.reactions.reduce((sum, r) => sum + r.count, 0) }, author, item.commentCount))}
    {(posts.length > 0 || loading || error) && <section className="section feed-footer"><Button variant="ghost" busy={loading} onClick={() => void load(cursor ?? undefined)}>{loading ? 'Загружаем твои посты' : error ? 'Повторить' : cursor ? 'Показать ещё' : 'Обновить мои посты'}</Button></section>}
  </>;
}
