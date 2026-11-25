import { event, tap } from './analytics'
import { createPortal } from 'react-dom'
import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import { contentOf } from '../api/worldContent'
import { api } from '../api/client'
import { useNav } from './nav'
import { useStore } from './store'
import { pendingLandingWorld } from './landing/experiment'
import { getWorldDraft } from './worldDraft'
import { acknowledgeCrystalShopOnboarding, acknowledgeGameOnboarding, gameOnboardingState, setOnboardingBonusClaim, beginGameOnboardingPreview, onboardingPostSaved, onboardingReplySaved, updateGameOnboarding, useGameOnboarding, isGameOnboardingPreview } from './GameOnboardingState'

export { isGameOnboardingPreview } from './GameOnboardingState'

const rewardCrystals = 2

function GameOnboardingCard({ target, title, text, step, done, lockedSection, onResume }: { target: string; title: string; text: string; step: number; done: boolean; lockedSection: boolean; onResume?: () => void }) {
  const card = useRef<HTMLElement>(null)
  const { player, refresh } = useStore()
  const onboarding = useGameOnboarding()
  const [claimMessage, setClaimMessage] = useState('')
  const [host, setHost] = useState<HTMLElement | null>(null)
  const [editing, setEditing] = useState(false)
  const [targetReady, setTargetReady] = useState(false)
  const [collecting, setCollecting] = useState(false)
  const flight = useRef<{ cancelled: boolean; controller: AbortController; nodes: HTMLElement[]; animations: Animation[]; counter?: HTMLElement } | null>(null)
  useEffect(() => () => {
    const active = flight.current
    if (!active) return
    active.cancelled = true
    active.controller.abort()
    active.animations.forEach(animation => animation.cancel())
    active.nodes.forEach(node => node.remove())
    active.counter?.removeAttribute('data-onboarding-flight-balance')
  }, [])
  const collectReward = async (button: HTMLButtonElement) => {
    if (flight.current) return
    const preview = isGameOnboardingPreview(), playerId = player?.id, state = gameOnboardingState()
    if (state?.phase !== 'done') return
    const counter = matchMedia('(prefers-reduced-motion: reduce)').matches
      ? undefined
      : document.querySelector<HTMLElement>('[data-onboarding-crystal-counter]') ?? undefined
    const active = { cancelled: false, controller: new AbortController(), nodes: [] as HTMLElement[], animations: [] as Animation[], counter }
    flight.current = active; setCollecting(true); setClaimMessage('')
    let confirmed = state.bonusClaim === 'confirmed'
    let canAcknowledge = preview
    let displayBalance = player?.energyWallet?.crystals
    const startingBalance = displayBalance
    try {
      if (!preview) {
        if (!playerId || !state.postId) { setClaimMessage('Не найден учебный пост. Вернись в канал.'); return }
        if (!state.bonusClaim) {
          tap('onboarding_claim')
          if (!setOnboardingBonusClaim('pending', playerId)) {
            setOnboardingBonusClaim(undefined, playerId)
            setClaimMessage('Не удалось сохранить состояние на устройстве. Запрос награды не отправлен.'); return
          }
          const granted = await api.claimOnboardingBonus(state.postId, AbortSignal.any([active.controller.signal, AbortSignal.timeout(15000)]))
          if (typeof granted === 'boolean') event('onboarding_step', { step: 'reward', ok: granted })
          if (granted === false) {
            setOnboardingBonusClaim(undefined, playerId)
            if (!active.cancelled) setClaimMessage('Награда пока недоступна. Обучение сохранено.')
            return
          }
          if (granted !== true) throw new Error('Unexpected bonus response')
          confirmed = true
          setOnboardingBonusClaim('confirmed', playerId)
          if (counter && !active.cancelled && startingBalance !== undefined) {
            counter.dataset.onboardingFlightBalance = String(startingBalance)
          }
        }
        if (active.cancelled) return
        const latest = await refresh()
        if (active.cancelled) return
        if (!latest?.energyWallet || latest.id !== playerId) {
          setClaimMessage(confirmed ? 'Сервер подтвердил награду, но баланс не загрузился. Обнови баланс — повторный запрос награды не отправляем.' : 'Не получилось обновить баланс. Повторный запрос награды не отправлен. Проверь позже.')
          return
        }
        if (!confirmed) { setClaimMessage('Баланс обновлён. Начисление не подтверждено; повторный запрос награды не отправлен.'); return }
        canAcknowledge = true
        if (state.bonusClaim || startingBalance === undefined || latest.energyWallet.crystals - startingBalance !== rewardCrystals) return
      }
      if (!counter?.isConnected) return
      const start = button.getBoundingClientRect()
      const end = (counter.querySelector('.onboarding-crystal-icon') ?? counter).getBoundingClientRect()
      if (!end.width || !end.height) return
      const flying = Array.from({ length: rewardCrystals }, (_, index) => {
        const gem = document.createElement('span')
        gem.className = 'game-onboarding-crystal-flight'; gem.dataset.onboardingFlight = String(index)
        gem.setAttribute('aria-hidden', 'true')
        const offset = index === 0 ? -13 : 13
        const fromX = start.left + start.width / 2 + offset, fromY = start.top + start.height / 2
        gem.style.left = `${fromX - 16}px`
        gem.style.top = `${fromY - 16}px`
        document.body.append(gem); active.nodes.push(gem)
        const x = end.left + end.width / 2 - fromX
        const y = end.top + end.height / 2 - fromY
        const animation = gem.animate([
          { transform: 'translate(0, 5px) scale(.7)', opacity: 0 },
          { transform: `translate(${offset}px, -30px) rotate(${offset}deg) scale(1.15)`, opacity: 1, offset: .24 },
          { transform: `translate(${x * .5 + offset}px, ${y * .54 - 12}px) rotate(14deg) scale(.85)`, opacity: 1, offset: .65 },
          { transform: `translate(${x}px, ${y}px) rotate(35deg) scale(.24)`, opacity: .3 },
        ], { duration: 610, delay: 85 + index * 130, easing: 'cubic-bezier(.24,.58,.23,1)', fill: 'both' })
        active.animations.push(animation)
        return animation.finished.then(() => {
          gem.remove()
          if (active.cancelled) return
          if (!preview && displayBalance !== undefined) {
            counter.dataset.onboardingFlightBalance = String(++displayBalance)
          }
          active.animations.push(counter.animate([
            { transform: 'scale(1)' },
            { transform: 'scale(1.12)', boxShadow: '0 0 0 4px #a6e5ff35', offset: .32 },
            { transform: 'scale(1)', boxShadow: '0 0 0 9px #a6e5ff00' },
          ], { duration: 260, easing: 'ease-out' }))
        })
      })
      await Promise.all(flying)
      if (!preview) setClaimMessage(`Начислено +${rewardCrystals} кристалла`)
      const delta = document.createElement('span')
      delta.className = 'game-onboarding-crystal-delta'
      delta.textContent = `+${rewardCrystals}`
      delta.setAttribute('aria-hidden', 'true')
      counter.append(delta); active.nodes.push(delta)
      const lift = delta.animate([
        { opacity: 0, transform: 'translateY(-3px)' },
        { opacity: 1, transform: 'translateY(0)', offset: .25 },
        { opacity: 1, transform: 'translateY(0)', offset: .65 },
        { opacity: 0, transform: 'translateY(4px)' },
      ], { duration: 420 })
      active.animations.push(lift)
      await lift.finished
    } catch {
      if (!preview && !canAcknowledge) {
        if (!confirmed && playerId) {
          event('onboarding_step', { step: 'reward', ok: false, kind: 'unconfirmed' })
          setOnboardingBonusClaim('uncertain', playerId)
        }
        if (!active.cancelled) setClaimMessage(confirmed ? 'Награда подтверждена. Обнови баланс, чтобы завершить.' : 'Не удалось подтвердить начисление. Проверь баланс позже; повторный запрос награды не отправляем.')
      }
    }
    finally {
      active.nodes.forEach(node => node.remove())
      active.counter?.removeAttribute('data-onboarding-flight-balance')
      if (!active.cancelled) {
        flight.current = null; setCollecting(false)
        if (canAcknowledge && preview === isGameOnboardingPreview() && playerId === api.playerId()) acknowledgeGameOnboarding()
      }
    }
  }
  useLayoutEffect(() => {
    let highlighted: HTMLElement | null = null
    const update = () => {
      const element = target ? document.querySelector<HTMLElement>(target) : null
      const button = element?.matches('button,.world-roster > span') ? element : element?.querySelector<HTMLElement>('button') ?? null
      if (highlighted !== button) { highlighted?.classList.remove('game-onboarding-target'); button?.classList.add('game-onboarding-target'); highlighted = button }
      setTargetReady(!!button)
      setHost(document.querySelector<HTMLElement>('.bottom-cta') ?? document.querySelector<HTMLElement>('.post-result-actions'))
    }
    const focus = () => setEditing(document.activeElement?.matches('input,textarea') ?? false)
    update(); focus()
    const observer = new MutationObserver(update)
    observer.observe(document.querySelector('.app') ?? document.body, { childList: true, subtree: true, attributes: true, attributeFilter: ['data-game-onboarding-post', 'data-game-onboarding-world', 'aria-pressed'] })
    document.addEventListener('focusin', focus); document.addEventListener('focusout', focus)
    return () => { observer.disconnect(); highlighted?.classList.remove('game-onboarding-target'); document.removeEventListener('focusin', focus); document.removeEventListener('focusout', focus) }
  }, [target])
  useLayoutEffect(() => {
    const element = card.current, app = document.querySelector<HTMLElement>('.app')
    if (!element || !app) return
    app.classList.add('game-onboarding-active')
    const footer = element.closest<HTMLElement>('.bottom-cta') ?? element
    const measure = () => app.style.setProperty('--game-onboarding-bottom-space', `${Math.max(0, innerHeight - footer.getBoundingClientRect().top) + 16}px`)
    measure()
    const observer = new ResizeObserver(measure)
    observer.observe(footer)
    window.addEventListener('resize', measure)
    return () => { observer.disconnect(); window.removeEventListener('resize', measure); app.classList.remove('game-onboarding-active'); app.style.removeProperty('--game-onboarding-bottom-space') }
  }, [host, editing])
  const waitingForReaction = !done && step === 3 && !!target && !targetReady
  const dock = <aside ref={card} className={`game-onboarding-progress${done ? ' game-onboarding-finish' : ''}${host ? ' is-inline' : ''}${editing ? ' is-editing' : ''}`} aria-label="Обучение" aria-live="polite">
    <div className="game-onboarding-meta"><span><span className="game-onboarding-segments" aria-hidden="true">{[1, 2, 3].map(index => <i key={index} className={index <= step ? 'on' : ''} />)}</span>Знакомство с игрой · {step} из 3</span><span className="game-onboarding-reward-hint">Награда 2 💎</span></div>
    <h2>{waitingForReaction ? 'Герой готовит ответ' : title}</h2>
    <p>{lockedSection ? 'Этот раздел откроется после знакомства с игрой.' : waitingForReaction ? 'Дождись реакции героя. При ошибке можно повторить загрузку разговора.' : text}</p>
    {done && <p className="game-onboarding-reward-status">{isGameOnboardingPreview() ? 'DEV: предпросмотр без начисления' : claimMessage || (collecting ? 'Зачисляем кристаллы в баланс…' : onboarding?.bonusClaim === 'confirmed' ? 'Награда подтверждена сервером. Обнови баланс, чтобы завершить.' : onboarding?.bonusClaim ? 'Результат начисления пока неизвестен. Можно проверить баланс; повторный запрос награды не отправляем.' : 'Забери награду за первый пост и разговор с героем.')}</p>}
    {done ? <button className="game-onboarding-collect" data-onboarding-collect disabled={collecting} aria-busy={collecting} onClick={event => void collectReward(event.currentTarget)}>{collecting ? 'Забираем награду…' : !isGameOnboardingPreview() && onboarding?.bonusClaim ? onboarding.bonusClaim === 'confirmed' ? 'Обновить баланс' : 'Проверить баланс' : 'Забрать 2 💎'}</button> : onResume && <button onClick={() => { if (!isGameOnboardingPreview()) tap('onboarding_resume'); onResume() }}>Вернуться к обучению</button>}
  </aside>
  return host ? createPortal(dock, host) : dock
}

