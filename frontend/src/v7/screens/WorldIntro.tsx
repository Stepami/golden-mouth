import { Button } from "../ui";
import type { World } from "../domain/types";
export function WorldIntro({
  world,
  resume,
  busy = false,
  onBack,
  onEnter,
}: {
  world: World;
  resume: boolean;
  busy?: boolean;
  onBack: () => void;
  onEnter: (name: string) => void;
}) {
  return (
    <div className="world-intro">
      <div className="world-hero">
        <img src={world.hero} alt={world.title} />
        <button className="back-button hero-back" onClick={onBack} disabled={busy}>
          Назад
        </button>
      </div>
      <section className="intro-sheet">
        <h1 className="display">{world.title}</h1>
        <div className="intro-description">
          <p>{world.description}</p>
        </div>
      </section>
      <div className="intro-cta">
        <Button
          busy={busy}
          size="lg"
          className="full"
          onClick={() => onEnter(world.playerName)}
        >
          {resume ? "Продолжить игру" : "Начать игру"}
        </Button>
      </div>
    </div>
  );
}
