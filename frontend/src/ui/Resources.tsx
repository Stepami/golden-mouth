import { useCallback, useEffect, useId, useRef, useState, type KeyboardEvent } from 'react'
import { createPortal } from 'react-dom'
import { api, ApiError } from '../api/client'
import type { Player } from '../api/types'
import { acknowledgeCrystalShopOnboarding, useGameOnboarding } from '../app/GameOnboardingState'
import { useStore } from '../app/store'
import { Button, Sheet } from '.'

export function Resources({ player }: { player: Player }) {
  const { refresh } = useStore()
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
  return <div className="resources">
    <div className="energy-resource"><span className="energy-ring" style={{ background: `conic-gradient(var(--gift) ${Math.min(100, Math.max(0, player.postsLeft / (wallet?.cap ?? 10) * 100))}%, var(--line) 0)` }}><span aria-label={`Осталось ${player.postsLeft} энергии`}>⚡{player.postsLeft}</span></span>{seconds !== null && <span className="energy-timer" aria-label="До восстановления энергии">{Math.floor(seconds / 60)}:{String(seconds % 60).padStart(2, '0')}</span>}</div>
    {wallet && <CrystalShop key={player.id} playerId={player.id} wallet={wallet} />}
  </div>
}

const boosterCost = 6
type ShopAction = 'energy' | 'booster' | 'refresh'
type PurchaseRecovery = 'purchase-confirmed' | 'purchase-unknown'
type ShopNotice = { text: string; refresh?: PurchaseRecovery }