function CrystalShopIntro({ screen }: { screen: string }) {
  const { player } = useStore()
  const hasWallet = Boolean(player?.energyWallet)
  const [target, setTarget] = useState<HTMLButtonElement | null>(null)
  useLayoutEffect(() => {
    const button = document.querySelector<HTMLButtonElement>('[data-crystal-shop-trigger]')
    setTarget(button)
    button?.classList.add('game-onboarding-target')
    return () => button?.classList.remove('game-onboarding-target')
  }, [screen, hasWallet])
  if (!target?.isConnected) return null

  return <aside className="game-onboarding-progress" aria-label="Знакомство с магазином">
    <h2>На что потратить кристаллы</h2>
    <p>Нажми на баланс 💎. В магазине можно пополнить энергию или взять карты для постов.</p>
    <div className="row" style={{ gap: 16 }}>
      <button type="button" onClick={() => target.click()}>Открыть магазин</button>
      <button type="button" onClick={acknowledgeCrystalShopOnboarding}>Пропустить</button>
    </div>
  </aside>
}

export function GameOnboarding({ lockedSection = false }: { lockedSection?: boolean }) {
  const state = useGameOnboarding(), nav = useNav(), { player } = useStore()
  useEffect(() => { beginGameOnboardingPreview(player?.worldInstanceId ?? player?.world) }, [player?.id, player?.worldInstanceId])
  useEffect(() => {
    if (state?.phase === 'setup' && ['01-3_world', '01-3b_world_overview', '01-4_channel', '01-4b_world_setup'].includes(nav.current) && (state.setupScreen !== nav.current || state.setupWorld !== nav.params.world)) updateGameOnboarding({ setupScreen: nav.current, setupWorld: typeof nav.params.world === 'string' ? nav.params.world : undefined })
  }, [state?.phase, nav.current, nav.params.world])
  useEffect(() => {
    if (isGameOnboardingPreview() || state?.phase !== 'post' || !state.worldId || state.worldId !== (player?.worldInstanceId ?? player?.world)) return
    let active = true
    void api.channelPosts(state.worldId).then(page => {
      const post = page.items.find(item => item.postId && !item.repostId)
      if (active && post?.postId) onboardingPostSaved(state.worldId!, post.postId)
    }).catch(() => { /* A failed history check cannot advance the step. */ })
    return () => { active = false }
  }, [state?.phase, state?.worldId, player?.worldInstanceId])
  // Only reconcile the exact request started in this learning conversation.
  useEffect(() => {
    const postId = state?.conversationPostId ?? state?.postId
    if (!postId || !state?.replyRequestId || state.phase !== 'reply') return
    let active = true
    void api.post(postId).then(post => {
      if (!active || !post) return
      const reply = post.comments.find(comment => comment.requestId === state.replyRequestId && (comment.me || comment.authorId === `Player_${player?.id.replaceAll('-', '').toLowerCase()}`))
      const stars = player ? contentOf(player.world).stars : []
      const recipient = reply?.parentCommentId ? post.comments.find(comment => comment.id === reply.parentCommentId)?.authorId : post.authorId
      if (reply && stars.some(star => `Star_${star.id}` === recipient)) onboardingReplySaved(postId, state.replyRequestId!)
    }).catch(() => { /* Keep the step; the thread provides retry. */ })
    return () => { active = false }
  }, [state?.postId, state?.conversationPostId, state?.replyRequestId, player?.id])
  if (!state) return null
  const screen = nav.current
  if (state.completionSeen) return state.crystalShopIntroPending && ['01-3_world', '03-5_stage', '02-6_thread'].includes(screen)
    ? <CrystalShopIntro screen={screen} /> : null
  const routePostId = (nav.params.post as { id?: string } | undefined)?.id ?? nav.params.postId ?? (nav.params.result as { postId?: string } | undefined)?.postId
  const setup = state.phase === 'setup'
  const sameWorld = !state.worldId || state.worldId === (player?.worldInstanceId ?? player?.world)
  const done = state.phase === 'done'
  const step = setup ? 1 : state.phase === 'post' ? 2 : 3
  let target = '', text = '', title = done ? 'Обучение завершено' : setup ? 'Начни со своего мира' : step === 2 ? 'Начни историю постом' : 'Теперь твой ход'
  if (setup && screen === '01-3_world') { target = '.world-catalog > button:first-child .world-roster > span'; text = 'Выбери, где начнётся твоя история.' }
  if (setup && screen === '01-3b_world_overview') { target = '.bottom-cta'; text = 'Мир уже выбран. Теперь выбери своего персонажа.' }
  if (setup && screen === '01-4_channel') { target = '.persona-option.selected'; title = 'Кем ты будешь в истории?'; text = 'Выбери, от чьего лица пойдёт история.' }
  if (setup && screen === '01-4b_world_setup') { target = '.drama-option.selected'; title = 'Выбери настроение истории'; text = 'Выбери градус драмы и начни историю.' }
  if (sameWorld && state.phase === 'post' && (screen === '03-5_stage' || screen === '01-6_home')) { target = '.bottom-cta'; text = 'Ты ведёшь канал своего героя. Напиши первый пост — можно дописать готовую идею.' }
  if (sameWorld && state.phase === 'post' && screen === '02-1_compose') { target = '.bottom-cta'; text = 'Дополни готовую идею или напиши свою. Опубликуй пост, чтобы увидеть реакцию героев.' }
  if (sameWorld && state.postId && (screen === '02-6_thread' ? routePostId === (state.conversationPostId ?? state.postId) : routePostId === state.postId) && ['02-3_result', '02-4_flop', '02-5_viral', '02-6_thread'].includes(screen)) {
    target = `[data-game-onboarding-post="${CSS.escape(String(routePostId))}"]`
    if (screen === '02-6_thread') { text = 'Ответь герою — так начнётся разговор. Ответ · 1 энергия.' }
    else { text = 'На твой пост ответил герой. Нажми «Комментировать» и продолжи разговор.' }
  }
  if (!sameWorld && screen === '01-3_world') { target = `[data-game-onboarding-world="${CSS.escape(state.worldId!)}"]`; text = 'Продолжи эту историю — в ней сохранён твой шаг обучения.' }
  const waiting = sameWorld && (screen === '02-2_live' || screen === '02-2b_publish_error' || setup && ['01-2_age', '01-2b_under14'].includes(screen))
  const resume = async () => {
    if (!sameWorld) { nav.push('01-3_world', { onboardingWorldId: state.worldId }); return }
    if (state.postId) nav.push('02-6_thread', { postId: state.conversationPostId ?? state.postId, focusReply: true })
    else if (state.phase === 'post') nav.push('03-5_stage', { firstPostConfirmed: true })
    else {
      const draft = getWorldDraft()
      const world = state.setupWorld ?? pendingLandingWorld()
      nav.push(draft ? state.setupScreen === '01-4b_world_setup' ? '01-4b_world_setup' : '01-4_channel' : world ? '01-3b_world_overview' : '01-3_world', world ? { world } : {})
    }
  }
  if ((screen.startsWith('06-') || screen === '05-8_settings' || ['01-1_invite', '01-2_age', '01-2b_under14'].includes(screen))) return null
  if (done) text = 'Первый пост опубликован. Разговор начался.'
  else if (!target && !waiting) text = 'Твой шаг сохранён. Вернись к знакомству с игрой, чтобы продолжить.'
  else if (screen === '02-2_live') text = 'Дождись сохранения поста и реакции героя.'
  else if (screen === '02-2b_publish_error') text = 'Черновик сохранён. Исправь текст или повтори публикацию.'
  return <GameOnboardingCard target={done ? '' : target} title={title} text={text} step={step} done={done} lockedSection={lockedSection} onResume={!done && !target && !waiting ? () => void resume() : undefined} />
}
