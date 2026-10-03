import { Component, type ReactNode } from 'react'
import { error } from './log'

/** Падение экрана не превращается в белый лист: показываем мягкий экран и пишем ошибку в лог. */
export class ErrorBoundary extends Component<{ children: ReactNode }, { failed: boolean }> {
  state = { failed: false }
  static getDerivedStateFromError() { return { failed: true } }
  componentDidCatch(e: unknown) { error('render_error', e) }
  render() {
    if (!this.state.failed) return this.props.children
    return (
      <div className="app" style={{ padding: '80px 26px', textAlign: 'center' }}>
        <div style={{ fontSize: 56 }}>🌧</div>
        <div style={{ fontSize: 22, fontWeight: 800, margin: '12px 0 6px' }}>Что-то пошло не так</div>
        <div className="muted" style={{ marginBottom: 24 }}>Мы уже знаем об ошибке. Прогресс сохранён</div>
        <button className="btn" onClick={() => location.reload()}>Открыть заново</button>
      </div>
    )
  }
}
