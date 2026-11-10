export type XPWallet = { total: number; available: number; levels: [number, number, number] };
// Hundredths of XP; matches the proposed story mechanics: 2 + .08 * (level - 1).
export const upgradeCost = (level: number) => 200 + 8 * (level - 1);
export const sampleWallet = (): XPWallet => ({ total: 3600, available: 3600, levels: [1, 1, 1] });
export function validWallet(value: unknown): value is XPWallet {
  if (!value || typeof value !== 'object') return false;
  const w = value as XPWallet;
  return Number.isSafeInteger(w.total) && Number.isSafeInteger(w.available) && w.total >= w.available && w.available >= 0
    && Array.isArray(w.levels) && w.levels.length === 3 && w.levels.every(n => Number.isInteger(n) && n >= 1 && n <= 100);
}
export function allocate(wallet: XPWallet, points: [number, number, number]): XPWallet | undefined {
  if (!validWallet(wallet) || points.some(n => !Number.isSafeInteger(n) || n < 0) || points.length !== 3) return;
  let cost = 0;
  const levels = wallet.levels.map((level, i) => {
    if (level + points[i] > 100) return NaN;
    for (let step = 0; step < points[i]; step++) cost += upgradeCost(level + step);
    return level + points[i];
  }) as XPWallet['levels'];
  if (levels.some(n => !Number.isFinite(n)) || cost > wallet.available) return;
  return { total: wallet.total, available: wallet.available - cost, levels };
}
export const formatXP = (cents: number) => new Intl.NumberFormat('ru', { maximumFractionDigits: 2 }).format(cents / 100);
