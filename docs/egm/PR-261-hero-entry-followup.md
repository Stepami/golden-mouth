# Hero entry follow-up to merged PR 261

Date: 2026-11-09. Source: the two changes in GitHub comment 6740217723, requested by the user. Base: issue-205-v7-release at 4d9d0fc, which includes PR 261. Deliver a new PR to that branch; do not append commits to the already merged PR or merge the release chain.

1. In Evening, keep the cover and “Ладонь на вмятине”. The next action opens the selected hero card directly; “У каждого своя тайна” is no longer an intro page. The existing full opening remains available to scenario/task context. Move each role's personal stake into a short motivation displayed with the selected hero, including the custom role. Lida's clip is the main story of her town's public channel. The other worlds retain their existing skill-selection-first entry flow.
2. Predefined heroes have a fixed portrait, goal and three skills, with a direct “Начать историю” action. Only “Свой персонаж” has the look picker, upload, no-photo and address controls. Returning from a custom look, uploaded photo or no-photo state to a predefined hero restores that hero's portrait, including the persisted portrait after starting the world. A late custom photo load must not replace a predefined hero. Use existing portrait assets; Lida uses the existing spark portrait consistently by role and name.

Implementation: presentation flags and motivations in roleScenarios.json, catalogue/hero entry and persona saving in BackendApp.tsx, HeroStudio/HeroPicker, and portraits.ts. The direct Evening entry intentionally starts on the hero panel without showing a misleading “step 2 of 2” tracker. Back returns to the last intro page. Existing story text/skills, scoring, API DTOs and stored playthroughs are not rewritten.

Validation: both builds, existing applicable checks, browser flow at 390/320px, all five Evening motivations, canonical → custom appearance/upload/no-photo → canonical, world creation and reload, and independent source review. No new automated test files. Keep prior header, caption and motion work.

## Validation result

- `npm run build` and `npm run design:build` passed, including TypeScript. Existing large-bundle warnings remain.
- Existing release-ui, ui-data and world-drafts checks: 14 passed. Existing persona checks: 2 passed. Test runners emitted the existing occupied HMR-port and local-storage warnings.
- Chromium: Evening has only cover + “Ладонь на вмятине”; the next button goes straight to the hero card. Back returns to that intro page. All five roles display their own motivation and exactly three skills.
- Only the custom role displays appearance controls. Lida's portrait is restored after a custom look, an uploaded image, no-photo and a delayed upload callback arriving after switching back to Lida. Her portrait, goal, three skills and first task remain correct after starting and reloading the world.
- At 390px and 320px there is no horizontal page or app overflow. Local screenshots are under frontend/output/playwright/pr261/hero-followup-lida-*.png and are not committed.
- Infobiz still has two distinct intro paragraphs followed by the skill-selection step. Canonical/custom appearance visibility is also correct there.
- Browser console: no errors or warnings. Separate fresh-context Goldfish source review: no drift detected. This validates the frontend and isolated demo flow, not a live backend deployment.
