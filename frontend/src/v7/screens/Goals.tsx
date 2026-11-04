import {
  Badge,
  Button,
  Icon,
  Notice,
  Progress,
  QuestRow,
  ScreenHeading,
} from "../ui";
import { skillNames } from "../domain/worlds";
import type { Quest, World, WorldSave } from "../domain/types";
export function Goals({
  world,
  save,
  onQuest,
  onProfile,
}: {
  world: World;
  save: WorldSave;
  onQuest: (q: Quest) => void;
  onProfile: () => void;
}) {
  const count = save.completedQuests.length;
  return (
    <>
      <ScreenHeading title="Цели" subtitle={world.title} />
      <section className="goal-hero">
        <div className="row">
          <span className="goal-symbol">
            <Icon name="goals" />
          </span>
          <h2>Ваша цель</h2>
        </div>
        <p>{world.goal}</p>
      </section>
      <section className="section">
        <div className="row between">
          <h2 className="section-title">День 1</h2>
          <span className="muted">{count} из 3 квестов</span>
        </div>
        <Progress label="Квесты игрового дня" value={count} max={3} />
        {world.quests.map((q, i) => (
          <QuestRow
            key={q.id}
            title={q.title}
            index={i + 1}
            status={
              save.completedQuests.includes(q.id)
                ? "done"
                : i === count
                  ? "active"
                  : "locked"
            }
            onClick={() => onQuest(q)}
          />
        ))}
        {count === 3 && (
          <div className="day-complete">
            <Badge tone="accent">День завершён</Badge>
            <h3>Разговор уже начался</h3>
            <p>Три разговора позади. Продолжай общаться в ленте.</p>
          </div>
        )}
      </section>
      <div className="section">
        <button className="link-button" onClick={onProfile}>
          Мой персонаж
        </button>
      </div>
    </>
  );
}
