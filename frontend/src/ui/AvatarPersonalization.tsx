export function AvatarPersonalization({ enabled, onChange }: { enabled: boolean; onChange: (enabled: boolean) => void }) {
  return <div className="avatar-personalization">
    <label className="quiet-toggle"><input type="checkbox" checked={enabled} onChange={event => onChange(event.target.checked)} /><span>Персонализация аватарки</span></label>
    <p className="small muted">Учитывает обращение и возраст в доступных деталях внешности. Если выключить, портрет зависит только от ника и стиля мира.</p>
  </div>
}
