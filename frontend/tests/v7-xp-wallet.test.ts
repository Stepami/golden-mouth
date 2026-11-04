import test from 'node:test';
import assert from 'node:assert/strict';
import { allocate, sampleWallet, upgradeCost, validWallet } from '../src/v7/xp/wallet.ts';
test('spending uses one shared wallet while cumulative XP stays unchanged', () => {
  const before = sampleWallet(), next = allocate(before, [1, 1, 1])!;
  assert.equal(next.total, 3600); assert.equal(next.available, 3000);
  assert.deepEqual(next.levels, [2, 2, 2]); assert.deepEqual(before, sampleWallet());
});
test('each successive level costs more and a zero allocation spends nothing', () => {
  const next = allocate(sampleWallet(), [3, 0, 0])!;
  assert.equal(next.available, 3600 - 200 - 208 - 216);
  assert.equal(upgradeCost(100), 992);
  assert.deepEqual(allocate(next, [0, 0, 0]), next);
});
test('reject insufficient balance, negative/fractional purchases and levels above cap', () => {
  assert.equal(allocate({ total: 100, available: 100, levels: [1, 1, 1] }, [1, 0, 0]), undefined);
  assert.equal(allocate(sampleWallet(), [-1, 0, 0]), undefined);
  assert.equal(allocate(sampleWallet(), [.5, 0, 0]), undefined);
  assert.equal(allocate({ ...sampleWallet(), levels: [100, 1, 1] }, [1, 0, 0]), undefined);
});
test('corrupt or impossible persisted balances cannot become spendable XP', () => {
  for (const value of [null, {}, { ...sampleWallet(), available: -1 }, { ...sampleWallet(), total: 0 }, { ...sampleWallet(), available: Infinity }, { ...sampleWallet(), levels: [0, 1, 1] }]) assert.equal(validWallet(value), false);
  assert.ok(validWallet(sampleWallet()));
});
