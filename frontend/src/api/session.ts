let generation = 0
export const sessionGeneration = (): number => generation
export const endSession = (): void => { generation++ }
export function assertSession(expected: number): void {
  if (expected !== generation) throw new DOMException('Account session has ended', 'AbortError')
}
