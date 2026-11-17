import { createPortal } from 'react-dom'
import { useEffect, useLayoutEffect, useState } from 'react'
import { api } from '../api/client'
import { useNav } from './nav'
import { useStore } from './store'
import { pendingLandingWorld } from './landing/experiment'
import { getWorldDraft } from './worldDraft'
import { beginGameOnboardingPreview, onboardingPostSaved, updateGameOnboarding, useGameOnboarding, isGameOnboardingPreview } from './GameOnboardingState'

export { isGameOnboardingPreview } from './GameOnboardingState'

export function OnboardingBubble({ target, title, text, count, onSkip }: { target: string; title: string; text: string; count: string; onSkip: () => void }) {
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
    observer.observe(document.querySelector('.app') ?? document.body, { childList: true, subtree: true })
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
      <div className="game-onboarding-heading"><b>{node ? title : 'Обучение сохранено'}</b><span>{count}</span></div><p>{node ? text : 'Дождись загрузки действия. При ошибке можно повторить загрузку или пропустить обучение.'}</p>
      {(!fits || !visible) && node && <button onClick={() => node.scrollIntoView({ block: 'center', behavior: 'smooth' })}>Показать действие</button>}
      <button onClick={onSkip}>Пропустить обучение</button>
    </aside>
  </>
  const dialog = node?.closest('[role="dialog"]')
  return dialog ? createPortal(bubble, dialog) : bubble
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
  // A confirmed existing comment restores completion even if the tab closed after saving.
  useEffect(() => {
    if (!state?.postId || (state.phase !== 'reply' && state.phase !== 'reaction')) return
    let active = true
    void api.post(state.postId).then(post => {
      if (!active || !post) return
      if (post.comments.some(comment => (comment.me || comment.authorId === `Player_${player?.id.replaceAll('-', '').toLowerCase()}`) && comment.parentCommentId && post.comments.some(parent => parent.id === comment.parentCommentId && parent.star))) updateGameOnboarding({ phase: 'done' })
    }).catch(() => { /* Keep the step; opening the thread provides its existing retry. */ })
    return () => { active = false }
  }, [state?.postId, player?.id])
  if (!state || state.phase === 'done' || state.phase === 'skipped') return null
  const screen = nav.current
  const routePostId = (nav.params.post as { id?: string } | undefined)?.id ?? nav.params.postId ?? (nav.params.result as { postId?: string } | undefined)?.postId
  const setup = state.phase === 'setup'
  const sameWorld = !state.worldId || state.worldId === (player?.worldInstanceId ?? player?.world)
  let target = '', title = '', text = '', count = ''
  if (setup && screen === '01-3_world') { target = '.world-catalog > button:first-child'; title = 'С чего начнём?'; text = 'Выбери, где начнётся твоя история.'; count = '1 из 6' }
  if (setup && screen === '01-3b_world_overview') { target = '.bottom-cta'; title = 'Твоя история'; text = 'Мир уже выбран. Теперь выбери своего персонажа.'; count = '1 из 6' }
  if (setup && screen === '01-4_channel') { target = '.bottom-cta'; title = 'Кем будет твой герой?'; text = 'Выбери персонажа и настрой его имя. Затем выбери настроение истории.'; count = '2 из 6' }
  if (setup && screen === '01-4b_world_setup') { target = '.bottom-cta'; title = 'Настроение истории'; text = 'Выбери градус драмы и начни историю.'; count = '2 из 6' }
  if (sameWorld && state.phase === 'post' && (screen === '03-5_stage' || screen === '01-6_home')) { target = '.bottom-cta'; title = 'Это твой канал'; text = 'Ты ведёшь канал своего героя. Напиши первый пост — можно дописать готовую идею.'; count = '3 из 6' }
  if (sameWorld && state.phase === 'post' && screen === '02-1_compose') { target = '.bottom-cta'; title = 'Начни историю постом'; text = 'Дополни готовую идею или напиши свою. Опубликуй пост, чтобы увидеть реакцию героев.'; count = '4 из 6' }
  if (sameWorld && state.postId && routePostId === state.postId && ['02-3_result', '02-4_flop', '02-5_viral', '02-6_thread'].includes(screen)) {
    target = `[data-game-onboarding-post="${CSS.escape(state.postId)}"]`
    if (screen === '02-6_thread') { title = 'Продолжи разговор'; text = 'Ответь на реплику героя, чтобы продолжить разговор. Ответ стоит 1 энергию.'; count = '6 из 6' }
    else { title = 'Тебе ответил герой'; text = 'На твой пост ответил герой. Нажми «Комментировать» и продолжи разговор.'; count = '5 из 6' }
  }
  if (!sameWorld && screen === '01-3_world') { target = `[data-game-onboarding-world="${CSS.escape(state.worldId!)}"]`; title = 'Вернись к своей истории'; text = 'Продолжи эту историю — в ней сохранён твой шаг обучения.'; count = state.phase === 'post' ? '3 из 6' : '5 из 6' }
  const waiting = sameWorld && (screen === '02-2_live' || screen === '02-2b_publish_error' || setup && ['01-2_age', '01-2b_under14'].includes(screen))
  const resume = async () => {
    if (!sameWorld) { nav.push('01-3_world', { onboardingWorldId: state.worldId }); return }
    if (state.postId) nav.push('02-6_thread', { postId: state.postId, focusReply: true })
    else if (state.phase === 'post') nav.push('03-5_stage', { firstPostConfirmed: true })
    else {
      const draft = getWorldDraft()
      const world = state.setupWorld ?? pendingLandingWorld()
      nav.push(draft ? state.setupScreen === '01-4b_world_setup' ? '01-4b_world_setup' : '01-4_channel' : world ? '01-3b_world_overview' : '01-3_world', world ? { world } : {})
    }
  }
  return target ? <OnboardingBubble target={target} title={title} text={text} count={count} onSkip={() => updateGameOnboarding({ phase: 'skipped' })} /> : <aside className="game-onboarding-resume" aria-label="Обучение"><span>{waiting ? 'Обучение продолжится после этого шага.' : 'Обучение сохранено.'}</span>{!waiting && <button onClick={() => void resume()}>Вернуться к обучению</button>}<button onClick={() => updateGameOnboarding({ phase: 'skipped' })}>Пропустить обучение</button></aside>
}
