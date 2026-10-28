# Three remaining UI compositions — 28.10.2026

Frontend **1c32f4b** (implementation e2aaf63 plus accessible pause spacing). Current executable app.html→tokens/app-base/app/components CSS + catalog/app JS, hashes in source-manifest.json. Marketing reference.json was not used.

- **24 reference/app pairs**,320/390×light/dark: welcome, worlds collapsed/expanded, each of3 skill examples. Controlled API world data, **NOT integration**. Contracts: manual pause/resume via keyboard, live reduced motion,2→4→2→4 world disclosure and hidden-world selection, Back/Close/Escape, preserved draft/selection/focus, reload. Errors/overflow0.
- **Real DEV backend, no interception**: two actual QA worlds (both visible, no unnecessary disclosure), world switch; each skill from post and reply editors in4 width/theme combinations. Reply target and request ID unchanged; text/selection/focus and reopening after reload survive. No world/post/account mutation requests. Reply route itself is not persisted on reload; reopening the same real post restores its draft.
- Real welcome geometry after frontend restart:44×44 control, no copy overlap and no overflow in all4 combinations. Full real post/reply scenarios ran on e2aaf63; subsequent1c32f4b changes only welcome CSS, checked separately in real DEV.

Adaptations:44px hit target has reserved space; the prototype uses32px. Example adds “Все навыки” back action; closing always returns to the existing editor. Existing balance explanation is retained; prototype crystal/boost/XP fixtures are not copied. Saved worlds use authoritative title/person/subscribers/current marker; artwork only for reliable template matches, no invented story status. Zero/one/two worlds never need a hidden remainder; real QA had2, four-world expansion was controlled UI data.

Physical Telegram WebView unavailable; browser emulation is not a phone test. No new publication/reply, account deletion, backend deployment, migration, production or merge.
