import { ProfileHeader, ResumeWorld, SettingsRow, Button } from "../ui";
import { worlds } from "../domain/worlds";
import type { GameState, World } from "../domain/types";

export function Account({
  state,
  onWorld,
  onBrowse,
  onResources,
  onInterests,
  textScale,
  onTextSize,
}: {
  state: GameState;
  textScale: number;
  onTextSize: () => void;
  onWorld: (world: World) => void;
  onBrowse: () => void;
  onResources: () => void;
  onInterests: () => void;
}) {
  const saved = worlds.filter((w) => state.saves[w.id]);
  return (
    <>
      <ProfileHeader
        label="Профиль"
        name="Игрок"
        avatar="assets/s1-imgEllipse6.png"
      />
      <section
        className="account-worlds"
        aria-labelledby="account-worlds-title"
      >
        <h2 id="account-worlds-title" className="section-title">
          Мои миры
        </h2>
        {saved.length ? (
          saved.map((w) => (
            <ResumeWorld
              key={w.id}
              world={w}
              subtitle={state.saves[w.id]?.playerName ?? w.playerName}
              onClick={() => onWorld(w)}
            />
          ))
        ) : (
          <Button onClick={onBrowse}>Выбрать мир</Button>
        )}
      </section>
      <section className="account-settings" aria-label="Настройки профиля">
        <SettingsRow
          label="Ресурсы"
          value={`${state.energy}/10 энергии · ${state.crystals} кристалла`}
          onClick={onResources}
        />
        <SettingsRow
          label="Размер текста"
          value={`${textScale * 100}%`}
          onClick={onTextSize}
        />
        <SettingsRow
          label="Интересы"
          value={
            state.qualities.length ? state.qualities.join(" · ") : "Выбрать"
          }
          onClick={onInterests}
        />
      </section>
    </>
  );
}
