import assert from 'node:assert/strict'
import test from 'node:test'

import { createDevelopmentInitData } from '../src/api/auth.ts'

test('createDevelopmentInitData_fixedInputs_emitsDeterministicDevelopmentPayload', () => {
  // Arrange
  const userId = 8001
  const authDateSeconds = 1791201600

  // Act
  const first = createDevelopmentInitData(userId, authDateSeconds)
  const second = createDevelopmentInitData(userId, authDateSeconds)
  const parameters = new URLSearchParams(first)

  // Assert
  assert.equal(second, first)
  assert.deepEqual([...parameters.keys()], ['user', 'auth_date', 'hash'])
  assert.deepEqual(JSON.parse(parameters.get('user') ?? ''), {
    id: userId,
    first_name: 'Development player',
  })
  assert.equal(parameters.get('auth_date'), authDateSeconds.toString())
  assert.equal(parameters.get('hash'), 'development')
})
