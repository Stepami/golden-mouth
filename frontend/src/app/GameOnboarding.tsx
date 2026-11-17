import { createPortal } from 'react-dom'
import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import { contentOf } from '../api/worldContent'
import { api } from '../api/client'
import { useNav } from './nav'
import { useStore } from './store'
import { pendingLandingWorld } from './landing/experiment'
import { getWorldDraft } from './worldDraft'
import { acknowledgeGameOnboarding, beginGameOnboardingPreview, onboardingPostSaved, onboardingReplySaved, updateGameOnboarding, useGameOnboarding, isGameOnboardingPreview } from './GameOnboardingState'

export { isGameOnboardingPreview } from './GameOnboardingState'

function GameOnboardingCard({ target, title, text, step, done, lockedSection, onResume }: { target: string; title: string; text: string; step: number; done: boolean; lockedSection: boolean; onResume?: () => void }) {
  const card = useRef<HTMLElement>(null)
  const [host, setHost] = useState<HTMLElement | null>(null)
  const [editing, setEditing] = useState(false)
  const [targetReady, setTargetReady] = useState(false)
  const [collecting, setCollecting] = useState(false)
  const flight = useRef<{ cancelled: boolean; nodes: HTMLElement[]; animations: Animation[] } | null>(null)
  useEffect(() => () => {
    const active = flight.current
    if (!active) return
    active.cancelled = true
    active.animations.forEach(animation => animation.cancel())
    active.nodes.forEach(node => node.remove())
  }, [])
  const collectPreview = async (button: HTMLButtonElement) => {
    if (!isGameOnboardingPreview() || flight.current) return
    const active = { cancelled: false, nodes: [] as HTMLElement[], animations: [] as Animation[] }
    flight.current = active; setCollecting(true)
    try {
      const counter = document.querySelector<HTMLElement>('.crystal-button')
      if (!counter || matchMedia('(prefers-reduced-motion: reduce)').matches) return
      const start = button.getBoundingClientRect(), end = counter.getBoundingClientRect()
      if (!end.width || !end.height) return
      for (const index of [0, 1]) {
        const gem = document.createElement('span')
        gem.className = 'game-onboarding-crystal-flight'; gem.dataset.onboardingFlight = String(index)
        gem.setAttribute('aria-hidden', 'true'); gem.textContent = '💎'
        const offset = index === 0 ? -8 : 8
        gem.style.left = `${start.left + start.width / 2 - 14 + offset}px`
        gem.style.top = `${start.top + start.height / 2 - 14}px`
        document.body.append(gem); active.nodes.push(gem)
        const x = end.left + end.width / 2 - (start.left + start.width / 2 + offset)
        const y = end.top + end.height / 2 - (start.top + start.height / 2)
        active.animations.push(gem.animate([
          { transform: 'translate(0, 0) scale(.8)', opacity: 1 },
          { transform: `translate(${x * .4 + offset * 3}px, ${y * .35}px) scale(1.1)`, opacity: 1, offset: .4 },
          { transform: `translate(${x}px, ${y}px) scale(.4)`, opacity: 0 },
        ], { duration: 650, delay: index * 90, easing: 'cubic-bezier(.3,.7,.3,1)', fill: 'forwards' }))
      }
      await Promise.all(active.animations.map(animation => animation.finished))
    } catch { /* A preview animation failure must not block closing the preview. */ }
    finally {
      active.nodes.forEach(node => node.remove())
      if (!active.cancelled) {
        flight.current = null; setCollecting(false)
        if (isGameOnboardingPreview()) acknowledgeGameOnboarding()
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
    <div className="game-onboarding-meta"><span><span className="game-onboarding-segments" aria-hidden="true">{[1, 2, 3].map(index => <i key={index} className={index <= step ? 'on' : ''} />)}</span>Знакомство с игрой · {step} из 3</span>{isGameOnboardingPreview() && <span className="game-onboarding-reward-hint">Награда 2 💎</span>}</div>
    <h2>{waitingForReaction ? 'Герой готовит ответ' : title}</h2>
    <p>{lockedSection ? 'Этот раздел откроется после знакомства с игрой.' : waitingForReaction ? 'Дождись реакции героя. При ошибке можно повторить загрузку разговора.' : text}</p>
    {done && isGameOnboardingPreview() && <p className="game-onboarding-reward-status">DEV: предпросмотр без начисления</p>}
    {done ? isGameOnboardingPreview() ? <button className="game-onboarding-collect" data-onboarding-collect disabled={collecting} aria-busy={collecting} onClick={event => void collectPreview(event.currentTarget)}>Забрать 2 💎</button> : <button onClick={acknowledgeGameOnboarding}>Продолжить историю</button> : onResume && <button onClick={onResume}>Вернуться к обучению</button>}
  </aside>
  return host ? createPortal(dock, host) : dock
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
  if (!state || state.completionSeen) return null
  const screen = nav.current
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
