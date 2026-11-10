import type { CatalogEntry } from './catalog';

export const communicationInterests = [
  'Смелее высказываться', 'Лучше слышать других', 'Увереннее убеждать',
  'Находить общий язык', 'Легче шутить', 'Быть собой',
] as const;

// Editorial recommendations, not an AI assessment or a gameplay modifier.
const profiles: Record<string, number[]> = {
  'b3a1a522-0263-4796-a197-57a8731fc291': [1, 4, 1, 3, 1, 4],
  'aec9494b-4118-4525-8458-e85d759c8dd9': [2, 3, 2, 4, 2, 3],
  '8c02a7fb-3b55-4eb0-b957-1b4869ed9834': [4, 1, 4, 2, 0, 1],
  '5720946a-ea7c-4a10-9c50-51ce3deefc53': [3, 1, 3, 2, 4, 2],
};
export function rankCatalog(entries: CatalogEntry[], interests: readonly string[]): CatalogEntry[] {
  const selected = [...new Set(interests)].map(value => communicationInterests.indexOf(value as typeof communicationInterests[number])).filter(index => index >= 0);
  if (!selected.length) return [...entries];
  return entries.map((entry, index) => ({ entry, index, score: entry.available ? selected.reduce((sum, i) => sum + (profiles[entry.world.id]?.[i] ?? 0), 0) : 0 }))
    .sort((a, b) => Number(b.entry.available) - Number(a.entry.available) || b.score - a.score || a.index - b.index)
    .map(({ entry }) => entry);
}
