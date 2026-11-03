import type { World } from './domain/types';

export type CatalogEntry = {
  key: string;
  world: World;
  available: boolean;
};

export function catalogEntries(available: World[], planned: World[]): CatalogEntry[] {
  return [
    ...available.map(world => ({ key: world.id, world, available: true })),
    ...planned.map(world => ({ key: `planned:${world.id}`, world, available: false })),
  ];
}

export function filterCatalog(entries: CatalogEntry[], query: string, genre: string): CatalogEntry[] {
  const term = query.trim().toLocaleLowerCase('ru');
  return entries.filter(({ world }) =>
    (genre === 'Все' || world.genre === genre) &&
    `${world.title} ${world.description}`.toLocaleLowerCase('ru').includes(term));
}
