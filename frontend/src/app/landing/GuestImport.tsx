import { useEffect, useState } from 'react'
import { api, ApiError } from '../../api/client'
import { hasAccess, needsRegistration } from '../../api/auth'
import type { Player, Post } from '../../api/types'
import { worldDefinitionOf } from '../../api/worldContent'
import { Button } from '../../ui'
import { withPersona } from '../worldDraft'
import { trackLanding } from '../analytics'
import { completeLandingPost } from './experiment'
import { recordEntryStep } from './openobserve'
import { saveGuestOnboarding } from '../GameOnboardingState'
import { readReplyDraft, replyDraftKey, saveReplyDraft } from '../postDraft'
import { rememberDailyTaskPost } from '../dailyTaskIntro'
import { pendingReferral } from '../referral'
import { acceptedRegistrationConsent, readGuestHandoff, saveGuestHandoff, type GuestHandoff } from './guestHandoff'

type Imported = { player: Player; post: Post; notice?: string; replyCommentId?: string }
class GuestImportError extends Error {}
let activeImport: Promise<Imported> | undefined

async function transferGuestStory(birthYear?: number): Promise<Imported> {
  const handoff = readGuestHandoff()
  if (!handoff?.id || !handoff.hero || !handoff.dialogue?.starId || !handoff.dialogue.first?.trim() || !handoff.text.trim()) {
    throw new GuestImportError('Не нашли гостевую историю на этом устройстве.')
  }
  const transferId = handoff.id
  const dialogue = handoff.dialogue
  let state: GuestHandoff = handoff
  const checkpoint = (change: Partial<GuestHandoff>) => {
    const saved = readGuestHandoff()
    if (saved?.id !== transferId || (state.playerId && saved.playerId !== state.playerId) || !saveGuestHandoff({ ...state, ...change })) {
      throw new GuestImportError('Не получилось сохранить этап переноса на устройстве. Проверь хранилище браузера.')
    }
    state = { ...state, ...change }
  }
  // A durable checkpoint is required before sending any write.
  checkpoint({})
  if (!state.playerId) {
    if (!birthYear || birthYear > new Date().getFullYear() - 18 || !acceptedRegistrationConsent() || (!needsRegistration() && !hasAccess())) {
      throw new GuestImportError('Нужна подтверждённая регистрация нового аккаунта.')
    }
    await api.onboarding({ birthYear, address: 'nick', referralCode: pendingReferral() }, playerId => checkpoint({ playerId }))
    if (!state.playerId) throw new GuestImportError('Не удалось связать историю с новой регистрацией. Игра в существующем аккаунте сохранена.')
  }
  if (!hasAccess()) throw new GuestImportError('Войди тем же способом, чтобы продолжить перенос.')
  if (!api.hasPlayer()) await api.restore()
  const playerId = state.playerId
  if (api.playerId() !== playerId) throw new GuestImportError('Эта история сохраняется в другом аккаунте.')

  const confirmWorld = (worldInstanceId: string) => {
    checkpoint({ worldInstanceId })
    recordEntryStep('landing_world_selected')
    recordEntryStep('landing_character_selected')
  }
  if (!state.worldInstanceId) {
    await api.loadWorldTemplates()
    const template = worldDefinitionOf(state.worldId)
    const hero = template.stars.find(star => star.id === handoff.hero)
    if (!hero || hero.id === dialogue.starId || !template.stars.some(star => star.id === dialogue.starId)) {
      throw new GuestImportError('В выбранном мире больше нет героя из гостевой истории.')
    }
    const definition = withPersona({ definition: template, address: 'nick', group: '18+' }, hero).definition
    definition.difficulty = 'normal'
    definition.drama = 'mid'
    const worlds = await api.ownedWorlds()
    if (state.worldPending) {
      const found = worlds.find(world => world.title === definition.title && world.name === definition.name && world.avatar === definition.avatar
        && world.description === definition.description && world.difficulty === definition.difficulty && world.drama === definition.drama)
      if (worlds.length !== 1 || !found) {
        throw new GuestImportError('Пока не удалось подтвердить создание мира. Проверь ещё раз, чтобы не создать копию.')
      }
      confirmWorld(found.id)
    } else {
      if (worlds.length) throw new GuestImportError('В аккаунте уже есть мир. Открой свою игру.')
      checkpoint({ worldPending: true })
      try { await api.createWorld(definition, confirmWorld) }
      catch (cause) {
        if (!state.worldInstanceId && cause instanceof ApiError && [400, 401, 403, 422].includes(cause.status)) checkpoint({ worldPending: false })
        throw cause
      }
    }
  }
  const worldId = state.worldInstanceId
  if (!worldId) throw new GuestImportError('Не удалось подтвердить создание мира.')
  const loadPlayer = async () => {
    const player = await api.me(worldId)
    if (player.id !== playerId || player.worldInstanceId !== worldId) throw new GuestImportError('Не удалось открыть мир гостевой истории.')
    return player
  }
  await loadPlayer()

  if (!state.postId) {
    if (state.postPending) {
      const page = await api.channelPosts(worldId)
      const found = page.items.find(item => item.text === state.text)
      if (page.items.length !== 1 || page.nextCursor || !found?.postId) {
        throw new GuestImportError('Пока не удалось подтвердить публикацию. Проверь ещё раз, чтобы не отправить пост повторно.')
      }
      checkpoint({ postId: found.postId })
      if (completeLandingPost()) trackLanding('landing_first_post')
    } else {
      checkpoint({ postPending: true })
      try {
        const { post } = await api.createPost(state.text, true)
        checkpoint({ postId: post.id })
      } catch (cause) {
        if (!state.postId && cause instanceof ApiError && [400, 401, 403, 409, 422].includes(cause.status)) checkpoint({ postPending: false })
        throw cause
      }
    }
  }
  const postId = state.postId
  if (!postId) throw new GuestImportError('Не удалось подтвердить публикацию.')
  const requestId = (step: string) => `${transferId}-${step}`
  const starCommentId = state.initialCommentId ?? await api.savePresetReply(postId, null, requestId('first'), dialogue.first, dialogue.starId)
  if (!state.initialCommentId) checkpoint({ initialCommentId: starCommentId })
  const commentIds = [starCommentId]
  const final = state.reply.trim() ? dialogue.final : undefined
  if (final) {
    const replyId = state.playerCommentId ?? await api.savePresetReply(postId, starCommentId, requestId('reply'), state.reply)
    if (!state.playerCommentId) checkpoint({ playerCommentId: replyId })
    const finalId = state.finalCommentId ?? await api.savePresetReply(postId, replyId, requestId('last'), final, dialogue.starId)
    if (!state.finalCommentId) checkpoint({ finalCommentId: finalId })
    commentIds.push(replyId, finalId)
  }
  const post = await api.post(postId)
  if (!post || !commentIds.every(id => post.comments.some(comment => comment.id === id))) {
    throw new GuestImportError('Пока не удалось открыть сохранённое обсуждение.')
  }

  if (final && !state.claim) {
    checkpoint({ claim: 'pending' })
    const granted = await api.claimOnboardingBonus(postId, AbortSignal.timeout(15000)).catch(() => undefined)
    if (granted !== undefined) checkpoint({ claim: granted ? 'confirmed' : 'declined' })
  }
  const player = await loadPlayer()
  if (final && !player.energyWallet) throw new GuestImportError('Пока не удалось загрузить баланс аккаунта.')
  if (!final && state.reply) {
    const key = replyDraftKey(player, postId)
    if (!readReplyDraft(key) && !saveReplyDraft(key, { requestId: requestId('reply'), commentId: starCommentId, text: state.reply, submitted: false })) {
      throw new GuestImportError('Не получилось перенести черновик ответа. Проверь хранилище браузера и попробуй ещё раз.')
    }
  }
  const notice = state.claim === 'pending'
    ? 'Сервер не подтвердил награду. На экране показан текущий баланс аккаунта.'
    : state.claim === 'declined' ? 'Награда за этот пост недоступна. История сохранена.' : undefined
  if (!saveGuestOnboarding(playerId, worldId, postId, !!final, state.claim === 'confirmed' ? 'confirmed' : state.claim === 'pending' ? 'uncertain' : undefined)) {
    throw new GuestImportError('Проверь хранилище браузера, чтобы завершить сохранение.')
  }
  if (!post.repostId) rememberDailyTaskPost(playerId, post.id, true)
  return { player, post, notice, replyCommentId: final ? undefined : starCommentId }
}

