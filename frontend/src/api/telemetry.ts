import { createClient } from './generated/client'
import { telemetrySubmitAnalytics, telemetrySubmitClientLogs } from './generated/sdk.gen'
import type { AnalyticsBatchRequest, ClientLogBatchRequest } from './generated/types.gen'
import { telegramAuthHeaders } from './auth'
import { runtimeConfig } from '../app/runtimeConfig'

// Separate from game error reporting: failed telemetry must not recursively log itself.
const client = createClient({ baseUrl: new URL(runtimeConfig.apiBaseUrl, window.location.origin).href,
  auth: () => telegramAuthHeaders().Authorization ?? '', fetch: (input, init) => fetch(input, init) })
export const sendAnalytics = (body: AnalyticsBatchRequest) => telemetrySubmitAnalytics({ client, body, keepalive: true, throwOnError: true })
export const sendLogs = (body: ClientLogBatchRequest) => telemetrySubmitClientLogs({ client, body, keepalive: true, throwOnError: true })
