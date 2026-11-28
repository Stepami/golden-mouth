import type { ReactNode } from 'react'

export function AvatarPersonalization({ enabled, onChange, info }: { enabled: boolean; onChange: (enabled: boolean) => void; info?: ReactNode }) {
  const toggle = <label className="quiet-toggle"><input type="checkbox" checked={enabled} onChange={event => onChange(event.target.checked)} /><span>Персонализация аватарки</span></label>
  return <div className="avatar-personalization">
    {info ? <div className="world-field-label">{toggle}{info}</div> : <>
      {toggle}
      <p className="small muted">Учитывает обращение и возраст в доступных деталях внешности. Если выключить, портрет зависит только от ника и стиля мира.</p>
    </>}
  </div>
}
