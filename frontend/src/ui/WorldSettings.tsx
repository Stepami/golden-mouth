import { useRef, useState } from 'react'
import { api } from '../api/client'
import { worldDocumentOf } from '../api/worldContent'
import type { WorldDefinition } from '../api/types'
import { usePlayer, useStore } from '../app/store'
import { useNav } from '../app/nav'
import { savedTheme, saveTheme, type ThemePreference } from '../app/theme'
import { Avatar, Button, Sheet } from './index'

export function WorldSettings({ onClose }: { onClose: () => void }) {
  const p = usePlayer(); const { setPlayer, resetPlayer } = useStore(); const nav = useNav()
  const [draft, setDraft] = useState(() => worldDocumentOf(p.world))
  const [theme, setTheme] = useState<ThemePreference>(savedTheme)
  const [saving, setSaving] = useState(false), [error, setError] = useState('')
  const [confirmDelete, setConfirmDelete] = useState(false), [deleting, setDeleting] = useState(false)
  const deletePending = useRef(false)
  const removeAccount = async () => {
    if (deletePending.current) return
    deletePending.current = true; setDeleting(true); setError('')
    try { await api.deleteAccount(); resetPlayer(); nav.reset('01-2_age') }
    catch { setError('Не получилось удалить аккаунт. Попробуй ещё раз.') }
    finally { deletePending.current = false; setDeleting(false) }
  }
  const change = (key: 'name' | 'avatar' | 'role' | 'title' | 'description' | 'voice', value: string) => setDraft(d => ({ ...d, [key]: value }))
  const save = async () => {
    if (saving) return
    setSaving(true); setError('')
    try {
      // Merge only edited fields into a fresh full definition; retain server progress.
      const fresh = (await api.ownedWorlds()).find(w => w.id === p.worldInstanceId)
      if (!fresh || draft.stars.some(s => !fresh.stars.some(f => f.id === s.id))) throw new Error('World changed')
      const { id, subscribers: _subscribers, lock: _lock, ...base } = fresh
      const definition: WorldDefinition = { ...base, templateId: null, name: draft.name.trim(), avatar: draft.avatar.trim(), role: draft.role.trim(), title: draft.title.trim(), description: draft.description, voice: draft.voice, drama: draft.drama, difficulty: draft.difficulty,
        stars: fresh.stars.map(star => { const edited = draft.stars.find(s => s.id === star.id); return edited ? { ...star, about: edited.about, voice: edited.voice } : star }) }
      setPlayer(await api.saveWorld(id, definition)); onClose()
    } catch { setError('Не удалось сохранить настройки. Текст остаётся в форме. Попробуй ещё раз.') }
    finally { setSaving(false) }
  }
  const field = (key: 'name' | 'avatar' | 'role' | 'title' | 'description' | 'voice', label: string, maxLength: number, multiline = false) => <label>{label}{multiline ? <textarea value={draft[key]} maxLength={maxLength} disabled={saving} onChange={e => change(key, e.target.value)} /> : <input value={draft[key]} required maxLength={maxLength} disabled={saving} onChange={e => change(key, e.target.value)} />}</label>
  if (confirmDelete) return <Sheet title="Удалить аккаунт и все данные?" className="account-delete-sheet" onClose={deleting ? undefined : () => { setConfirmDelete(false); setError('') }}>
    <p>Все твои миры, записи, переписка и прогресс удалятся навсегда. Восстановить их будет нельзя.</p>
    {error && <p role="alert">{error}</p>}
    <Button disabled={deleting} style={{ background: 'var(--danger)', marginTop: 16, marginBottom: 8 }} onClick={removeAccount}>{deleting ? 'Удаляем…' : 'Удалить навсегда'}</Button>
    <Button secondary disabled={deleting} onClick={() => { setConfirmDelete(false); setError('') }}>Отмена</Button>
  </Sheet>
  return <Sheet title="Настройки мира" className="settings-sheet" onClose={saving ? undefined : onClose}>
    <section className="appearance-setting"><label>Тема приложения</label><div className="appearance-choices">{([['system','Системная'],['light','Светлая'],['dark','Тёмная']] as const).map(([value,label]) => <button key={value} aria-pressed={theme === value} onClick={() => { setTheme(value); saveTheme(value) }}>{label}</button>)}</div></section>
    <form onSubmit={e => { e.preventDefault(); void save() }}>
      <p className="settings-context">{draft.title}</p><h3>Твой персонаж</h3>
      <div className="avatar-setting"><Avatar seed={draft.avatar} size={56} /><div><b>{draft.name}</b><small>Аватар создаётся по нику. Загрузка фото пока недоступна.</small></div></div>
      <div className="form-pair">{field('name','Имя в мире',80)}{field('avatar','Ник',80)}</div>{field('role','Роль',80)}
      <details className="settings-section"><summary>Мир · название и описание</summary>{field('title','Название мира',120)}{field('description','Описание',2000,true)}</details>
      <details className="settings-section"><summary>Стиль и параметры событий</summary>{field('voice','Как ты пишешь',200)}<label>Интенсивность событий<select value={draft.drama} disabled={saving} onChange={e => setDraft(d => ({ ...d, drama: e.target.value as typeof d.drama }))}>{[['off','Без драмы'],['soft','Мягкая'],['mid','Умеренная'],['hot','Высокая']].map(([v,l]) => <option value={v} key={v}>{l}</option>)}</select></label><label>Сложность<select value={draft.difficulty} disabled={saving} onChange={e => setDraft(d => ({ ...d, difficulty: e.target.value as typeof d.difficulty }))}>{[['easy','Лёгкая'],['normal','Обычная'],['hard','Сложная']].map(([v,l]) => <option value={v} key={v}>{l}</option>)}</select></label></details>
      <details className="settings-section"><summary>Персонажи мира</summary>{draft.stars.map(star => <details className="setting-row" key={star.id}><summary>{star.name} · {star.role}</summary>{(['about','voice'] as const).map(key => <label key={key}>{key === 'about' ? 'О персонаже' : 'Голос'}<textarea value={star[key]} disabled={saving} onChange={e => setDraft(d => ({ ...d, stars: d.stars.map(s => s.id === star.id ? { ...s, [key]: e.target.value } : s) }))} /></label>)}</details>)}</details>
      <details className="settings-section"><summary>Правила · только чтение</summary>{draft.rules.map((rule,i) => <p key={i}>{rule.locked ? '🔒 ' : ''}{rule.text}</p>)}<p className="form-help">Изменения сохранят твои записи и прогресс.</p></details>
      <details className="settings-section"><summary>Настройки аккаунта</summary><p className="form-help">Уведомления, обращение, персонализация и данные.</p><button type="button" className="text-button" disabled={saving} onClick={() => { onClose(); nav.push('05-8b_account') }}>Открыть настройки аккаунта →</button></details>
      <button type="button" className="account-delete-action" disabled={saving} onClick={() => { setError(''); setConfirmDelete(true) }}>Удалить аккаунт и все данные</button>
      {error && <p role="alert">{error}</p>}<footer><Button secondary disabled={saving} onClick={onClose}>Отмена</Button><button className="btn" disabled={saving} type="submit">{saving ? 'Сохраняем…' : 'Сохранить'}</button></footer>
    </form>
  </Sheet>
}
