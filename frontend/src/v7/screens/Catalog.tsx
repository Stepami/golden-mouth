import { useState } from "react";
import {
  Badge,
  Chip,
  EmptyState,
  ResumeWorld,
  ScreenHeading,
  TextField,
  WorldCard,
} from "../ui";
import { worlds } from "../domain/worlds";
import type { GameState, World } from "../domain/types";
export function Catalog({
  state,
  onWorld,
  search = false,
}: {
  state: GameState;
  onWorld: (world: World) => void;
  search?: boolean;
}) {
  const [query, setQuery] = useState("");
  const [genre, setGenre] = useState("Все");
  const filtered = worlds.filter(
    (w) =>
      (genre === "Все" || w.genre === genre) &&
      `${w.title} ${w.description}`
        .toLocaleLowerCase("ru")
        .includes(query.toLocaleLowerCase("ru")),
  );
  const saved = worlds.filter((w) => state.saves[w.id]);
  return (
    <>
      <ScreenHeading title={search ? "Найти свой мир" : "Миры"} />
      {search && (
        <div className="search-field catalog-overview">
          <TextField
            label="Название или тема"
            placeholder="Магия, прогулки, семейный чат…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            autoFocus
          />
        </div>
      )}
      {!search && saved.length > 0 && (
        <section className="saved-worlds catalog-overview">
          <div className="row between">
            <h2 className="section-title">Мои миры</h2>
            <Badge>{saved.length}</Badge>
          </div>
          {saved.map((w) => (
            <ResumeWorld
              key={w.id}
              world={w}
              subtitle={state.saves[w.id]?.playerName ?? w.playerName}
              onClick={() => onWorld(w)}
            />
          ))}
        </section>
      )}
      <div className="filter-strip" aria-label="Жанры">
        {["Все", "Фэнтези", "Мистика", "Реализм", "Ситком"].map((x) => (
          <Chip key={x} selected={genre === x} onClick={() => setGenre(x)}>
            {x}
          </Chip>
        ))}
      </div>
      {filtered.length ? (
        <div className="world-grid">
          {filtered.map((w) => (
            <WorldCard
              key={w.id}
              world={w}
              selected={w.id === "gorpcore"}
              onClick={() => onWorld(w)}
            />
          ))}
        </div>
      ) : (
        <EmptyState
          title="Такого мира пока нет"
          action={
            <button
              className="link-button"
              onClick={() => {
                setQuery("");
                setGenre("Все");
              }}
            >
              Сбросить поиск
            </button>
          }
        >
          Попробуй другое название или убери фильтр.
        </EmptyState>
      )}
    </>
  );
}
