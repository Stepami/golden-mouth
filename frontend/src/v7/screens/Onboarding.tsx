import { useState } from "react";
import { communicationInterests } from "../interests";
import { Button, Chip, QuoteBlock } from "../ui";
export function Onboarding({
  onDone,
  onBack,
  initialQualities = [],
  initialStep = 0,
  onTry,
}: {
  onDone: (qualities: string[]) => void;
  onBack: () => void;
  initialQualities?: string[];
  initialStep?: 0 | 1;
  onTry?: () => void;
}) {
  const [step, setStep] = useState<0 | 1>(initialStep);
  const [selected, setSelected] = useState<string[]>(initialQualities);
  return (
    <section className="onboarding">
      <div className="row between">
        <span className="brand-small">реки®</span>
        {step === 1 && <button className="link-button" onClick={() => onDone([])}>
          Пропустить
        </button>}
      </div>
      {step === 0 ? (
        <>
          <div className="onboarding-art">
            <img
              src="assets/catalog-imgImage2.jpg"
              alt="Персонаж мира Gorpcore"
            />
            <div className="sample-bubble">Кто идёт знакомиться?</div>
            <div className="sample-bubble sample-bubble--answer">
              Сначала расскажи о себе ↗
            </div>
          </div>
          <h1 className="display">
            Твой пост.
            <br />
            Новая история.
          </h1>
          <p>
            Публикуй, отвечай и знакомься. Твои слова влияют на разговор и
            отношения с персонажами.
          </p>
          <p className="caption muted">Твои собеседники — AI-персонажи.</p>
          <Button size="lg" className="full" onClick={() => onTry ? onTry() : setStep(1)}>
            Попробовать ↗
          </Button>
        </>
      ) : (
        <>
          <div className="onboarding-star" aria-hidden="true">
            ✳
          </div>
          <h1 className="display">
            Что хочется
            <br />
            попробовать?
          </h1>
          <p className="muted">Выбери то, что тебе близко.</p>
          <div className="quality-grid">
            {communicationInterests.map((q) => (
              <Chip
                key={q}
                selected={selected.includes(q)}
                onClick={() =>
                  setSelected((x) =>
                    x.includes(q) ? x.filter((y) => y !== q) : [...x, q],
                  )
                }
              >
                {q}
              </Chip>
            ))}
          </div>
          <Button size="lg" className="full" onClick={() => onDone(selected)}>
            Продолжить ↗
          </Button>
          <button className="link-button" onClick={() => initialStep === 1 ? onBack() : setStep(0)}>
            Назад
          </button>
        </>
      )}
    </section>
  );
}
