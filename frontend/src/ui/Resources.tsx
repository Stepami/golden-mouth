import { useEffect, useState } from 'react'
import { api } from '../api/client'
import type { Player } from '../api/types'
import { useStore } from '../app/store'
import { Button, Sheet } from '.'

export function Resources({ player }: { player: Player }) {
  const { patch, refresh } = useStore()
  const [open, setOpen] = useState(false)
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState('')
  const [now, setNow] = useState(Date.now())
  const wallet = player.energyWallet
  const next = wallet?.nextEnergyAtUtc ? new Date(wallet.nextEnergyAtUtc).getTime() : null
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000)
    return () => window.clearInterval(timer)
  }, [])
  useEffect(() => {
    if (next === null) return
    const timer = window.setTimeout(() => { void refresh() }, Math.max(0, next - Date.now()) + 500)
    return () => window.clearTimeout(timer)
  }, [next, refresh])
  const seconds = next === null ? null : Math.max(0, Math.ceil((next - now) / 1000))
  const exchange = async () => {
    setBusy(true); setMessage('')
    try {
      const result = await api.exchangeEnergy()
      if (result.wallet) patch({ energyWallet: result.wallet, postsLeft: result.wallet.amount })
      setMessage(result.exchanged ? 'Энергия пополнена' : 'Обмен сейчас недоступен')
    } catch { setMessage('Не получилось обменять кристаллы. Попробуй ещё раз.') }
    finally { setBusy(false) }
  }
  return <>
    <div className="resources">
      <div className="energy-resource"><span className="energy-ring" style={{ background: `conic-gradient(var(--gift) ${Math.min(100, Math.max(0, player.postsLeft / (wallet?.cap ?? 10) * 100))}%, var(--line) 0)` }}><span aria-label={`Осталось ${player.postsLeft} энергии`}>⚡{player.postsLeft}</span></span>{seconds !== null && <span className="energy-timer" aria-label="До восстановления энергии">{Math.floor(seconds / 60)}:{String(seconds % 60).padStart(2, '0')}</span>}</div>
      {wallet && <button className="energy-badge crystal-button" onClick={() => { setMessage(''); setOpen(true) }} aria-label={`Кристаллы: ${wallet.crystals}. Обменять на энергию`}>💎 {wallet.crystals}</button>}
    </div>
    {open && wallet && <Sheet onClose={() => !busy && setOpen(false)}><h2>Обменять кристаллы на энергию</h2><p>За {wallet.exchangeQuote.crystalCost} кристалла можно пополнить энергию до {wallet.exchangeQuote.targetEnergy}.</p>{!wallet.exchangeQuote.available && <p className="small muted">{wallet.exchangeQuote.unavailableReason === 'energy_full' ? 'Энергия уже полная.' : 'Недостаточно кристаллов.'}</p>}{message && <p role="status">{message}</p>}<Button disabled={busy || !wallet.exchangeQuote.available} onClick={exchange}>{busy ? 'Обмениваем…' : 'Обменять'}</Button><Button secondary onClick={() => setOpen(false)} disabled={busy}>Закрыть</Button></Sheet>}
  </>
}
