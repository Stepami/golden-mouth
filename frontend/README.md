# frontend

Telegram Mini App игры «Реки». Правила для агента и людей: [AGENTS.md](AGENTS.md). Навыки агента: [apm.yml](apm.yml).

Источник данных задаётся в единственном `public/runtime-config/config.js`. `apiMode: 'mock'` использует локальные данные, `apiMode: 'backend'` направляет запросы на same-origin `apiBaseUrl`; сейчас файл настроен на backend. Local Compose запускает Vite dev server и проксирует `/api` в backend.

В local Compose Vite-плагин читает общий `TG_PAT`, даёт `@telegram-apps/sdk` вымышленного игрока и свежий HMAC-подписанный `initData`. Compose передаёт тот же `TG_PAT` в настройку `TgMiniAppAuth` backend; отключения auth для development нет. Production build и nginx не включают эмулятор.
