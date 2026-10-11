import test from 'node:test'
import assert from 'node:assert/strict'
import { ActiveTime } from '../src/app/activeTime.ts'
import { ageBand } from '../src/app/ageBand.ts'

test('age bands preserve boundaries and reject under-14', () => {
  for (const [age, band] of [[13,undefined],[14,'14-17'],[17,'14-17'],[18,'18-24'],[24,'18-24'],[25,'25-34'],[34,'25-34'],[35,'35-44'],[44,'35-44'],[45,'45+'],[99,'45+']]) assert.equal(ageBand(age), band)
})
test('foreground chunks do not overlap and hidden time is excluded', () => {
  const time = new ActiveTime(0,true)
  assert.equal(time.take(15000),15000)
  assert.equal(time.take(20000),5000)
  time.visibility(20000,false)
  assert.equal(time.take(100000),0)
  time.visibility(100000,true)
  assert.equal(time.take(110000),10000)
})
test('idle cutoff and resumed interaction exclude idle gap', () => {
  const time = new ActiveTime(0,true)
  for (const t of [15000,30000,45000,60000]) assert.equal(time.take(t),15000)
  assert.equal(time.take(120000),0)
  time.interact(130000)
  assert.equal(time.take(140000),10000)
})
test('frequent interactions accumulate without emitting tiny duplicate chunks', () => {
  const time = new ActiveTime(0,true)
  for (let t=100;t<=10000;t+=100) time.interact(t)
  assert.equal(time.take(15000),15000)
  assert.equal(time.take(15000),0)
})
