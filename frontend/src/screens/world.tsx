import { useState } from 'react'
import { api } from '../api/client'
import { optionalWorldTemplate, worldDocumentOf } from '../api/worldContent'
import { usePlayer, useStore } from '../app/store'
import { useNav } from '../app/nav'
import { selectDraftOwner, setWorldDraft } from '../app/worldDraft'
import { Button, Sheet, TopBar } from '../ui'
import { AdvancedSetup, DescriptionSetup, WorldCreated } from './worldWizard'
import { register } from '.'
import { worldArt } from '../app/worldPrototype'
import { avatarPersonalization } from '../app/avatarPersonalization'

function WorldDetailsScreen() {
  const nav = useNav(); const p = usePlayer(); const r = worldDocumentOf(p.world)
  const custom = !optionalWorldTemplate(r)
  const { resetPlayer, refresh } = useStore()
  const [loading, setLoading] = useState(false); const [error, setError] = useState('')
  const [confirmDelete, setConfirmDelete] = useState(false)
  const edit = async () => {
    setLoading(true); setError('')
    try {
      const world = (await api.ownedWorlds()).find(item => item.id === p.worldInstanceId)
      if (!world) throw new Error('World not found')
      const { id, subscribers: _subscribers, lock: _lock, ...definition } = world
      selectDraftOwner(p.id); setWorldDraft({ definition: { ...definition, templateId: null, rules: definition.rules.map(rule => ({ ...rule, locked: rule.text === 'Без тем 18+ и травли по внешности' })) }, advanced: custom, editingId: id, customEditing: custom, address: p.address, group: p.ageBand === '14-17' ? '14-17' : '18+', ageBand: p.ageBand, personalizeAvatar: avatarPersonalization(p.id) })
      nav.push('05-7b_own_world', { world: p.world })
    } catch { setError('Не получилось загрузить настройки. Попробуй ещё раз.') } finally { setLoading(false) }
  }
  const remove = async () => {
    if (!p.worldInstanceId || loading) return
    setLoading(true); setError('')
    try {
      await api.deleteWorld(p.worldInstanceId)
      nav.reset('01-3_world'); resetPlayer(); await refresh()
    } catch { setError('Не получилось удалить мир. Попробуй ещё раз.'); setLoading(false) }
  }
  return <div className="screen no-tab"><TopBar onBack={nav.back} title="Настройки мира" rightPlain right={<button className="header-action delete-trigger" disabled={loading} onClick={() => { setError(''); setConfirmDelete(true) }}>🧺🌎 Удалить</button>} /><div className="pad world-details">
    <h1>{r.emoji} {r.title}</h1><p className="muted world-details-pitch">{worldArt[p.world]?.pitch ?? r.summary}</p>
    <div className="world-about"><h2>Твоя история</h2><p>{r.firstContext}</p></div>
    <div className="world-about"><h2>Как играть</h2><p>Публикуй находки, получай реакции героев и отвечай им, чтобы развивать историю.</p></div><details className="editor-extra"><summary>Все особенности мира</summary><p>{r.description}</p><div className="world-rule-list">{r.rules.filter(rule => !rule.locked).map((rule, i) => <p key={i}>✓ {rule.text}</p>)}</div></details>
    <p className="small muted editor-save-note">Подписчики и прогресс сохранятся.</p>
    <Button disabled={loading} onClick={edit}>{loading ? 'Загружаем…' : 'Редактировать мир'}</Button>
    <Button secondary onClick={() => nav.push('01-3_world')}>Выбрать другой мир</Button>
    {error && !confirmDelete && <p role="alert">{error}</p>}
  </div>{confirmDelete && <Sheet onClose={() => { if (!loading) setConfirmDelete(false) }}>
    <h2>Удалить мир?</h2>
    <p style={{ margin: '8px 0 16px' }}>Мир и все его посты будут удалены. Отменить это действие нельзя.</p>
    {error && <p role="alert" style={{ color: 'var(--warn)', marginBottom: 12 }}>{error}</p>}
    <Button disabled={loading} onClick={remove} style={{ marginBottom: 8, background: 'transparent', border: '2px solid #F06473', color: '#F06473' }}>{loading ? 'Удаляем…' : 'Удалить мир'}</Button>
    <Button secondary disabled={loading} onClick={() => setConfirmDelete(false)}>Отмена</Button>
  </Sheet>}</div>
}
register({ '05-12_world_advanced': { component: AdvancedSetup, title: 'Редактирование мира' }, '05-7b_own_world': { component: DescriptionSetup, title: 'Описание и правила мира' }, '05-7d_world_created': { component: WorldCreated, title: 'Мир создан' }, '05-11_world_rules': { component: WorldDetailsScreen, title: 'Настройки мира' } })
