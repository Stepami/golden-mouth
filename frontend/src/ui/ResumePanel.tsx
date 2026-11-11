import { usePlayer } from '../app/store'
import { useNav } from '../app/nav'
import { resumeTarget } from '../app/resumeContext'
export function ResumePanel() {
  const p = usePlayer(); const nav = useNav(); const target = resumeTarget(p)
  if (!target) return null
  return <button className="resume-panel" onClick={() => nav.push(target.screen, target.params)}><span aria-hidden="true">↩</span><span><strong>{target.title}</strong><small>{target.detail}</small></span><span aria-hidden="true">→</span></button>
}
