import { useEffect, useRef, useState } from 'react';
import { Button, Icon } from '../ui';
import type { World } from '../domain/types';

export function WorldIntro({ world, resume, busy = false, onBack, onEnter, step: controlledStep, onStepChange }: {
  world: World; resume: boolean; busy?: boolean; onBack: () => void;
  onEnter: (name: string) => void; step?: number; onStepChange?: (step: number) => void;
}) {
  const [localStep, setLocalStep] = useState(0);
  const step = controlledStep ?? localStep;
  const change = onStepChange ?? setLocalStep;
  const paragraphs = world.description.split(/\n\s*\n/).map(text => text.trim()).filter(Boolean);
  const sceneCount = paragraphs.length + 1;
  const lastScene = step === sceneCount - 1;
  const root = useRef<HTMLElement>(null);
  const heading = useRef<HTMLHeadingElement>(null);
  useEffect(() => {
    root.current?.closest('.app-body')?.scrollTo({top: 0});
    heading.current?.focus({ preventScroll: true });
  }, [step]);
  return <section ref={root} className={`world-prologue${step > 0 ? ' world-prologue--story' : ''}`} aria-label={`Знакомство с миром ${world.title}`}>
    <img className="prologue-image" src={world.hero} alt="" style={{ viewTransitionName: 'world-cover' }} />
    <div className="prologue-shade" />
    <header className="prologue-header">
      <div className="prologue-progress" aria-label={`Сцена ${step + 1} из ${sceneCount}`}>
        {Array.from({length: sceneCount}, (_, i) => <span key={i} className={i <= step ? 'is-read' : ''} />)}
      </div>
      <div className="row between">
        <button className="prologue-back" disabled={busy} onClick={() => step ? change(step - 1) : onBack()} aria-label={step ? 'Предыдущая сцена' : 'К мирам'}><Icon name="back"/></button>
        <span className="eyebrow">{world.genre}</span>
        <span className="prologue-page caption">{String(step + 1).padStart(2, '0')} / {String(sceneCount).padStart(2, '0')}</span>
      </div>
    </header>
    <div className="prologue-scenes">
      <div className={`prologue-scene ${step === 0 ? 'is-current' : 'is-past'}`} inert={step !== 0} aria-hidden={step !== 0}>
        <h1 ref={step === 0 ? heading : undefined} tabIndex={-1}>{world.title}</h1>
        <p className="prologue-lead">{world.tagline}</p>
      </div>
      {paragraphs.map((paragraph, index) => <div key={index} className={`prologue-scene ${step === index + 1 ? 'is-current' : step > index + 1 ? 'is-past' : 'is-next'}`} inert={step !== index + 1} aria-hidden={step !== index + 1}>
        <div className="prologue-chapter-heading"><p className="eyebrow">{world.title}</p><h2 ref={step === index + 1 ? heading : undefined} tabIndex={-1}>{world.introTitles?.[index] ?? (index === 0 ? 'За порогом нового мира' : `История продолжается · ${index + 1}`)}</h2></div>
        <p className="prologue-story">{paragraph}</p>
      </div>)}
    </div>
    <footer className="prologue-actions">
      <Button className="prologue-next full" busy={busy} onClick={() => resume || lastScene ? onEnter(world.playerName) : change(step + 1)}>
        {resume ? 'Продолжить игру' : lastScene ? 'Создать персонажа' : step === 0 ? 'Погрузиться в историю' : 'Дальше'}
      </Button>
      {!resume && <div className="prologue-secondary">{step > 0 && <button className="prologue-skip" disabled={busy} onClick={() => change(0)}>Вернуться к обложке</button>}</div>}
    </footer>
  </section>;
}
