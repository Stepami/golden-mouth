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
  const heading = useRef<HTMLHeadingElement>(null);
  useEffect(() => { if (step > 0) heading.current?.focus({ preventScroll: true }); }, [step]);
  return <section className={`world-prologue world-prologue--${step}`} aria-label={`Знакомство с миром ${world.title}`}>
    <img className="prologue-image" src={world.hero} alt="" style={{ viewTransitionName: 'world-cover' }} />
    <div className="prologue-shade" />
    <header className="prologue-header">
      <div className="prologue-progress" aria-label={`Сцена ${step + 1} из 2`}>
        {[0, 1].map(i => <span key={i} className={i <= step ? 'is-read' : ''} />)}
      </div>
      <div className="row between">
        <button className="prologue-back" disabled={busy} onClick={() => step ? change(step - 1) : onBack()} aria-label={step ? 'Предыдущая сцена' : 'К мирам'}><Icon name="back"/></button>
        <span className="eyebrow">{world.genre}</span>
        <span className="prologue-page caption">0{step + 1} / 02</span>
      </div>
    </header>
    <div className="prologue-scenes">
      <div className={`prologue-scene ${step === 0 ? 'is-current' : 'is-past'}`} inert={step !== 0} aria-hidden={step !== 0}>
        <h1>{world.title}</h1>
        <p className="prologue-lead">{world.tagline}</p>
      </div>
      <div className={`prologue-scene ${step === 1 ? 'is-current' : 'is-next'}`} inert={step !== 1} aria-hidden={step !== 1}>
        <div className="prologue-chapter-heading"><p className="eyebrow">{world.title}</p><h2 ref={heading} tabIndex={-1}>С чего всё начинается</h2></div>
        <p className="prologue-story">{world.description}</p>
      </div>
    </div>
    <footer className="prologue-actions">
      <Button className="prologue-next full" busy={busy} onClick={() => resume || step === 1 ? onEnter(world.playerName) : change(1)}>
        {resume ? 'Продолжить игру' : step === 0 ? 'Погрузиться в историю' : 'Создать персонажа'}
      </Button>
      {!resume && <button className="prologue-skip" disabled={busy} onClick={() => step === 0 ? onEnter(world.playerName) : change(0)}>{step === 0 ? 'Сразу к выбору персонажа' : 'Вернуться к обложке'}</button>}
    </footer>
  </section>;
}
