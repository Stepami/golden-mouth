import { TelemetryClient } from './generated/reki-api'
import type { AnalyticsBatchRequest, ClientLogBatchRequest } from './generated/reki-api'
import { runtimeConfig } from '../app/runtimeConfig'
import { transport } from './transport'

const client = new TelemetryClient(new URL(runtimeConfig.apiBaseUrl, window.location.origin).href, transport(true))
export const sendAnalytics = (body: AnalyticsBatchRequest) => client.submitAnalytics(body)
export const sendLogs = (body: ClientLogBatchRequest) => client.submitClientLogs(body)

let sessionRequest: Promise<string> | undefined
export function getSessionId(): Promise<string> {
  return sessionRequest ??= client.startSession().then(response => response.result).catch(error => {
    sessionRequest = undefined
    throw error
  })
}