function importGuestStory(birthYear?: number): Promise<Imported> {
  return activeImport ??= (
    navigator.locks
      ? navigator.locks.request('reki:guest-import', () => transferGuestStory(birthYear))
      : transferGuestStory(birthYear)
  ).finally(() => { activeImport = undefined })
}

export function GuestImport({ birthYear, onDone, onExit }: {
  birthYear?: number
  onDone: (result: Imported) => void
  onExit: () => void
}) {
  const [attempt, setAttempt] = useState(0)
  const [error, setError] = useState('')
  useEffect(() => {
    let mounted = true
    const timeout = window.setTimeout(() => {
      void importGuestStory(birthYear).then(result => { if (mounted) onDone(result) }).catch(cause => {
        if (mounted) setError(cause instanceof GuestImportError
          ? cause.message : 'Не получилось подтвердить сохранение истории. Проверь подключение и попробуй снова.')
      })
    }, 0)
    return () => { mounted = false; clearTimeout(timeout) }
  }, [attempt, birthYear, onDone])
  return <div className="app"><div className="screen no-tab"><div className="pad">
    <h1>Сохраняем твою историю…</h1>
    {error ? <>
      <p role="alert">{error}</p>
      <Button onClick={() => { setError(''); setAttempt(value => value + 1) }}>Продолжить сохранение</Button>
      {hasAccess() && !readGuestHandoff()?.playerId && <Button secondary onClick={onExit}>Открыть игру</Button>}
    </> : <p className="muted" role="status">Переносим твой мир, пост и обсуждение в аккаунт.</p>}
  </div></div></div>
}
