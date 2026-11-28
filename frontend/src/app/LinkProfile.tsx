import { useEffect, useId, useRef, useState } from 'react'
import { AuthProviders } from './AuthProviders'
import { currentAuthProvider, getInitData, hasInitData, type AuthProvider } from '../api/auth'
import { identifyOtherProfile, mergeProfile } from '../api/linkProfile'
import { Button } from '../ui'
import type { Player } from '../api/types'
import type { PlayerResponse } from '../api/generated/reki-api'

export function LinkProfile({ player, refresh, onClose }: { player: Player; refresh: () => Promise<unknown>; onClose: () => void }) {
  const titleId = useId()
  const dialog = useRef<HTMLDialogElement>(null)
  useEffect(() => {
    const node = dialog.current
    node?.showModal()
    return () => node?.close()
  }, [])
  const [other, setOther] = useState<PlayerResponse | null>(null)
  const [provider, setProvider] = useState<AuthProvider | null>(null)
  const [merged, setMerged] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const pending = useRef(false)
  const mounted = useRef(true)
  useEffect(() => { mounted.current = true; return () => { mounted.current = false } }, [])
  const identify = async (provider: AuthProvider, proof: string, miniApp = false) => {
    if (pending.current) return
    pending.current = true; setBusy(true); setError('')
    try {
      const result = await identifyOtherProfile(provider, proof, miniApp)
      if (!mounted.current) return
      if (result.id === player.id) setError('Этот способ входа уже связан с текущим профилем.')
      else { setOther(result); setProvider(provider) }
    } catch { if (mounted.current) setError('Не получилось найти профиль. Проверь вход и попробуй ещё раз.') }
    finally { pending.current = false; if (mounted.current) setBusy(false) }
  }
  const confirm = async () => {
    if (!other || pending.current) return
    pending.current = true; setBusy(true); setError('')
    try { await mergeProfile(other.id) }
    catch {
      if (mounted.current) { setError('Не получилось привязать профиль. Попробуй ещё раз.'); setBusy(false) }
      pending.current = false
      return
    }
    if (!mounted.current) { pending.current = false; return }
    setOther(null); setMerged(true)
    try {
      const updated = await refresh()
      if (updated && mounted.current) onClose()
    } catch { /* The profiles are already merged; leave the refresh action available. */ }
    finally { pending.current = false; if (mounted.current) setBusy(false) }
  }
  return <dialog ref={dialog} className="link-profile-dialog" aria-labelledby={titleId}
    onCancel={event => { event.preventDefault(); if (!pending.current) onClose() }}
    onClick={event => {
      if (event.target !== event.currentTarget || pending.current) return
      const box = event.currentTarget.getBoundingClientRect()
      if (event.clientX < box.left || event.clientX > box.right || event.clientY < box.top || event.clientY > box.bottom) onClose()
    }}>
    <h2 id={titleId}>🔑 Привязать профиль</h2>
    {merged ? <>
      <p>Профиль привязан. Не получилось обновить данные игры — попробуй обновить ещё раз.</p>
      <Button disabled={busy} onClick={() => { setBusy(true); void refresh().then(updated => { if (updated) onClose() }).finally(() => setBusy(false)) }}>Обновить игру</Button>
    </> : other ? <>
      <p>Сохраним твою текущую игру. Профиль, выбранный при входе другим способом, и его игровые данные будут удалены. Его способы входа перейдут в твой текущий профиль. Прогресс, энергия, награды и покупки не складываются. Отменить слияние нельзя.</p>
      <p style={{ overflowWrap: 'anywhere' }}><b>Сохраняем:</b> {player.name}<br /><span className="small muted">ID: {player.id}</span></p>
      <p style={{ overflowWrap: 'anywhere' }}><b>Удаляем:</b> профиль {provider === 'yandex' ? 'Яндекс ID' : provider === 'vk' ? 'VK' : 'Telegram'} · энергия {other.energy}<br /><span className="small muted">ID: {other.id}</span></p>
      <Button disabled={busy} onClick={() => void confirm()}>{busy ? 'Привязываем…' : 'Привязать'}</Button>
    </> : <>
      <p>Выбери способ входа в профиль, который хочешь привязать.</p>
      <AuthProviders hiddenProvider={currentAuthProvider()} allowMiniAppLink={false} busy={busy}
        onMiniApp={hasInitData() ? () => void identify('telegram', getInitData(), true) : undefined}
        onTelegram={token => void identify('telegram', token)} onVk={token => void identify('vk', token)} onYandex={token => void identify('yandex', token)}
        onError={() => setError('Вход не завершён. Попробуй ещё раз.')} />
    </>}
    {error && <p role="alert" style={{ color: 'var(--warn)' }}>{error}</p>}
    <button className="text-button" disabled={busy} onClick={onClose}>{merged ? 'Закрыть' : 'Отмена'}</button>
  </dialog>
}
