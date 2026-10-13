import type { Drama, Star, Want, WorldId, WorldDocument, WorldDefinition, World } from './types'

export interface WorldContent {
  where: string
  roles: string[]
  name: string
  handle: string
  avatarStyle: 'lorelei' | 'notionists' | 'pixelArt' | 'thumbs' | 'shapes'
  avatarLabel: string
  stars: Star[]
  voices: string[]
  drama: Drama
  description: string
  rules: string[]
  firstContext: string
  firstPrompt: string
  ideas: string[]
  posts: string[]
}

const documents: Partial<Record<WorldId, WorldDocument>> = {}
export const worlds: World[] = []
const templateDocuments: Partial<Record<WorldId, WorldDocument>> = {}

export function installWorldTemplates(templates: WorldDocument[]): void {
  for (const key of Object.keys(templateDocuments)) delete templateDocuments[key as WorldId]
  for (const template of templates) templateDocuments[template.balanceKey] = structuredClone(template)
  worlds.splice(0, worlds.length, ...templates.filter(world => world.balanceKey !== 'own').map(world => ({
    id: world.balanceKey, title: world.title, emoji: world.emoji, description: world.description,
    difficulty: world.difficulty, summary: world.summary,

  })))
}

const serverContent: Partial<Record<WorldId, WorldContent>> = {}
const traitNames = ['Дерзкость', 'Поддержка', 'Загадка', 'Воображение', 'Любопытство', 'Энергия', 'Ирония и расчёт', 'Сухой юмор', 'Самоирония', 'Азарт', 'Внимание']
export function installWorldContent(worlds: WorldDocument[]): void {
  for (const world of worlds) {
    documents[world.balanceKey] = world
    serverContent[world.balanceKey] = toWorldContent(world)
  }
}

function toWorldContent(world: WorldDocument): WorldContent {
  return {
    where: world.where, roles: world.roles, name: world.name, handle: world.avatar,
    avatarStyle: world.avatarStyle as WorldContent['avatarStyle'], avatarLabel: 'аватар канала',
    stars: world.stars.map(star => ({ ...star, trait: traitNames[star.trait] ?? '' })),
    voices: world.voices, drama: world.drama, description: world.description,
    rules: world.rules.map(rule => rule.text), firstContext: world.firstContext, firstPrompt: world.firstPrompt,
    ideas: world.postIdeas, posts: world.demoPosts,
  }
}

export function templateContentOf(world: string): WorldContent {
  const template = templateDocuments[world as WorldId]
  if (!template) throw new Error('World template has not been loaded')
  return toWorldContent(template)
}

export function contentOf(world: string): WorldContent {
  const content = serverContent[world as WorldId]
  if (!content) throw new Error('World content has not been loaded')
  return content
}

export function wantsFor(world: string): Want[] {
  const document = documents[world as WorldId]
  if (!document) throw new Error('World wants have not been loaded')
  return structuredClone(document.wants)
}
export function worldDocumentOf(world: string): WorldDocument {
  const document = documents[world as WorldId]
  if (!document) throw new Error('World has not been loaded')
  return structuredClone(document)
}

const WORLD_FORMS: Record<string, { gen: string; acc: string }> = {
  evening: { gen: 'Вечерело', acc: 'Вечерело' }, sorcerers: { gen: 'Школы чародеев', acc: 'Школу чародеев' }, houses: { gen: 'Игры домов', acc: 'Игру домов' },
  stage: { gen: 'Сцены', acc: 'Сцену' }, academy: { gen: 'Академии', acc: 'Академию' }, it: { gen: 'Айти-тусовки', acc: 'Айти-тусовку' },
  stream: { gen: 'Стрима', acc: 'Стрим' }, own: { gen: 'своего мира', acc: 'свой мир' },
}
export function worldOf(id: string) {
  const world = documents[id as WorldId] ?? templateDocuments[id as WorldId]
  if (!world) throw new Error('World has not been loaded')
  return { ...world, id: world.balanceKey, ...(WORLD_FORMS[world.balanceKey] ?? { gen: world.title, acc: world.title }) }
}

/** Выбор шаблона заполняет форму; сервер получает только описание нового мира. */
export function worldDefinitionOf(world: WorldId): WorldDefinition {
  const template = templateDocuments[world]
  if (!template) throw new Error('World template has not been loaded')
  const { id: _id, subscribers: _subscribers, lock: _lock, ...definition } = structuredClone(template)
  return { ...definition, rules: definition.rules.map(rule => ({ ...rule, locked: rule.text === 'Без тем 18+ и травли по внешности' })) }
}
