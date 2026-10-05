# Златоуст: игра «Реки»

Telegram Mini App 14+: ведёшь канал блогера в вымышленном мире, AI-звёзды и фанаты реагируют на каждый пост.

| Где | Что |
|---|---|
| [AGENTS.md](AGENTS.md) | Правила для агентов и список навыков |
| [docs/game-flow.md](docs/game-flow.md) | Флоу игры и правила: источник правды |
| [docs/backend-spec.md](docs/backend-spec.md) | Спека бэкенда: ход, оценка поста LLM, контент миров, бот, безопасность, антифрод, аналитика |
| [docs/config/balance.json](docs/config/balance.json) | Все числа баланса одним конфигом |
| [docs/analytics.md](docs/analytics.md) | Продуктовая аналитика: вопросы, события, воронки, метрики, хранение |
| [docs/dashboards.md](docs/dashboards.md) | Представления OpenObserve: конкурсные, продуктовые и технические метрики |
| [docs/deployment.md](docs/deployment.md) | CI/CD, production HTTPS и доступ к служебным сервисам через SSH |
| [docs/screens/](docs/screens/README.md) | Макеты всех экранов |
| [docs/audit.md](docs/audit.md) | Итоговый аудит игры и экранов, вердикт для вёрстки |
| [docs/design/](docs/design/README.md) | Исходники экранов (HTML → PNG) |
| [docs/balance-sim/](docs/balance-sim/README.md) | Симуляции баланса и их выводы |
| [.agents/skills/](.agents/skills) | Навыки агента: `rule-change`, `issue-spec`, `repo-consistency-check`, `screen-check`, `game-balance-sim` (таблица в AGENTS.md) |
| [frontend/](frontend/README.md) | Mini App: React + TypeScript + Vite, Docker |
| [Issues](https://github.com/Stepami/golden-mouth/issues) | Задачи со спецификацией в теле |

## CI/CD

- push в рабочую ветку запускает проверки только затронутой части проекта;
- pull request и push в `master` запускают проверку секретов во всех отслеживаемых файлах;
- тег семантической версии запускает полный CI, создаёт GitHub Release через GitReleaseManager и разворачивает версию на self-hosted runner.

Docker images собираются только во время deployment на VDS.

Подробная настройка VDS, HTTPS, release labels и SSH-туннелей описана в [docs/deployment.md](docs/deployment.md).

## Запуск всего проекта

Перед первым запуском включи Docker Desktop → **Settings → AI → Enable Docker Model Runner** и задай логин и пароль администратора OpenObserve. Эти же данные Collector использует только внутри Compose-сети для отправки OTLP. Затем из корня репозитория запусти Compose:

```powershell
$env:OPENOBSERVE_EMAIL = '<выбранный email администратора>'
$env:OPENOBSERVE_PASSWORD = '<случайный длинный пароль>'
docker compose up --build
```

Внешний LLM, `LLM_PAT` и Telegram bot token для локального запуска не нужны. `compose.override.yml` включает окружение `Development`, подключает отдельную runtime-конфигурацию фронтенда и эмулирует Telegram-пользователя с id `1001`. Фронтенд отправляет обычный заголовок `Authorization: TMiniApp <initData raw>`, а backend принимает специальный dev-вариант этих данных только в `Development`. В production dev-аутентификация не регистрируется и требуется подпись Telegram.

Тот же override объявляет модель `ai/qwen2.5:3B-Q4_K_M`: Compose сам скачивает её и передаёт backend совместимые с OpenAI endpoint и имя модели. Первая загрузка модели занимает около 1,8 ГБ; backend ждёт локальную генерацию до 120 секунд.

Compose соберёт и запустит пять сервисов и локальную AI-модель:

- фронтенд: `http://localhost:8080`;
- бэкенд: `http://localhost:8081` (`/health/live`, `/health/ready`, `/swagger/`);
- OpenObserve: `http://localhost:5080` (логи, метрики и трейсы);
- OpenTelemetry Collector во внутренней Compose-сети;
- MongoDB: `mongodb://127.0.0.1:27017/reki` для локального подключения через Compass; внутри Compose-сети — `mongo:27017`.

Локальные модели и их размер видны в Docker Desktop на вкладке **Models**; удалить модель можно там же или командой `docker model rm ai/qwen2.5:3B-Q4_K_M`. Если нужен внешний LLM вместо локального, запусти только `compose.yml` с отдельным незакоммиченным override, который передаёт `Llm__ApiKey`, `Llm__BaseUrl` и `Llm__Model`.

При первом запуске OpenObserve создаёт root-пользователя из `OPENOBSERVE_EMAIL` и `OPENOBSERVE_PASSWORD`; отдельный ingestion token для MVP не нужен.

Порты можно переопределить через `FRONTEND_PORT`, `BACKEND_PORT`, `MONGO_PORT` и `OPENOBSERVE_PORT`. MongoDB и OpenObserve публикуются только на loopback-интерфейсе и недоступны другим устройствам в сети. Для внешней MongoDB задай `MONGO_CONNECTION_STRING` вне репозитория.

Backend отправляет OTLP/gRPC во внутренний `otel-collector:4317`. Collector разделяет продуктовые события и технические логи по атрибуту `reki.stream`, после чего отправляет оба потока, метрики и трейсы в OpenObserve. Backend не зависит от доступности OpenObserve при запуске.

Фронтенд выбирает источник данных во время запуска через `frontend/public/runtime-config/config.js`. В обычном образе по умолчанию включён `mock`; чтобы перейти на реальный API, поменяй `apiMode` на `backend` и перезагрузи Mini App. Пересборка образа и перезапуск контейнера не нужны. Локальный Compose автоматически монтирует `config.development.js`, где уже выбраны backend и dev-пользователь `1001`. `apiBaseUrl` остаётся same-origin путём `/api/v1`, а реальный адрес backend скрыт за nginx-прокси.

```javascript
window.__REKI_CONFIG__ = Object.freeze({
  apiMode: 'backend',
  apiBaseUrl: '/api/v1',
  developmentTelegramUserId: 1001,
})
```

`developmentTelegramUserId` действует только вместе с backend в окружении `Development`. В production это поле не даёт доступа к API.

Данные MongoDB, её служебная конфигурация и данные OpenObserve хранятся в именованных volumes `reki_mongo-data`, `reki_mongo-config` и `reki_openobserve-data`.

Остановить проект без удаления данных:

```powershell
docker compose down
```

Репозиторий приватный, но токены, ключи, адреса серверов и реальные данные людей в Git не кладём.
