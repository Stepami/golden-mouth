import { error, warn } from '../app/log'
import { runtimeConfig } from '../app/runtimeConfig'
import { getAccessToken, refreshAccess } from './auth'
import { EventsClient, PlayerClient, PostsClient, SocialClient, TelemetryClient, WorldsClient } from './generated/reki-api'
import type { GenerateReplyRequest, SwaggerResponse } from './generated/reki-api'
import { assertSession, sessionGeneration } from './session'

export class ApiError extends Error {
  constructor(public readonly status: number, method: string, path: string, public readonly problemType?: string) {
    super(`${method} ${path.replace(/\/[\w-]{16,}/g, '/:id')} ${status}`)
    this.name = 'ApiError'
  }
}
const baseUrl = new URL(runtimeConfig.apiBaseUrl, window.location.origin).href
export function transport(telemetry = false) {
  return { async fetch(url: RequestInfo, init: RequestInit = {}): Promise<Response> {
    const headers = new Headers(init.headers)
    const path = new URL(String(url), baseUrl).pathname.slice(new URL(baseUrl).pathname.replace(/\/$/, '').length)
    const anonymous = path === '/sessions'
    const firstToken = getAccessToken()
    if (!anonymous && firstToken) headers.set('Authorization', `Bearer ${firstToken}`)
    let response = await fetch(url, { ...init, headers, ...(telemetry ? { keepalive: true } : {}) })
    if (!anonymous && !(path === '/posts' && init.method === 'POST') && response.status === 401 && (getAccessToken() !== firstToken || await refreshAccess())) {
      headers.set('Authorization', `Bearer ${getAccessToken()}`)
      response = await fetch(url, { ...init, headers, ...(telemetry ? { keepalive: true } : {}) })
    }
    if (response.ok) return response
    let problemType: string | undefined
    try {
      const body: unknown = await response.clone().json()
      if (typeof body === 'object' && body !== null && 'type' in body && typeof body.type === 'string') problemType = body.type
    } catch { /* Non-JSON transport error. */ }
    const cause = new ApiError(response.status, init.method ?? 'GET', path, problemType)
    if (!telemetry) {
      if (response.status >= 500) error('api_error', cause, { status: response.status })
      else warn('api_error', { status: response.status, msg: cause.message })
    }
    throw cause
  } }
}
const http = transport()
export const sdk = {
  events: new EventsClient(baseUrl, http), player: new PlayerClient(baseUrl, http), posts: new PostsClient(baseUrl, http),
  social: new SocialClient(baseUrl, http), telemetry: new TelemetryClient(baseUrl, http), worlds: new WorldsClient(baseUrl, http),
}
export async function data<T>(request: Promise<SwaggerResponse<T>>, authenticated = true): Promise<T> {
  const generation = sessionGeneration()
  const result = await request
  if (authenticated) assertSession(generation)
  return result.result
}
// Fetch generation buffers file responses into Blob. Keep SSE streaming through
// this small adapter; REST requests and wire DTOs use the generated client.
export async function stream(input: { url: '/posts/{postId}/replies/generation'; path: { postId: string }; query: { worldId: string }; body: GenerateReplyRequest }, signal?: AbortSignal): Promise<Response> {
  const generation = sessionGeneration()
  const path = input.url.replace('{postId}', encodeURIComponent(input.path.postId))
  const response = await http.fetch(`${baseUrl}${path}?worldId=${encodeURIComponent(input.query.worldId)}`, {
    method: 'POST', body: JSON.stringify(input.body), signal, headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream' },
  })
  assertSession(generation)
  return response
}
