# PR 261: requested header refinement

Date: 2026-11-09. This later explicit user request supersedes the capsule/ring constraints in PR-261-visual-followup.md. Keep the original single-row structure.

- Remove the world-name capsule and the blurred audience icon. Align name and audience to the same left edge, retaining the down chevron and existing world-selection action.
- If the name exceeds its available width, scroll it continuously in a seamless loop with fades only at the viewport edges. A duplicate copy follows with a 16px track gap; the second copy is hidden from accessibility. Never fade out/reload the whole name at the loop boundary. Measure actual overflow on size/font/title changes; short names remain still. Keep the full name accessible and available as a tooltip. Hover/focus pauses movement without hiding text; reduced-motion users get one static wrapping copy.
- Energy recovery uses the outside circumference of its circular control, matching the avatar's conic progress ring. No green circle around only the lightning. Keep the energy value, formula and click action. The later caption-reduction request removes the full-state caption; below capacity, show the useful countdown.
- Centre all header controls relative to one another. Energy, crystals and avatar retain equal normal-size outer diameters; align resource count baselines. Preserve existing large-text adaptation without clipping.

Implementation: `frontend/src/v7/ui/WorldName.tsx`, `frontend/src/v7/BackendApp.tsx`, `frontend/src/v7/energy/WalletView.tsx`, `frontend/src/v7/night.css`. This is presentation work; no API, balance, scenario or persistence changes. Validate both builds and browser geometry/animation at 320 and 390 pixels, short/long names and reduced motion. No new automated test files.


Validation: at 320px all four controls have the same y and 48px height; at 390px resources/avatar are 52px. Resource numbers share a baseline. A sampled loop boundary differs by only 0.024px for 1ms of motion; opacity stays 1. A fitting name has no animation, and reduced motion shows one wrapping copy with no animation or horizontal overflow. The 200% text layout retains circular energy geometry. Hover pause is restricted to fine pointers.
