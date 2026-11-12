import test from 'node:test'
import assert from 'node:assert/strict'
import vm from 'node:vm'
import {readFile} from 'node:fs/promises'
import {register} from 'node:module'
register('./typescript-resolver.mjs', import.meta.url)
const {applicationServerKey, subscribePush, pushSupport} = await import('../src/app/push/browser.ts')
const key = Buffer.from([4, ...Array(64).fill(1)]).toString('base64url')

test('validates VAPID and reuses subscription without creating duplicates', async () => {
  assert.equal(applicationServerKey(key).length, 65)
  assert.throws(() => applicationServerKey('broken'))
  const existing = {options: {applicationServerKey: applicationServerKey(key).buffer}}
  let calls = 0
  const registration = {pushManager: {getSubscription: async () => existing, subscribe: async () => { calls++ }}}
  assert.equal(await subscribePush(registration, key), existing)
  assert.equal(calls, 0)
  existing.options.applicationServerKey = new Uint8Array(65).buffer
  await assert.rejects(subscribePush(registration, key), /key changed/)
  registration.pushManager.getSubscription = async () => null
  registration.pushManager.subscribe = async options => { calls++; assert.equal(options.userVisibleOnly, true); return existing }
  assert.equal(await subscribePush(registration, key), existing)
  assert.equal(calls, 1)
})

test('Telegram bypasses push; iOS requires standalone; missing APIs are unsupported', () => {
  assert.equal(pushSupport(true), 'telegram')
  Object.defineProperty(globalThis, 'navigator', {configurable: true, value: {userAgent: 'iPhone', platform: 'iPhone', maxTouchPoints: 1}})
  globalThis.matchMedia = () => ({matches: false})
  assert.equal(pushSupport(false), 'install')
  globalThis.matchMedia = () => ({matches: true})
  globalThis.window = {isSecureContext: true}
  assert.equal(pushSupport(false), 'unsupported')
  navigator.serviceWorker = {}; window.PushManager = {}; window.Notification = {}
  assert.equal(pushSupport(false), 'supported')
})

async function worker() {
  const handlers = {}, shown = [], opened = []
  const self = {location: {origin: 'https://game.example'}, addEventListener: (name, handler) => { handlers[name] = handler },
    registration: {showNotification: async (...args) => shown.push(args)},
    clients: {matchAll: async () => [], openWindow: async url => opened.push(url)}}
  vm.runInNewContext(await readFile(new URL('../public/push-sw.js', import.meta.url), 'utf8'), {self, URL})
  const dispatch = async (name, event) => { let pending; handlers[name]({...event, waitUntil: promise => { pending = promise }}); await pending }
  return {self, shown, opened, dispatch, handlers}
}

test('push worker rejects external/invalid URLs and malformed JSON, with no fetch cache', async () => {
  const w = await worker()
  assert.equal(w.handlers.fetch, undefined)
  for (const url of ['https://evil.example/', '//evil.example/', 'javascript:alert(1)', 'https://name:secret@game.example/']) {
    await w.dispatch('push', {data: {json: () => ({title: 'ignored', body: 'Hello', url})}})
    assert.equal(w.shown.at(-1)[0], 'Реки')
    assert.equal(w.shown.at(-1)[1].data.url, 'https://game.example/')
    await w.dispatch('notificationclick', {notification: {close() {}, data: {url}}})
    assert.equal(w.opened.at(-1), 'https://game.example/')
  }
  await w.dispatch('push', {data: {json() { throw Error('invalid JSON') }}})
  assert.equal(w.shown.at(-1)[1].body, 'Загляни в свой мир.')
})

test('notification focuses an existing game without navigating away from a draft', async () => {
  const w = await worker()
  let focused = 0
  w.self.clients.matchAll = async () => [{url: 'https://game.example/?editor', focus: async () => {focused++}}]
  await w.dispatch('notificationclick', {notification: {close() {}, data: {url: '/'}}})
  assert.equal(focused, 1)
  assert.equal(w.opened.length, 0)
})
