# PR 261: reduce redundant captions

Date: 2026-11-09. User explicitly chose removing unnecessary/repeated captions throughout the application, rather than just making them visually quieter.

Review active v7 onboarding, catalogue, world intro, hero selection, feed, goals, character/global profiles, energy/crystals and publication/result surfaces. Remove obvious status captions and repeated headings/help that restate the action or nearby content. Keep meaningful units, actionable errors, consent/age information, the AI/demo disclosure, costs, recovery timing when energy is below capacity, and authored scene/task content. Preserve accessible names and the prior agreed flows. No scoring/API/balance changes. Do not dim necessary information below readable contrast.

Keep the newly requested single-row header, seamless name loop, left-aligned audience text and outer energy ring. Prior story/skill work remains. Use existing components and avoid adding new explanation buttons merely to replace removed prose. Validate representative screens, builds and existing applicable checks; no new automated test files.


Implemented in WalletView, ProfileHeader, Onboarding, HeroStudio, WorldBrief, WorldResources, Composer/BackendApp, StorySurfaces and PublicationCards. Removed the full-energy HUD caption and duplicate resource explanations, profile kicker, extra onboarding heading, generic direction hints, repeated skill-count heading, obvious draft/start status, streak filler and repeated XP/audience amounts. Real skill descriptions, units, costs, outcomes and accessibility labels remain. Full-energy exchange stays disabled with an explanatory label. An existing draft is still named by its action button.

Browser inspection covered interests, skill/hero selection, feed, resources, profile, composer and result details. The result displays its audience change once with a unit and all three skill changes. Necessary error/consent/disclosure branches are unchanged. Shared caption styles were not globally hidden or dimmed.
