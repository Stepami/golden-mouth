# frontend

Telegram Mini App игры «Реки». Правила для агента и людей: [AGENTS.md](AGENTS.md). Навыки агента: [apm.yml](apm.yml).

Источник данных задаётся в `public/runtime-config/config.js`. `apiMode: 'mock'` использует локальные данные, `apiMode: 'backend'` направляет запросы на same-origin `apiBaseUrl`. В Compose каталог runtime-конфигурации примонтирован в nginx read-only, поэтому для переключения достаточно изменить файл и перезагрузить Mini App — пересборка и перезапуск контейнера не требуются.
