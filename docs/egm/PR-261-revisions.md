# PR 261: interface review corrections

Date: 2026-11-09. Scope: the existing frontend handoff and its isolated design preview. Source: the nine points in GitHub comment 6736808155, requested by the user. Server scoring, numeric balance and deployment are outside this PR.

1. Selected interests rank the full world catalogue; show-all clears search and genre filters.
2. Six communication directions and up to three specific skills recommend a role. Specific choices come only from that world’s approved role skills. A role always keeps its own three skills.
3. All available roles have named, wrapping selection buttons, including the custom role. No limit of three portraits.
4. Goals are fixed for every role. Initial task and context use the current short-cycle scenarios, including Isabella’s response after the rescue. Remove implementation notices from player task text.
5. A saved turn displays audience change, all three role skills and task outcome separately. Partial progress and positive audience with zero skill growth are distinct examples. Missing server skill data is not converted to zero or inferred from brave/humor/topic.
6. Character profile displays the last saved turn’s audience delta for that world only, including negative and zero deltas.
7. Energy has a bright ring around the lightning, filling from the supplied next-unit timestamp over the ten-minute recovery interval. Keep the count and recovery label; full and unknown timers are distinct.
8. A role campaign has a progress ring on the feed avatar and a percentage on Goals. Use supplied progress, not publication counts. The isolated preview supplies illustrative progress; missing server progress is shown as unknown.
9. Global profile has a neutral person icon, stable first-world name and a stable unique account handle. Character profile offers exit instead of account settings. Exiting clears world navigation history.

Content sources: docs/redesign/short-cycle/{HOGWARTS,EVENING,HOUSES,INFOBIZ}.md. Keep legacy campaign experiments separate. New presentation data must not relabel legacy numeric scales as approved role skills. Preview examples stay inside the design entry point and retain its visible demo banner.

Validation: existing relevant checks, TypeScript, both builds, secret scan, browser pass at 390 and 320 pixels, reload/world-switch persistence, independent source/spec drift review. Do not add automated test files; repository instructions prohibit unrequested new tests.

## Implementation boundary

The role catalogue is frontend/src/v7/story/roleScenarios.json; roles.ts selects the authored fixed skill trio and constructs new world definitions without old scene-specific NPC text. Legacy saved worlds retain their scenario. Design preview controls explicit outcome examples; no text scoring is implemented. The optional UI fields roleSkills/campaignProgress/lastTurn are populated only by this adapter. The normal API currently leaves them absent, so named skill values and campaign percent show unknown. The global display handle namespaces the full server-generated random unique account ID, and first-world name is stored locally. Existing accounts use the first returned owned-world entry on initial setup; cross-device first-world chronology requires server support.
