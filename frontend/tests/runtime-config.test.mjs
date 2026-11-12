import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';
import { stripTypeScriptTypes } from 'node:module';

const root = new URL('../', import.meta.url);
const source = readFileSync(new URL('src/app/runtimeConfig.ts', root), 'utf8');
const compiled = stripTypeScriptTypes(source);
let sequence = 0;

async function loadConfig(config) {
  globalThis.window = { __REKI_CONFIG__: config };
  const url = `data:text/javascript;base64,${Buffer.from(compiled).toString('base64')}#${sequence++}`;
  return (await import(url)).runtimeConfig;
}

test('runtime config exposes the backend VAPID public key', async () => {
  const window = { location: { origin: 'https://example.invalid' } };
  vm.runInNewContext(readFileSync(new URL('public/runtime-config/config.js', root), 'utf8'), { window });
  const backend = JSON.parse(readFileSync(new URL('../backend/src/Reki.Api/appsettings.json', root), 'utf8'));
  assert.equal((await loadConfig(window.__REKI_CONFIG__)).webPushPublicKey, backend.VapidKeyOptions.PublicKey);
  assert.equal(Buffer.from(backend.VapidKeyOptions.PublicKey, 'base64url').length, 65);
});

test('runtime config falls back when the VAPID key is absent or invalid', async () => {
  assert.equal((await loadConfig({ apiBaseUrl: '/api/v1' })).webPushPublicKey, '');
  assert.equal((await loadConfig({ apiBaseUrl: '/api/v1', webPushPublicKey: 123 })).webPushPublicKey, '');
});
