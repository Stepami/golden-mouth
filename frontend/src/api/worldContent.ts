import { worldArt } from '../app/worldArt'
import type { Drama, Star, Want, WorldId, WorldDocument, WorldDefinition, World } from './types'

export interface WorldContent {
  where: string
  roles: string[]
  name: string
  handle: string
  avatarStyle: 'adventurer' | 'lorelei' | 'notionists' | 'pixelArt' | 'thumbs' | 'shapes'
  avatarLabel: string
  stars: (Star & { avatarSeed: string })[]
  voices: string[]
  drama: Drama
  description: string
  rules: string[]
  firstContext: string
  firstPrompt: string
  ideas: string[]
}

const documents: Partial<Record<WorldId, WorldDocument>> = {}
export const worlds: World[] = []
const templateDocuments: Partial<Record<WorldId, WorldDocument>> = {}

export function installWorldTemplates(templates: WorldDocument[]): void {
  for (const key of Object.keys(templateDocuments)) delete templateDocuments[key as WorldId]
  for (const template of templates) templateDocuments[template.id] = structuredClone(template)
  worlds.splice(0, worlds.length, ...templates.map(world => ({
    id: world.id, title: world.title, emoji: world.emoji, description: world.description,
    difficulty: world.difficulty, summary: world.summary,

  })))
}

const serverContent: Partial<Record<WorldId, WorldContent>> = {}
const traitNames = ['Дерзкость', 'Поддержка', 'Загадка', 'Воображение', 'Любопытство', 'Энергия', 'Ирония и расчёт', 'Сухой юмор', 'Самоирония', 'Азарт', 'Внимание']
export function installWorldContent(worlds: WorldDocument[]): void {
  for (const world of worlds) {
    const template = optionalWorldTemplate(world)
    if (template && worldArt[template.id]) worldArt[world.id] = worldArt[template.id]
    documents[world.id] = world
    serverContent[world.id] = toWorldContent(world)
  }
}

export function clearWorldContent(): void {
  for (const key of Object.keys(documents)) delete documents[key as WorldId]
  for (const key of Object.keys(serverContent)) delete serverContent[key as WorldId]
}

function toWorldContent(world: WorldDocument): WorldContent {
  return {
    where: world.where, roles: world.roles, name: world.name, handle: world.avatar,
    avatarStyle: world.avatarStyle as WorldContent['avatarStyle'], avatarLabel: 'аватар канала',
    stars: world.stars.map(star => ({ ...star, trait: traitNames[star.trait] ?? '' })),
    voices: world.voices, drama: world.drama, description: world.description,
    rules: world.rules.map(rule => rule.text), firstContext: world.firstContext, firstPrompt: world.firstPrompt,
    ideas: world.postIdeas,
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
  'b3a1a522-0263-4796-a197-57a8731fc291': { gen: 'Вечерело', acc: 'Вечерело' }, 'aec9494b-4118-4525-8458-e85d759c8dd9': { gen: 'Школы чародеев', acc: 'Школу чародеев' }, '8c02a7fb-3b55-4eb0-b957-1b4869ed9834': { gen: 'Игры домов', acc: 'Игру домов' },
  'edfccf53-65c6-4eaf-8321-2391bc1758bd': { gen: 'Сцены', acc: 'Сцену' }, 'b018145e-c887-4d00-9f18-5c877d0af8be': { gen: 'Академии', acc: 'Академию' }, '5720946a-ea7c-4a10-9c50-51ce3deefc53': { gen: 'Айти-тусовки', acc: 'Айти-тусовку' },
  '0cb3f809-6dfd-4ef1-af52-daa12a5b8804': { gen: 'Стрима', acc: 'Стрим' },
}
export function worldOf(id: string) {
  const world = documents[id as WorldId] ?? templateDocuments[id as WorldId]
  if (!world) throw new Error('World has not been loaded')
  return { ...world, id: world.id, ...(WORLD_FORMS[optionalWorldTemplate(world)?.id ?? world.id] ?? { gen: world.title, acc: world.title }) }
}

/** Выбор шаблона заполняет форму; сервер получает описание и ссылку на исходный шаблон. */
export function worldDefinitionOf(world: WorldId): WorldDefinition {
  const template = templateDocuments[world]
  if (!template) throw new Error('World template has not been loaded')
  const { id, subscribers: _subscribers, lock: _lock, ...definition } = structuredClone(template)
  return { ...definition, templateId: id, rules: definition.rules.map(rule => ({ ...rule, locked: rule.text === 'Без тем 18+ и травли по внешности' })) }
}

/** Missing templates must not make custom worlds inherit preset copy. */
export function optionalWorldTemplate(world: WorldDefinition | WorldDocument): WorldDocument | undefined {
  return Object.values(templateDocuments).find(template => template && template.title === world.title
    && template.description === world.description.split('\nПерсона игрока: ')[0]
    && template.where === world.where && template.firstContext === world.firstContext && template.firstPrompt === world.firstPrompt)
}
