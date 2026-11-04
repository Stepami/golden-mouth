import { initialState } from "../domain/game";
import type { GameState } from "../domain/types";
const KEY = "reki-frontend-demo-v1";
export function loadState(): GameState {
  try {
    const x = JSON.parse(localStorage.getItem(KEY) ?? "null");
    if (
      x?.version === 1 &&
      typeof x.saves === "object" &&
      x.saves &&
      Array.isArray(x.qualities) &&
      typeof x.energy === "number" &&
      Object.values(x.saves).every(
        (s: any) =>
          Array.isArray(s.posts) &&
          Array.isArray(s.skills) &&
          Array.isArray(s.activities) &&
          Array.isArray(s.completedQuests) &&
          typeof s.draft === "string",
      )
    )
      return x;
  } catch {}
  return structuredClone(initialState);
}
export function persistState(state: GameState): boolean {
  try {
    localStorage.setItem(KEY, JSON.stringify(state));
    return true;
  } catch {
    return false;
  }
}
