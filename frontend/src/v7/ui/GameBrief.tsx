import type { ReactNode } from 'react';
import {Icon} from './index';

/** The same goal follows the character through selection, briefing and profile. */
export function GoalCard({ goal, onClick }: { goal: string; onClick?: () => void }) {
  const Tag = onClick ? 'button' : 'div';
  return <Tag className="goal-card" onClick={onClick}>
    <span className="card-label">Цель {onClick && <span aria-hidden="true">↗</span>}</span>
    <strong className="card-title">{goal}</strong>
  </Tag>;
}

/** The briefing and editor display one task using the same visual hierarchy. */
export function TaskCard({ title, prompt, children, status, outcome }: { title: string; prompt: string; children?: ReactNode; status?:string; outcome?:string }) {
  return <section className="task-card">
    <div className="task-heading"><p className="card-label">Текущее задание</p>{status&&<span className="task-status">{status}</span>}</div>
    <h2 className="task-action">{prompt}</h2>
    {children}
    <details className="task-context"><summary>Контекст задания<Icon name="chevron"/></summary><p>{title}</p>{outcome&&<p className="caption muted">{outcome}</p>}</details>
  </section>;
}
