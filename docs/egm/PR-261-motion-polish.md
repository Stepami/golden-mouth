# PR 261: complete icons and restrained interaction feedback

Date: 2026-11-09. Additional explicit user request: recheck cropped icons, animate all energy/crystal illustrations, and add immersive but restrained micro-interactions throughout the application.

Preserve the approved layouts, story fades, seamless world title, captions cleanup, content and behaviour. Correct the shared atlas viewport so complete silhouettes remain visible, including during motion. Inspect all 16 illustrations at small and large sizes.

Energy and crystals share subtle, slow motion wherever their artwork is used. Keep numerical values, text, progress rings and hit targets still. Add quick pointer press/release feedback to shared navigation, selectors, rows and cards; animate disclosure chevrons and selection marks. Existing dialog, story, result and social feedback remain coherent. Do not add sound, confetti, screen-wide movement, delays before actions, or dependencies.

Use transform/opacity for motion, preserve keyboard focus and disabled behaviour, respect reduced motion, and avoid sticky touch hover effects. Validate full silhouettes, motion extrema, common interaction states, small/large text and existing checks. This is presentation only; no API, balance or persistence changes.


Implemented with per-illustration source regions in GameArt and shared ui/motion.css imported by both entrypoints. Opaque silhouettes in the 1254px atlas have at least 11px of space on every side (alpha >=128); the gallery visually confirms all 16 shapes. The source PNG is unchanged. Energy uses a brief tilt in a 7.2s cycle; crystal moves gently over 8.4s. Pointer feedback lasts 80–180ms. The empty-energy signal now affects only the inner icon, twice for 320ms, rather than scaling the entire control. No new dependencies or test files.

Browser checks: resource illustrations animate in the HUD, balance and exchange views. At the sampled motion extreme the 320px HUD remains unclipped. Holding energy preserves its 48px control bounds and the counter transform stays none; only the icon scales to .92. Reduced motion leaves zero running HUD/illustration animations. Keyboard input bypasses added pointer transitions. Both builds and 14 existing tests pass. Independent source review has no open findings.
