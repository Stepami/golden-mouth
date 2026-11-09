export type Persona = { goal?: string; skills?: string[]; roleId?: string; templateId?: string; avatar?: string };
const memory = new Map<string, Persona>();
const key = (playerId: string, worldId: string) => `reki:v7:${playerId}:persona:${worldId}`;
export function personaFor(playerId: string, worldId: string): Persona {
  const k = key(playerId, worldId); if (memory.has(k)) return memory.get(k)!;
  try { const value = JSON.parse(localStorage.getItem(k) ?? '{}'); if (value && typeof value === 'object') return value; } catch { /* optional local presentation */ }
  return {};
}
export function savePersona(playerId: string, worldId: string, value: Persona): boolean {
  const k = key(playerId, worldId); memory.set(k, value);
  try { localStorage.setItem(k, JSON.stringify(value)); return true; } catch { return false; }
}

export function clearPersonas(playerId:string){for(const k of memory.keys())if(k.startsWith(`reki:v7:${playerId}:`))memory.delete(k);}
