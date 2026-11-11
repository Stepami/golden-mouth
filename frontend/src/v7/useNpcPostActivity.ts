import {useEffect, useRef} from 'react';
import {api} from '../api/client';
import {worldDocumentOf} from '../api/worldContent';
import {readLocal, saveLocal} from './backend';
import {npcPostActivity, type Activity} from './activity';

/** Local inbox of confirmed posts in the active history; no bot delivery/API history. */
export function useNpcPostActivity(owner: string | undefined, worldId: string | undefined, active: boolean, onNew: (items: Activity[]) => void, onStorageError: () => void) {
  const notify = useRef(onNew), storageError = useRef(onStorageError);
  notify.current = onNew; storageError.current = onStorageError;
  useEffect(() => {
    if (!owner || !worldId || !active) return;
    const key = `reki:v7:${owner}:npc-posts:${worldId}`;
    const seen = new Set(readLocal<string[]>(key, []));
    const controller = new AbortController();
    let running = false;
    const check = async () => {
      if (running || document.visibilityState !== 'visible' || controller.signal.aborted) return;
      running = true;
      try {
        const page = await api.feed(worldId, undefined, controller.signal);
        const fresh: Activity[] = [];
        // Preserve feed order. Failed detail requests remain eligible for the next check.
        for (const item of page.items) {
          if (controller.signal.aborted) return;
          if (item.demo || item.kind !== 'post' || !item.postId || seen.has(item.postId)) continue;
          try {
            const post = await api.post(item.postId);
            if (controller.signal.aborted) return;
            const entry = npcPostActivity(item, post, worldDocumentOf(worldId));
            if (entry) fresh.push(entry);
            seen.add(item.postId);
          } catch { /* Retry on the next poll; never invent a notification. */ }
        }
        if (controller.signal.aborted) return;
        if (fresh.length) notify.current(fresh);
        if (!saveLocal(key, [...seen])) storageError.current();
      } catch { /* Keep the inbox and drafts intact while offline. */ }
      finally { running = false; }
    };
    void check();
    const timer = window.setInterval(() => void check(), 30_000);
    const resume = () => void check();
    window.addEventListener('focus', resume);
    window.addEventListener('online', resume);
    document.addEventListener('visibilitychange', resume);
    return () => {
      controller.abort(); window.clearInterval(timer);
      window.removeEventListener('focus', resume);
      window.removeEventListener('online', resume);
      document.removeEventListener('visibilitychange', resume);
    };
  }, [owner, worldId, active]);
}
