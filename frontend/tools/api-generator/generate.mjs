import { rm } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import { createClient } from '@hey-api/openapi-ts'
import config from './openapi-ts.config.mjs'

process.chdir(fileURLToPath(new URL('.', import.meta.url)))
// A failed run must not leave a previous client available to build/dev.
await rm(new URL('../../src/api/generated/', import.meta.url), { recursive: true, force: true })
const jobs = await createClient(await config)
// openapi-ts 0.99 returns [] for input errors; its CLI still exits successfully.
if (jobs.length !== 1) throw new Error('API client generation failed: no contract was generated')
