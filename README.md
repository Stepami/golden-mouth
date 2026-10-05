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

Один раз скачай основную 4-битную Gemma 4 E2B и её MTP draft, затем включи speculative decoding. Основная модель остаётся моделью API, а `e2b-mtp` предлагает ей несколько следующих токенов для параллельной проверки:

```powershell
docker model pull ai/gemma4:e2b-q4_K_M
docker model pull ai/gemma4:e2b-mtp
docker model configure --context-size 4096 --speculative-draft-model ai/gemma4:e2b-mtp --speculative-num-tokens 2 ai/gemma4:e2b-q4_K_M
```

```powershell
$env:OPENOBSERVE_EMAIL = '<выбранный email администратора>'
$env:OPENOBSERVE_PASSWORD = '<случайный длинный пароль>'
$env:TG_PAT = '123456:reki-test-bot-tg-token'
docker compose up --build
```

Внешний LLM, `LLM_PAT` и Telegram-бот для локального запуска не нужны. `compose.override.yml` передаёт backend основную `ai/gemma4:e2b-q4_K_M`, а выполненная выше команда атомарно задаёт ей контекст 4096 токенов и MTP draft. Модель намеренно не объявлена через Compose `models:`: model-provider повторно конфигурирует основную модель и сбрасывает speculative decoding. Backend внутри контейнера обращается к Docker Desktop через `http://model-runner.docker.internal/engines/v1/` и ждёт локальную генерацию до 120 секунд.

Локальный Vite dev server эмулирует Telegram Mini App, каждый раз создаёт свежий HMAC-подписанный `initData` для вымышленного игрока и отправляет запросы в backend. Backend проверяет подпись штатным `TgMiniAppAuth`; auth bypass нет. `TG_PAT` — единый вход Compose: Vite читает его при подписи, а Compose передаёт то же значение в настройку `TgMiniAppAuth` backend. Показанное значение фиктивное, не связано с Telegram и не даёт доступа ни к одному боту.

Compose соберёт и запустит пять сервисов и локальную AI-модель:

- фронтенд: `http://localhost:8080`;
- бэкенд: `http://localhost:8081` (`/health/live`, `/health/ready`, `/swagger/`);
- OpenObserve: `http://localhost:5080` (логи, метрики и трейсы);
- OpenTelemetry Collector во внутренней Compose-сети;
- MongoDB: `mongodb://127.0.0.1:27017/reki` для локального подключения через Compass; внутри Compose-сети — `mongo:27017`.

Локальные модели и их размер видны в Docker Desktop на вкладке **Models**. Удалить основную модель и MTP draft можно там же или командами `docker model rm ai/gemma4:e2b-q4_K_M` и `docker model rm ai/gemma4:e2b-mtp`. Если нужен внешний LLM вместо локального, запусти только `compose.yml` с отдельным незакоммиченным override, который передаёт `Llm__ApiKey`, `Llm__BaseUrl` и `Llm__Model`.

При первом запуске OpenObserve создаёт root-пользователя из `OPENOBSERVE_EMAIL` и `OPENOBSERVE_PASSWORD`; отдельный ingestion token для MVP не нужен.

Порты можно переопределить через `FRONTEND_PORT`, `BACKEND_PORT`, `MONGO_PORT` и `OPENOBSERVE_PORT`. MongoDB и OpenObserve публикуются только на loopback-интерфейсе и недоступны другим устройствам в сети. Для внешней MongoDB задай `MONGO_CONNECTION_STRING` вне репозитория.

Backend отправляет OTLP/gRPC во внутренний `otel-collector:4317`. Collector разделяет продуктовые события и технические логи по атрибуту `reki.stream`, после чего отправляет оба потока, метрики и трейсы в OpenObserve. Backend не зависит от доступности OpenObserve при запуске.

Фронтенд выбирает источник данных во время запуска через единственный `frontend/public/runtime-config/config.js`; сейчас он настроен на backend. `apiBaseUrl` остаётся same-origin путём `/api/v1`: Vite проксирует его в backend при разработке, а production nginx — после деплоя. Для автономной работы с моками достаточно временно поменять `apiMode` на `mock` в этом же файле.

```javascript
window.__REKI_CONFIG__ = Object.freeze({
  apiMode: 'backend',
  apiBaseUrl: '/api/v1',
})
```

Данные MongoDB, её служебная конфигурация и данные OpenObserve хранятся в именованных volumes `reki_mongo-data`, `reki_mongo-config` и `reki_openobserve-data`.

Остановить проект без удаления данных:

```powershell
docker compose down
```

Репозиторий приватный, но токены, ключи, адреса серверов и реальные данные людей в Git не кладём.
