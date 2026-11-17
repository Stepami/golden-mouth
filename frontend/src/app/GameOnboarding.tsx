import { createPortal } from 'react-dom'
import { useEffect, useLayoutEffect, useState } from 'react'
import { contentOf } from '../api/worldContent'
import { api } from '../api/client'
import { useNav } from './nav'
import { useStore } from './store'
import { pendingLandingWorld } from './landing/experiment'
import { getWorldDraft } from './worldDraft'
import { acknowledgeGameOnboarding, beginGameOnboardingPreview, onboardingPostSaved, onboardingReplySaved, updateGameOnboarding, useGameOnboarding, isGameOnboardingPreview } from './GameOnboardingState'

export { isGameOnboardingPreview } from './GameOnboardingState'

export function GameOnboardingProgress() {
  const state = useGameOnboarding(), nav = useNav()
  if (!state || state.completionSeen || (nav.current.startsWith('06-') || nav.current === '05-8_settings' || ['01-1_invite', '01-2_age', '01-2b_under14'].includes(nav.current))) return null
  if (state.phase === 'done') return <section className="game-onboarding-finish" role="status"><span className="game-onboarding-finish-mark">✓</span><div><h2>Обучение завершено</h2><p>Первый пост опубликован. Разговор начался.</p>{isGameOnboardingPreview() && <p className="game-onboarding-reward">2 💎 за обучение<br /><span>Начисление ожидает подключения</span></p>}<button onClick={acknowledgeGameOnboarding}>Продолжить историю</button></div></section>
  const completed = state.phase === 'setup' ? 0 : state.phase === 'post' ? 1 : 2
  const action = state.phase === 'setup' ? nav.current === '01-4_channel' ? 'Выбери персонажа' : nav.current === '01-4b_world_setup' ? 'Настрой настроение истории' : 'Выбери мир' : state.phase === 'post' ? 'Опубликуй первый пост' : state.phase === 'reaction' ? 'Дождись ответа героя' : 'Сохрани ответ герою'
  const routePostId = (nav.params.post as { id?: string } | undefined)?.id ?? nav.params.postId
  const awayFromConversation = nav.current === '02-6_thread' && state.postId && routePostId !== (state.conversationPostId ?? state.postId)
  return <div className="game-onboarding-progress" role="status"><div><b>Глава {completed + 1}: {['История', 'Пост', 'Разговор'][completed]}</b><span>Выполнено {completed} из 3</span></div><div className="game-onboarding-segments" aria-label={`${completed} из 3 глав завершено`}>{['История', 'Пост', 'Разговор'].map((label, index) => <span key={label} className={index < completed ? 'complete' : index === completed ? 'current' : ''}><i />{label}</span>)}</div><p className="game-onboarding-action"><span>{awayFromConversation ? 'Вернись к разговору обучения' : action}</span>{isGameOnboardingPreview() && <span className="game-onboarding-reward-hint">За обучение 2 💎</span>}</p></div>
}

export function OnboardingBubble({ target, text }: { target: string; text: string }) {
  const [rect, setRect] = useState<DOMRect | null>(null)
  const [height, setHeight] = useState(150)
  const [node, setNode] = useState<HTMLElement | null>(null)
  useLayoutEffect(() => {
    const update = () => {
      const element = document.querySelector<HTMLElement>(target)
      setNode(element)
      setRect(element?.getBoundingClientRect() ?? null)
    }
    update()
    const observer = new MutationObserver(update)
    observer.observe(document.querySelector('.app') ?? document.body, { childList: true, subtree: true, attributes: true, attributeFilter: ['data-game-onboarding-post', 'data-game-onboarding-world', 'aria-pressed'] })
    window.addEventListener('resize', update); window.addEventListener('scroll', update, true)
    window.visualViewport?.addEventListener('resize', update)
    return () => { observer.disconnect(); window.removeEventListener('resize', update); window.removeEventListener('scroll', update, true); window.visualViewport?.removeEventListener('resize', update) }
  }, [target])
  const viewport = window.visualViewport?.height ?? window.innerHeight
  const visible = rect && rect.bottom > 0 && rect.top < viewport
  const above = !!rect && rect.top > height + 24
  const top = visible && rect ? above ? rect.top - height - 14 : Math.min(rect.bottom + 14, viewport - height - 12) : 80
  const fits = !!rect && (above || rect.bottom + height + 24 < viewport)
  const bubble = <>
    {visible && fits && rect && <div className="game-onboarding-spotlight" aria-hidden="true" style={{ top: rect.top - 5, left: rect.left - 5, width: rect.width + 10, height: rect.height + 10 }} />}
    <aside ref={element => { if (element && Math.abs(element.offsetHeight - height) > 1) setHeight(element.offsetHeight) }} className={`game-onboarding-bubble${above ? ' above' : ''}`} style={{ top: fits ? top : 72 }} aria-label="Обучение" aria-live="polite">
      <p>{node ? text : 'Ждём ответ героя…'}</p>
      {(!fits || !visible) && node && <button onClick={() => node.scrollIntoView({ block: 'center', behavior: 'smooth' })}>Показать действие</button>}
    </aside>
  </>
  const dialog = node?.closest('[role="dialog"]')
  const hint = target === '.persona-option.selected' ? 'character' : target === '.drama-option.selected' ? 'mood' : null
  const inline = hint ? document.querySelector(`[data-game-onboarding-hint="${hint}"]`) : null
  if (!node) return null
  return inline ? createPortal(bubble, inline) : dialog ? createPortal(bubble, dialog) : bubble
}

export function GameOnboarding() {
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
  if (!state || state.phase === 'done') return null
  const screen = nav.current
  const routePostId = (nav.params.post as { id?: string } | undefined)?.id ?? nav.params.postId ?? (nav.params.result as { postId?: string } | undefined)?.postId
  const setup = state.phase === 'setup'
  const sameWorld = !state.worldId || state.worldId === (player?.worldInstanceId ?? player?.world)
  let target = '', text = ''
  if (setup && screen === '01-3_world') { target = '.world-catalog > button:first-child'; text = 'Выбери, где начнётся твоя история.' }
  if (setup && screen === '01-3b_world_overview') { target = '.bottom-cta'; text = 'Мир уже выбран. Теперь выбери своего персонажа.' }
  if (setup && screen === '01-4_channel') { target = '.persona-option.selected'; text = 'Выбери, от чьего лица пойдёт история.' }
  if (setup && screen === '01-4b_world_setup') { target = '.drama-option.selected'; text = 'Выбери градус драмы и начни историю.' }
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
  return target ? <OnboardingBubble target={target} text={text} /> : waiting ? null : <button className="game-onboarding-continue" onClick={() => void resume()}>{state.postId ? 'Вернуться к разговору обучения' : 'Продолжить обучение'}</button>
}
