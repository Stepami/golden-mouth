import test from 'node:test'
import assert from 'node:assert/strict'
import { mkdtemp, mkdir, copyFile, symlink, writeFile, rm, access } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { spawnSync } from 'node:child_process'

const frontend = fileURLToPath(new URL('../', import.meta.url))
async function fixture(run) {
  const root = await mkdtemp(join(tmpdir(), 'reki-api-generation-'))
  const tool = join(root, 'frontend/tools/api-generator')
  const output = join(root, 'frontend/src/api/generated')
  const snapshot = join(root, 'frontend/openapi/reki-api.json')
  try {
    await mkdir(tool, { recursive: true })
    await mkdir(join(root, 'frontend/openapi'), { recursive: true })
    for (const name of ['generate.mjs', 'openapi-ts.config.mjs']) await copyFile(join(frontend, 'tools/api-generator', name), join(tool, name))
    await symlink(join(frontend, 'tools/api-generator/node_modules'), join(tool, 'node_modules'), 'dir')
    await run({ output, snapshot, generate: () => spawnSync(process.execPath, [join(tool, 'generate.mjs')], { encoding: 'utf8' }) })
  } finally { await rm(root, { recursive: true, force: true }) }
}

test('generation succeeds from the saved contract without a running API', async () => {
  await fixture(async ({ output, snapshot, generate }) => {
    await copyFile(join(frontend, 'openapi/reki-api.json'), snapshot)
    const result = generate()
    assert.equal(result.status, 0, result.stdout + result.stderr)
    for (const file of ['types.gen.ts', 'sdk.gen.ts', 'valibot.gen.ts', 'client/index.ts']) await access(join(output, file))
  })
})
for (const input of ['missing', 'malformed']) test(`${input} contract fails generation and removes stale client`, async () => {
  await fixture(async ({ output, snapshot, generate }) => {
    await mkdir(output, { recursive: true })
    await writeFile(join(output, 'stale.ts'), '// previous generation')
    if (input === 'malformed') await writeFile(snapshot, '{ invalid JSON')
    const result = generate()
    assert.notEqual(result.status, 0, result.stdout + result.stderr)
    await assert.rejects(access(join(output, 'stale.ts')), { code: 'ENOENT' })
  })
})
