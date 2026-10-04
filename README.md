# Златоуст: игра «Реки»

Telegram Mini App 14+: ведёшь канал блогера в вымышленном мире, AI-звёзды и фанаты реагируют на каждый пост.

| Где | Что |
|---|---|
| [AGENTS.md](AGENTS.md) | Правила для агентов и список навыков |
| [docs/game-flow.md](docs/game-flow.md) | Флоу игры и правила: источник правды |
| [docs/backend-spec.md](docs/backend-spec.md) | Спека бэкенда: ход, оценка поста LLM, контент миров, бот, безопасность, антифрод, аналитика |
| [docs/config/balance.json](docs/config/balance.json) | Все числа баланса одним конфигом |
| [docs/analytics.md](docs/analytics.md) | Продуктовая аналитика: вопросы, события, воронки, метрики, хранение |
| [docs/dashboards.md](docs/dashboards.md) | Шесть дашбордов Metabase: графики, как считать, какие решения принимать |
| [docs/screens/](docs/screens/README.md) | Макеты всех экранов |
| [docs/audit.md](docs/audit.md) | Итоговый аудит игры и экранов, вердикт для вёрстки |
| [docs/design/](docs/design/README.md) | Исходники экранов (HTML → PNG) |
| [docs/balance-sim/](docs/balance-sim/README.md) | Симуляции баланса и их выводы |
| [.agents/skills/](.agents/skills) | Навыки агента: `rule-change`, `issue-spec`, `repo-consistency-check`, `screen-check`, `game-balance-sim` (таблица в AGENTS.md) |
| [frontend/](frontend/README.md) | Mini App: React + TypeScript + Vite, Docker |
| [Issues](https://github.com/Stepami/golden-mouth/issues) | Задачи со спецификацией в теле |

## Запуск всего проекта

Из корня репозитория:

```powershell
docker compose up
```

Compose соберёт и запустит пять сервисов:

- фронтенд: `http://localhost:8080`;
- бэкенд: `http://localhost:8081` (`/health/live`, `/health/ready`, `/swagger/`);
- Metabase: `http://localhost:3000`;
- Aspire Dashboard: `http://localhost:18888` (метрики, трейсы и логи бэкенда);
- MongoDB: `mongodb://127.0.0.1:27017/reki` для локального подключения через Compass; внутри Compose-сети — `mongo:27017`.

При первом запуске Metabase создай учётную запись команды и добавь MongoDB с host `mongo`, port `27017` и database `reki`. Настройки Metabase и данные MongoDB сохраняются в отдельных Docker volumes.

Порты можно переопределить через `FRONTEND_PORT`, `BACKEND_PORT`, `MONGO_PORT`, `METABASE_PORT` и `ASPIRE_DASHBOARD_PORT`. MongoDB и Aspire Dashboard публикуются только на loopback-интерфейсе и недоступны другим устройствам в сети. Для внешней MongoDB задай `MONGO_CONNECTION_STRING` вне репозитория.

Compose включает типизированную секцию `OpenTelemetry` через `OpenTelemetry__Enabled=true` и направляет OTLP/gRPC-телеметрию во внутренний endpoint `aspire-dashboard:18889`. Backend не зависит от доступности Dashboard при запуске. Dashboard работает в anonymous mode только для локальной разработки; не публикуй его порт во внешнюю сеть.

Фронтенд выбирает источник данных во время запуска через `frontend/public/runtime-config/config.js`. По умолчанию включён `mock`; чтобы перейти на реальный API, поменяй `apiMode` на `backend` и перезагрузи Mini App. Пересборка образа и перезапуск контейнера не нужны. `apiBaseUrl` остаётся same-origin путём `/api/v1`, а реальный адрес backend скрыт за nginx-прокси.

```javascript
window.__REKI_CONFIG__ = Object.freeze({
  apiMode: 'backend',
  apiBaseUrl: '/api/v1',
})
```

Данные MongoDB, её служебная конфигурация и состояние Metabase хранятся в именованных volumes `reki_mongo-data`, `reki_mongo-config` и `reki_metabase-data`.

Остановить проект без удаления данных:

```powershell
docker compose down
```

Репозиторий публичный: токены, ключи, адреса серверов и реальные данные людей сюда не кладём.
