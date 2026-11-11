// Original v7 handoff checks, run against the imported v7 modules.
import test from "node:test";
import assert from "node:assert/strict";
import {
  initialState,
  enterWorld,
  applyPublication,
  simulateResponse,
  validatePost,
} from "../src/v7/domain/game.ts";
import { worlds } from "../src/v7/domain/worlds.ts";
import type { PublishCommand } from "../src/v7/domain/types.ts";
const world = worlds[2];
const command: PublishCommand = {
  id: "test-post",
  worldId: world.id,
  text: "Давайте обсудим условия каждого.",
  approach: "ask",
  questId: "g1",
};
test("empty and excessive posts are blocked", () => {
  assert.ok(validatePost("  \n"));
  assert.ok(validatePost("x".repeat(2001)));
  assert.equal(validatePost("Привет"), undefined);
});
test("a result is applied once, including energy, XP and followers", () => {
  const start = enterWorld(initialState, world);
  const result = simulateResponse(command, world);
  const first = applyPublication(start, command, result);
  assert.equal(first.energy, 9);
  assert.deepEqual(first.saves[world.id]!.skills, [1, 2, 0]);
  assert.equal(first.saves[world.id]!.posts.length, 5);
  assert.deepEqual(applyPublication(first, command, result), first);
});
test("late response keeps correct world even if player has left", () => {
  const start = enterWorld(initialState, world);
  const exited = { ...start, activeWorldId: null };
  const next = applyPublication(
    exited,
    command,
    simulateResponse(command, world),
  );
  assert.equal(next.activeWorldId, null);
  assert.equal(next.saves[world.id]!.posts[0].id, command.id);
});
test("different worlds have independent posts, drafts and skills", () => {
  let state = enterWorld(initialState, world);
  state = applyPublication(state, command, simulateResponse(command, world));
  state = enterWorld(state, worlds[0]);
  assert.equal(state.saves.hogwarts!.posts.length, 1);
  assert.deepEqual(state.saves.hogwarts!.skills, [0, 0, 0]);
  assert.equal(state.saves.gorpcore!.completedQuests.length, 1);
});
test("repeated quest does not duplicate completion or grant repeat quest XP", () => {
  const state = applyPublication(
    enterWorld(initialState, world),
    command,
    simulateResponse(command, world),
  );
  const repeat = { ...command, id: "new-id" };
  const next = applyPublication(state, repeat, simulateResponse(repeat, world));
  assert.deepEqual(next.saves.gorpcore!.completedQuests, ["g1"]);
  assert.deepEqual(next.saves.gorpcore!.skills, state.saves.gorpcore!.skills);
});
test("NPC replies target player and each other, preserve independent boundaries", () => {
  const result = simulateResponse(command, world);
  assert.equal(result.posts[1].parentId, command.id);
  assert.equal(result.posts[3].parentId, result.posts[2].id);
  assert.match(result.relations.max, /не обещает|Хочет/);
  assert.ok(result.posts.some((p) => p.text.includes("не обещаю")));
});
test("a reply links to its parent, while a quote remains a public root post", () => {
  const reply = simulateResponse({ ...command, parentId: "seed" }, world);
  assert.equal(reply.posts[0].parentId, "seed");
  const quote = simulateResponse({ ...command, quoteId: "seed" }, world);
  assert.equal(quote.posts[0].quoteId, "seed");
  assert.equal(quote.posts[0].parentId, undefined);
});
test("all four demo days have three distinct quests and exactly three NPCs", () => {
  for (const w of worlds) {
    assert.equal(w.quests.length, 3);
    assert.equal(new Set(w.quests.map((q) => q.id)).size, 3);
    assert.equal(w.cast.length, 3);
    assert.ok(w.goal);
  }
});
test("mismatched response cannot update state", () => {
  const start = enterWorld(initialState, world);
  assert.equal(
    applyPublication(start, command, {
      ...simulateResponse(command, world),
      commandId: "other",
    }),
    start,
  );
});
