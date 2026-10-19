/** Display only: keep the original handle intact for API payloads and avatar seeds. */
export function displayHandle(value: string): string {
  const handle = value.trim().replace(/^@+/, '').trim()
  return handle ? `@${handle}` : ''
}
