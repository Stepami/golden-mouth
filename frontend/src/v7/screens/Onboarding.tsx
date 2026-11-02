import { useState } from "react";
import { Button, Chip, QuoteBlock } from "../ui";
export function Onboarding({
  onDone,
  onBack,
}: {
  onDone: (qualities: string[]) => void;
  onBack: () => void;
}) {
  const [step, setStep] = useState(0);
  const [selected, setSelected] = useState<string[]>([]);
  return (
    <section className="onboarding">
      <div className="row between">
        <span className="brand-small">реки®</span>
        <button className="link-button" onClick={() => onDone([])}>
          Пропустить
        </button>
      </div>
      <div className="onboarding-steps" aria-label={`Шаг ${step + 1} из 2`}>
        <span className="is-active" />
        <span className={step === 1 ? "is-active" : ""} />
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
          <Button size="lg" className="full" onClick={() => setStep(1)}>
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
            {[
              "Смелее высказываться",
              "Лучше слышать других",
              "Увереннее убеждать",
              "Находить общий язык",
              "Легче шутить",
              "Быть собой",
            ].map((q) => (
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
          <button className="link-button" onClick={() => setStep(0)}>
            Назад
          </button>
        </>
      )}
    </section>
  );
}
