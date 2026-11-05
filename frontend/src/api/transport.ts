import { error, warn } from '../app/log'
import { runtimeConfig } from '../app/runtimeConfig'
import { getAccessToken, refreshAccess } from './auth'
import { EventsClient, PlayerClient, PostsClient, SocialClient, TelemetryClient, WorldsClient } from './generated/reki-api'
import type { GenerateReplyRequest, PublicationFailure, StartPublicationRequest, SwaggerResponse } from './generated/reki-api'
import { assertSession, sessionGeneration } from './session'

export class ApiError extends Error {
  constructor(public readonly status: number, method: string, path: string, public readonly failure?: PublicationFailure) {
    super(`${method} ${path.replace(/\/[\w-]{16,}/g, '/:id')} ${status}`)
    this.name = 'ApiError'
  }
}
export function publicationFailure(value: unknown): PublicationFailure | undefined {
  if (typeof value !== 'object' || value === null || !('code' in value) || !('category' in value) || !('policyVersion' in value)) return undefined
  return typeof value.code === 'string' && typeof value.category === 'string' && typeof value.policyVersion === 'string'
    ? { code: value.code, category: value.category, policyVersion: value.policyVersion } : undefined
}
const baseUrl = new URL(runtimeConfig.apiBaseUrl, window.location.origin).href
export function transport(telemetry = false, retryUnauthorized = true) {
  return { async fetch(url: RequestInfo, init: RequestInit = {}): Promise<Response> {
    const headers = new Headers(init.headers)
    const path = new URL(String(url), baseUrl).pathname.slice(new URL(baseUrl).pathname.replace(/\/$/, '').length)
    const anonymous = path === '/sessions'
    const firstToken = getAccessToken()
    if (!anonymous && firstToken) headers.set('Authorization', `Bearer ${firstToken}`)
    let response = await fetch(url, { ...init, headers, cache:path==='/me'||path.startsWith('/me/energy')?'no-store':init.cache, ...(telemetry ? { keepalive: true } : {}) })
    if (retryUnauthorized && !anonymous && response.status === 401 && (getAccessToken() !== firstToken || await refreshAccess())) {
      headers.set('Authorization', `Bearer ${getAccessToken()}`)
      response = await fetch(url, { ...init, headers, cache:path==='/me'||path.startsWith('/me/energy')?'no-store':init.cache, ...(telemetry ? { keepalive: true } : {}) })
    }
    if (response.ok) return response
    let failure: PublicationFailure | undefined
    try {
      const body: unknown = await response.clone().json()
      if (typeof body === 'object' && body !== null && 'failure' in body) failure = publicationFailure(body.failure)
    } catch { /* Non-JSON transport error. */ }
    const cause = new ApiError(response.status, init.method ?? 'GET', path, failure)
    if (!telemetry) {
      if (response.status >= 500) error('api_error', cause, { status: response.status })
      else if (response.status !== 404 || path !== '/me') warn('api_error', { status: response.status, msg: cause.message })
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
export async function stream(input: { url: '/posts/publications'; query: { worldId: string }; body: StartPublicationRequest }
  | { url: '/posts/{postId}/replies/generation'; path: { postId: string }; query: { worldId: string }; body: GenerateReplyRequest }, signal?: AbortSignal): Promise<Response> {
  const generation = sessionGeneration()
  const path = 'path' in input ? input.url.replace('{postId}', encodeURIComponent(input.path.postId)) : input.url
  const response = await http.fetch(`${baseUrl}${path}?worldId=${encodeURIComponent(input.query.worldId)}`, {
    method: 'POST', body: JSON.stringify(input.body), signal, headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream' },
  })
  assertSession(generation)
  return response
}

// Exchanges must never be replayed implicitly, including authentication recovery.
export const walletExchangeClient = new PlayerClient(baseUrl, transport(false, false))
