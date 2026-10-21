import { safeParse } from 'valibot'
import { error, warn } from '../app/log'
import { runtimeConfig } from '../app/runtimeConfig'
import { telegramAuthHeaders } from './auth'
import { createClient } from './generated/client'
import { vPublicationFailure } from './generated/valibot.gen'
import type { PublicationFailure } from './types'
import type { PostsGeneratePublicationData, PostsReplyToPostData } from './generated/types.gen'
import { assertSession, sessionGeneration } from './session'

export class ApiError extends Error {
  constructor(public readonly status: number, method: string, path: string, public readonly failure?: PublicationFailure) {
    super(`${method} ${path.replace(/\/[\w-]{16,}/g, '/:id')} ${status}`)
    this.name = 'ApiError'
  }
}
const baseUrl = new URL(runtimeConfig.apiBaseUrl, window.location.origin).href
export const client = createClient({ baseUrl, auth: () => telegramAuthHeaders().Authorization ?? '', fetch: (input, init) => fetch(input, init) })
client.interceptors.response.use(async (response, request) => {
  if (response.ok) {
    // TypedResults.Ok(null) intentionally writes no body for this nullable endpoint.
    // Normalize that wire representation before the generated JSON decoder runs.
    if (request.method === 'GET' && new URL(request.url).pathname === `${new URL(baseUrl).pathname}/challenges/active`
      && !response.headers.get('Content-Type') && !(await response.clone().text())) {
      const headers = new Headers(response.headers)
      headers.set('Content-Type', 'application/json')
      headers.delete('Content-Length')
      return new Response('null', { status: response.status, headers })
    }
    return response
  }
  let failure: PublicationFailure | undefined
  try {
    const body: unknown = await response.clone().json()
    const parsed = safeParse(vPublicationFailure, typeof body === 'object' && body !== null && 'failure' in body ? body.failure : undefined)
    if (parsed.success) failure = parsed.output
  } catch { /* Non-JSON transport error. */ }
  const path = new URL(request.url).pathname.slice(new URL(baseUrl).pathname.replace(/\/$/, '').length)
  const cause = new ApiError(response.status, request.method, path, failure)
  if (response.status >= 500) error('api_error', cause, { status: response.status })
  else if (response.status !== 404 || path !== '/me') warn('api_error', { status: response.status, msg: cause.message })
  throw cause
})
export const options = { client, throwOnError: true } as const
export async function data<T>(request: Promise<{ data: T }>, authenticated = true): Promise<T> {
  const generation = sessionGeneration()
  const result = await request
  if (authenticated) assertSession(generation)
  return result.data
}

// Do not use the generated SSE reconnect loop for mutations. Retain the existing
// single request + durable status recovery, with URL/query/body checked by OpenAPI.
export async function stream(input: PostsGeneratePublicationData | PostsReplyToPostData, signal?: AbortSignal): Promise<Response> {
  const generation = sessionGeneration()
  const result = await client.post<ReadableStream<Uint8Array>, unknown, true>({
    ...input, signal, throwOnError: true, parseAs: 'stream',
    headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream', ...telegramAuthHeaders() },
  })
  assertSession(generation)
  return result.response
}
