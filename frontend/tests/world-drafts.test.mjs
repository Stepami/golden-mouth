import test from 'node:test'
import assert from 'node:assert/strict'
import { register } from 'node:module'
register('./typescript-resolver.mjs', import.meta.url)
const { blankWorld, selectDraftOwner, setWorldDraft, savedWorldDraft, clearWorldDrafts } = await import('../src/app/worldDraft.ts')

function storage() {
  const data = {}
  Object.defineProperties(data, {
    getItem: { value: key => data[key] ?? null },
    setItem: { value: (key, value) => { data[key] = value } },
    removeItem: { value: key => { delete data[key] } },
  })
  return data
}
const draft = (world, name) => ({ definition: { ...blankWorld(), balanceKey: world, name }, address: 'nick', group: '18+' })

test('switching keeps independent drafts, isolates accounts and deletion clears archived settings', () => {
  globalThis.sessionStorage = storage()
  selectDraftOwner('account-a')
  setWorldDraft(draft('evening', 'First name'))
  setWorldDraft(draft('sorcerers', 'Second name'))
  assert.equal(savedWorldDraft('evening').definition.name, 'First name')
  selectDraftOwner('account-b')
  assert.equal(savedWorldDraft('evening'), null)
  setWorldDraft(draft('evening', 'Other account'))
  selectDraftOwner('account-a')
  assert.equal(savedWorldDraft('evening').definition.name, 'First name')
  clearWorldDrafts('account-a')
  assert.equal(savedWorldDraft('evening'), null)
  assert.equal(savedWorldDraft('sorcerers'), null)
  selectDraftOwner('account-b')
  assert.equal(savedWorldDraft('evening').definition.name, 'Other account')
})

test('unavailable storage keeps drafts in memory and clears only the completed current draft', () => {
  globalThis.sessionStorage = { getItem() { throw Error() }, setItem() { throw Error() }, removeItem() { throw Error() } }
  selectDraftOwner('memory-account')
  setWorldDraft(draft('evening', 'Stored in memory'))
  setWorldDraft(draft('sorcerers', 'Completed'))
  setWorldDraft(null)
  assert.equal(savedWorldDraft('sorcerers'), null)
  assert.equal(savedWorldDraft('evening').definition.name, 'Stored in memory')
  clearWorldDrafts('memory-account')
  assert.equal(savedWorldDraft('evening'), null)
})
