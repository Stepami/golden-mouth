/** Display only: keep the original handle intact for API payloads and avatar seeds. */
export function displayHandle(value: string): string {
  const handle = value.trim().replace(/^@+/, '').trim()
  return handle ? `@${handle}` : ''
}

/** World stars without a nickname may have a generated UUID for their avatar. */
export function heroHandle(value?: string | null): string {
  const handle = displayHandle(value ?? '')
  return /^@(?:star_)?[\da-f]{8}(?:-?[\da-f]{4}){3}-?[\da-f]{12}$/i.test(handle) ? '' : handle
}
