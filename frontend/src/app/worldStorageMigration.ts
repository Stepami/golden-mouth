// One-way compatibility for local data written before backend migration 2.5.0.
const legacyTemplates: Record<string, string> = {
  stage: 'edfccf53-65c6-4eaf-8321-2391bc1758bd',
  academy: 'b018145e-c887-4d00-9f18-5c877d0af8be',
  it: '5720946a-ea7c-4a10-9c50-51ce3deefc53',
  stream: '0cb3f809-6dfd-4ef1-af52-daa12a5b8804',
  evening: 'b3a1a522-0263-4796-a197-57a8731fc291',
  sorcerers: 'aec9494b-4118-4525-8458-e85d759c8dd9',
  houses: '8c02a7fb-3b55-4eb0-b957-1b4869ed9834',
}

export function migrateWorldDraft(value: unknown): unknown {
  if (!value || typeof value !== 'object' || !('definition' in value)) return value
  const draft = value as { definition: Record<string, unknown> }
  const definition = draft.definition
  if (typeof definition.balanceKey === 'string') {
    definition.templateId = legacyTemplates[definition.balanceKey] ?? null
    delete definition.balanceKey
  }
  if (typeof definition.difficulty === 'number') definition.difficulty = ['easy', 'normal', 'hard'][definition.difficulty - 1] ?? 'normal'
  return value
}

export function legacyDraftSlot(value: string): string {
  if (value === 'custom') return 'own'
  return Object.keys(legacyTemplates).find(key => legacyTemplates[key] === value) ?? value
}

async function migratedId(value: string): Promise<string> {
  const plain = value.replaceAll('-', '').toLowerCase()
  if (/^[0-9a-f]{32}$/.test(plain)) return `${plain.slice(0, 8)}-${plain.slice(8, 12)}-${plain.slice(12, 16)}-${plain.slice(16, 20)}-${plain.slice(20)}`
  // Matches the migration's deterministic Guid(byte[]) conversion for early instances.
  const hash = new Uint8Array(await crypto.subtle.digest('SHA-256', new TextEncoder().encode(`reki:world:2.5.0:${value}`)))
  const bytes = [3, 2, 1, 0, 5, 4, 7, 6, 8, 9, 10, 11, 12, 13, 14, 15].map(i => hash[i].toString(16).padStart(2, '0')).join('')
  return `${bytes.slice(0, 8)}-${bytes.slice(8, 12)}-${bytes.slice(12, 16)}-${bytes.slice(16, 20)}-${bytes.slice(20)}`
}

export async function migrateWorldStorage(playerId: string, knownWorldIds: string[]): Promise<void> {
  try {
    const selectedKey = `reki:selected-world:${playerId}`
    const selected = localStorage.getItem(selectedKey)
    if (selected) {
      const id = await migratedId(selected)
      if (knownWorldIds.includes(id)) localStorage.setItem(selectedKey, id)
    }
    for (const key of Object.keys(localStorage)) {
      const prefix = `reki:post-draft:v1:${playerId}:`
      if (!key.startsWith(prefix)) continue
      const id = await migratedId(key.slice(prefix.length))
      if (!knownWorldIds.includes(id)) continue
      const next = prefix + id
      if (next !== key && localStorage.getItem(next) === null) {
        const draft = localStorage.getItem(key)
        if (draft !== null) localStorage.setItem(next, draft)
      }
    }
  } catch { /* Preserve existing local data when storage or Web Crypto is unavailable. */ }
}
