// Original v7 handoff checks, run against the imported v7 modules.
import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync, existsSync } from "node:fs";
import { worlds } from "../src/v7/domain/worlds.ts";
const css = readFileSync(new URL("../src/v7/theme.css", import.meta.url), "utf8");
const color = (name: string) =>
  css.match(new RegExp(`--color-${name}:\\s*(#[0-9a-f]+)`, "i"))![1];
function luminance(hex: string) {
  let h = hex.slice(1);
  if (h.length === 3) h = [...h].map((c) => c + c).join("");
  const c = [0, 2, 4]
    .map((i) => parseInt(h.slice(i, i + 2), 16) / 255)
    .map((v) => (v <= 0.04045 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4));
  return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2];
}
const ratio = (a: string, b: string) => {
  const x = luminance(a),
    y = luminance(b);
  return (Math.max(x, y) + 0.05) / (Math.min(x, y) + 0.05);
};
test("normal text meets WCAG AA against every assigned surface", () => {
  for (const [text, surfaces] of [
    [
      "ink",
      ["paper", "surface", "world-surface", "accent", "progress", "crystal"],
    ],
    ["muted", ["paper", "surface", "world-surface", "accent"]],
    ["nav-muted", ["paper"]],
    ["error", ["paper"]],
    ["success", ["paper"]],
  ] as [string, string[]][])
    for (const bg of surfaces)
      assert.ok(
        ratio(color(text), color(bg)) >= 4.5,
        `${text}/${bg} ${ratio(color(text), color(bg))}`,
      );
  assert.ok(ratio(color("error"), "#fff0f2") >= 4.5);
  assert.ok(ratio(color("success"), "#eaf4eb") >= 4.5);
});
test("field boundaries and semantic dark icons meet 3:1", () => {
  for (const fg of ["control-border", "energy", "goal"])
    for (const bg of ["paper", "surface"])
      assert.ok(ratio(color(fg), color(bg)) >= 3, `${fg}/${bg}`);
});
test("all worlds and characters have existing local images", () => {
  for (const w of worlds)
    for (const path of [
      w.cover,
      w.hero,
      w.playerAvatar ?? "assets/s1-imgEllipse6.png",
      ...w.cast.map((p) => p.avatar),
    ]) {
      assert.ok(path, `Missing avatar in ${w.title}`);
      assert.ok(
        existsSync(new URL(`../public/${path}`, import.meta.url)),
        path,
      );
    }
});