function CrystalShop({ playerId, wallet }: { playerId: string; wallet: NonNullable<Player['energyWallet']> }) {
  const { patch, refresh } = useStore()
  const learning = useGameOnboarding()?.crystalShopIntroPending === true
  const [open, setOpen] = useState(false)
  const [busy, setBusy] = useState<ShopAction | null>(null)
  const [notice, setNotice] = useState<ShopNotice | null>(null)
  const pending = useRef(false)
  const trigger = useRef<HTMLButtonElement>(null)
  const dialog = useRef<HTMLDivElement>(null)
  const titleId = useId()
  const quote = wallet.exchangeQuote

  useEffect(() => {
    if (!open) return
    const button = trigger.current
    dialog.current?.focus()
    return () => button?.focus()
  }, [open])

  const reconcileWallet = useCallback(async (recovery: PurchaseRecovery) => {
    if (api.playerId() !== playerId) return
    const latest = await refresh()
    if (api.playerId() !== playerId) return
    if (latest?.id !== playerId || !latest.energyWallet) {
      setNotice({
        text: recovery === 'purchase-confirmed'
          ? 'Бустер-пак куплен. Проверим баланс автоматически.'
          : 'Покупка пока не подтверждена. Проверим баланс автоматически.',
        refresh: recovery,
      })
      return
    }
    setNotice({
      text: recovery === 'purchase-confirmed'
        ? 'Бустер-пак куплен. Карты добавлены.'
        : 'Покупка не подтверждена. Баланс обновлён.',
    })
  }, [playerId, refresh])

  useEffect(() => {
    const recovery = notice?.refresh
    if (!open || busy || !recovery) return
    const timer = window.setTimeout(() => { void reconcileWallet(recovery) }, 5000)
    return () => window.clearTimeout(timer)
  }, [open, busy, notice, reconcileWallet])

  const close = () => {
    if (!pending.current) setOpen(false)
  }
  function handleKeys(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Escape') { event.stopPropagation(); close(); return }
    if (event.key !== 'Tab') return
    const buttons = [...event.currentTarget.querySelectorAll<HTMLButtonElement>('button:not(:disabled)')]
    const first = buttons[0]
    const last = buttons.at(-1)
    if (!first || (event.shiftKey && (document.activeElement === first || document.activeElement === event.currentTarget)) ||
      (!event.shiftKey && document.activeElement === last)) {
      event.preventDefault()
      const target = (event.shiftKey ? last : first) ?? event.currentTarget
      target.focus()
    }
  }
  function start(action: ShopAction) {
    if (pending.current || api.playerId() !== playerId) return false
    pending.current = true
    setBusy(action)
    return true
  }
  function finish() {
    pending.current = false
    setBusy(null)
  }
  async function exchange() {
    if (!quote.available || notice?.refresh || !start('energy')) return
    setNotice(null)
    try {
      const result = await api.exchangeEnergy()
      if (api.playerId() !== playerId) return
      if (result.wallet) patch({ energyWallet: result.wallet, postsLeft: result.wallet.amount })
      setNotice({ text: result.exchanged ? 'Энергия пополнена.' : 'Обмен сейчас недоступен.' })
    } catch { setNotice({ text: 'Не получилось обменять кристаллы. Проверь подключение и попробуй ещё раз.' }) }
    finally { finish() }
  }
  async function buyBooster() {
    if (wallet.crystals < boosterCost || notice?.refresh || !start('booster')) return
    setNotice(null)
    try {
      let recovery: PurchaseRecovery = 'purchase-unknown'
      try {
        const bought = await api.buyBoosterPack()
        if (api.playerId() !== playerId) return
        if (bought === false) {
          setNotice({ text: 'Для бустер-пака не хватает кристаллов.' })
          return
        }
        if (bought === true) recovery = 'purchase-confirmed'
      } catch (cause) {
        if (api.playerId() !== playerId) return
        if (cause instanceof ApiError && cause.status === 404) {
          setNotice({ text: 'Бустер-пак пока недоступен.' })
          return
        }
      }

      setBusy('refresh')
      setNotice({ text: 'Сверяем баланс…' })
      await reconcileWallet(recovery)
    } finally { finish() }
  }
  return <>
    <button ref={trigger} type="button" className="energy-badge crystal-button" data-onboarding-crystal-counter data-crystal-shop-trigger
      onClick={() => { if (!notice?.refresh) setNotice(null); setOpen(true) }} aria-label={`Кристаллы: ${wallet.crystals}. Открыть магазин`} aria-haspopup="dialog">
      <span className="onboarding-crystal-icon" aria-hidden="true">💎</span>
      <span className="onboarding-crystal-balance" aria-hidden="true">{wallet.crystals}</span>
    </button>
    {open && createPortal(<Sheet onClose={close}>
      <div className="crystal-shop" ref={dialog} data-crystal-shop role="dialog" aria-modal="true" aria-labelledby={titleId} tabIndex={-1} onKeyDown={handleKeys}>
        <div className="crystal-shop-head">
          <h2 id={titleId}>Магазин кристаллов</h2>
          <output className="energy-badge" aria-label="Баланс кристаллов">💎 {wallet.crystals}</output>
        </div>
        <section className="crystal-shop-item">
          <h3><span aria-hidden="true">⚡</span> Энергия</h3>
          <p>Пополняет энергию до {quote.targetEnergy}.</p>
          {!quote.available && <p className="small muted">{quote.unavailableReason === 'energy_full' ? 'Энергия уже полная.' : 'Не хватает кристаллов для обмена.'}</p>}
          <Button secondary disabled={!!busy || !!notice?.refresh || !quote.available} onClick={exchange}>{busy === 'energy' ? 'Обмениваем…' : `Обменять за ${quote.crystalCost} 💎`}</Button>
        </section>
        <section className="crystal-shop-item crystal-shop-booster">
          <h3><span aria-hidden="true">✦</span> Бустер-пак</h3>
          <ul className="crystal-shop-pack">
            <li><b>3 виральные карты ×3</b><span>Гарантируют залёт обычного поста и усиливают результат</span></li>
            <li><b>3 флоп-гарда +5</b><span>Превращают потерю подписчиков в прирост и добавляют 5 просмотров</span></li>
          </ul>
          {wallet.crystals < boosterCost && <p className="small muted">Не хватает кристаллов для покупки.</p>}
          <Button disabled={!!busy || !!notice?.refresh || wallet.crystals < boosterCost} onClick={buyBooster}>{busy === 'booster' ? 'Покупаем…' : `Купить за ${boosterCost} 💎`}</Button>
        </section>
        {notice && <p className="crystal-shop-status small" role="status">{notice.text}</p>}
        <button type="button" className="text-button crystal-shop-close" disabled={!!busy} onClick={() => {
          if (learning && !pending.current) acknowledgeCrystalShopOnboarding()
          close()
        }}>{learning ? 'Понятно' : 'Закрыть'}</button>
      </div>
    </Sheet>, document.body)}
  </>
}
