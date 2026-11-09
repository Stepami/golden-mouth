# PR 261: visual follow-up

Date: 2026-11-09. User review identifies three corrections to the existing frontend.

- World intro: cover followed by one story screen per non-empty paragraph, preserving the authored text and order. Progress segments and page count reflect every scene. New-player entry is available only on the last scene; remove the cover shortcut. Next/back navigate individual scenes, return-to-cover resets to the cover. Existing saved-game resume is unaffected. The controlled intro step continues to support the application/Telegram back action. Reset reading scroll and focus when changing scenes.
- Follow-up direction: each authored paragraph has a distinct atmospheric heading, stored as presentation-only `openingTitles` and passed through `World.introTitles`. Text fades out in 100ms and the next scene fades in over 180ms, without translation or scaling. Scenes share a grid cell to keep the footer position stable. Reduced-motion mode switches instantly. Headings do not rewrite the authored events or tasks.
- Skill art: the six discovery directions use six distinct existing atlas cells: courage/shield, attention/magnifier, persuasion/speech, communication/links, humour/mask, self-expression/quill. All named skills in the four authored scenarios get explicit mappings to appropriate icon families; related skills can share a family. Reuse the existing art style and assets.
- Feed header: retain the original single-row composition, world-name capsule, circular resources and avatar, including existing narrow/large-text layouts. Do not redesign or add resource rows/cards. Fix the energy ring SVG selector and fit resource contents within the existing controls: rotate/position the direct ring SVG, keep its nested lightning sprite upright and centred. At narrow widths, ring and crystal glyph both use 24px so count baselines match; control padding is 2px. Keep recovery caption and progress calculations unchanged.

Implementation scope: `frontend/src/v7/screens/WorldIntro.tsx`, `frontend/src/v7/world-entry.css`, `frontend/src/v7/ui/GameArt.tsx`, the energy-ring rules in `frontend/src/v7/night.css`; presentation titles in `frontend/src/v7/story/roleScenarios.json`, `frontend/src/v7/domain/types.ts` and the catalogue mapping in `frontend/src/v7/BackendApp.tsx`. Header markup, WalletView and ResourceBadge retain their previous implementation. No backend changes, new assets, packages or automated test files.

Large-text recovery caption is anchored at the left edge of its control to avoid clipping outside the viewport; normal-size alignment stays unchanged.

Validation: TypeScript and both builds; browser checks of all intro scenes and back/entry gating, distinct direction art, header geometry, narrow and large-text views. Independent EGM source review before push. Preserve the demo/real-API boundary from the earlier revision.

## Validation result

Both builds and 14 existing UI/data/draft checks passed; no new tests added. The existing large-bundle warning remains. Independent fresh-context review found missing cover focus (fixed) and documentation scope omissions (clarified). Final header markup, WalletView and ResourceBadge match the previous commit.

Chromium checks: one paragraph per story, entry only on the final scene, distinct headings, forward/back/cover navigation and cover focus. Frame sampling measured opacity progressing from 0 to 1 without transforms; the next button stayed at the same coordinates. All six direction icons use distinct atlas cells, and all 46 authored skill names have explicit mappings. At 320px all four original header controls have the same y-coordinate and 48px height, resource counts share a baseline, and the lightning has no transform. At 390px the original 52px controls align. Large-text view was checked separately. Local screenshots are in `frontend/output/playwright/pr261/` and are not committed.
