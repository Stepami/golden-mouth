# PR 261 corrections: validation

Date: 2026-11-09. Acceptance criteria: [PR-261-revisions.md](PR-261-revisions.md). Independent source review: [drift report](2026-11-09_PR-261_DRIFT_REPORT.md).

## Automated checks

- `npm run build` and `npm run design:build`: passed, including TypeScript. Existing large-bundle warnings remain.
- Existing interests, release-policy, submission, energy-exchange and persona tests: 21 passed.
- Existing release-ui, ui-data and world-drafts tests: 14 passed. The concurrent Vite test servers reported an occupied HMR port; all assertions passed.
- Production bundle inspection: the isolated preview's sample reply, demo banner and adapter-only error are absent.
- `git diff --check` and repository secret scan: passed before commit.
- No new test files, dependencies, generated client changes or backend changes.

## Browser checks

The isolated static design build was exercised in Chromium at 320 and 390 pixels. Its demo banner remained visible throughout.

- Humour interests rank the relevant world first; humour direction recommends Burger with Dry Punch and Self-irony. The world-specific list limits explicit selections to three. Zero selections can continue.
- All five authored roles are named and selectable in the wrapping roster at 320 pixels. Custom role goal opens as read-only text.
- “Show all worlds” clears the selected genre: one matching playable card becomes all five playable cards, alongside planned worlds.
- Isabella starts with the rescue response and dent context from the current scenario.
- Partial post: audience +24, one skill +10 percentage points, two unchanged, story 12.5%, and a task that can continue.
- Ordinary navigation post with no new progress: audience +24, all three skills unchanged, and updated task status on Goals.
- Comment with audience loss: −8 in both result and character profile.
- Successful post: the next task and 25% progress remain after a page reload.
- Switching worlds retains separate skill snapshots, task progress and last-turn audience deltas: Isabella +24/25%; Burger −8/12.5%.
- Energy ring advances with elapsed time and retains the count/recovery caption. Story ring reflects the current hero's progress.
- After changing worlds and reloading, global profile retains the first hero's name, stable generated handle and neutral person icon. Character profile exits to the catalogue.
- No horizontal page overflow in the checked narrow layouts. Static browser console: zero errors or warnings.

Local visual evidence is in `frontend/output/playwright/pr261/` (not included in Git): role roster, partial result, energy, profile and Isabella's opening.

## Limits

These checks validate the frontend and illustrative preview flows. They do not validate AI scoring, numeric balance, production campaign progression or cross-device account identity. The optional role skill/progress fields remain absent from the current real API; the interface displays unknown values there. No merge or deployment was performed.
