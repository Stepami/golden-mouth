import { useState } from 'react'
import { api } from '../api/client'
import { worldDocumentOf } from '../api/worldContent'
import { usePlayer, useStore } from '../app/store'
import { useNav } from '../app/nav'
import { selectDraftOwner, setWorldDraft } from '../app/worldDraft'
import { Button, TopBar } from '../ui'
import { AdvancedSetup, DescriptionSetup } from './worldWizard'
import { register } from '.'
import { AgeRuleNote } from './worldAdvanced'
import { worldArt } from '../app/worldPrototype'
import { avatarPersonalization } from '../app/avatarPersonalization'

function WorldDetailsScreen() {
  const nav = useNav(); const p = usePlayer(); const r = worldDocumentOf(p.world)
  const { resetPlayer, refresh } = useStore()
  const [loading, setLoading] = useState(false); const [error, setError] = useState('')
  const edit = async () => {
    setLoading(true); setError('')
    try {
      const world = (await api.ownedWorlds()).find(item => item.id === p.worldInstanceId)
      if (!world) throw new Error('World not found')
      const { id, subscribers: _subscribers, lock: _lock, ...definition } = world
      selectDraftOwner(p.id); setWorldDraft({ definition: { ...definition, templateId: null, rules: definition.rules.map(rule => ({ ...rule, locked: rule.text === 'Без тем 18+ и травли по внешности' })) }, advanced: false, editingId: id, address: p.address, group: p.ageBand === '14-17' ? '14-17' : '18+', ageBand: p.ageBand, personalizeAvatar: avatarPersonalization(p.id) })
      nav.push('05-7b_own_world', { world: p.world })
    } catch { setError('Не получилось загрузить настройки. Попробуй ещё раз.') } finally { setLoading(false) }
  }
  const remove = async () => {
    if (!p.worldInstanceId || !window.confirm('Удалить мир и все его посты? Это действие нельзя отменить.')) return
    setLoading(true); setError('')
    try {
      await api.deleteWorld(p.worldInstanceId)
      nav.reset('01-3_world'); resetPlayer(); await refresh()
    } catch { setError('Не получилось удалить мир. Попробуй ещё раз.'); setLoading(false) }
  }
  return <div className="screen no-tab"><TopBar onBack={nav.back} title="Настройки мира" rightPlain right={<button className="header-action" disabled={loading} onClick={remove}>🧺🌎 Удалить</button>} /><div className="pad world-details">
    <h1>{r.emoji} {r.title}</h1><p className="muted world-details-pitch">{worldArt[p.world]?.pitch ?? r.summary}</p>
    <div className="world-about"><h2>Твоя история</h2><p>{r.firstContext}</p></div>
    <div className="world-about"><h2>Как играть</h2><p>Публикуй находки, получай реакции героев и отвечай им, чтобы развивать историю.</p></div><details className="editor-extra"><summary>Все особенности мира</summary><p>{r.description}</p><div className="world-rule-list">{r.rules.map((rule, i) => rule.locked ? <AgeRuleNote key={i} teen={!p.ageBand || p.ageBand === '14-17'} /> : <p key={i}>✓ {rule.text}</p>)}</div></details>
    <p className="small muted editor-save-note">Подписчики и прогресс сохранятся.</p>
    <Button disabled={loading} onClick={edit}>{loading ? 'Загружаем…' : 'Редактировать мир'}</Button>
    <Button secondary onClick={() => nav.push('01-3_world')}>Выбрать другой мир</Button>
    {error && <p role="alert">{error}</p>}
  </div></div>
}
register({ '05-12_world_advanced': { component: AdvancedSetup, title: 'Редактирование мира' }, '05-7b_own_world': { component: DescriptionSetup, title: 'Описание и правила мира' }, '05-11_world_rules': { component: WorldDetailsScreen, title: 'Настройки мира' } })
