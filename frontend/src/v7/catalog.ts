import type { World } from './domain/types';

export type CatalogEntry = {
  key: string;
  world: World;
  available: boolean;
  storyVariant?: 'hogwarts';
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

/** A distinct UI entry backed by the real school template, never a synthetic API ID. */
export function withHogwartsCard(entries: CatalogEntry[], story: { templateId: string; scenario: string; premise: string; subtitle: string }): CatalogEntry[] {
  return entries.flatMap(entry => entry.available && entry.world.id === story.templateId && !entry.storyVariant
    ? [entry, { key: `story:${story.templateId}:hogwarts`, available: true, storyVariant: 'hogwarts' as const,
      world: { ...entry.world, title: story.scenario, description: story.premise, tagline: story.subtitle, members: '' } }]
    : [entry]);
}
