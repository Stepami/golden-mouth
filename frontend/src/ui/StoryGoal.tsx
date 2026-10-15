import { presetGoalFor } from '../app/presetGoals'
import { worldDocumentOf } from '../api/worldContent'
import { useEffect, useState } from 'react'
import { api } from '../api/client'

export function usePresetGoal(world: string) {
  const [, loaded] = useState(false)
  useEffect(() => { let active = true; void api.loadWorldTemplates().then(() => { if (active) loaded(true) }).catch(() => { /* Story guidance is optional when templates are unavailable. */ }); return () => { active = false } }, [])
  return presetGoalFor(worldDocumentOf(world))
}

export function StoryGoal({ world, firstStep = false }: { world: string; firstStep?: boolean }) {
  const goal = usePresetGoal(world)
  if (!goal) return null
  return <details className="card story-goal"><summary>Твой путь <span className="small muted">в этой истории</span></summary>
    <p>{goal.goal}</p>
    {firstStep && <div className="story-first-step"><b className="small">Первый шаг</b><p>{goal.firstStep}</p></div>}
    <p className="small muted">Можно идти своим путём — это твоя история.</p>
  </details>
}
