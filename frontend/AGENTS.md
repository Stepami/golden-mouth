# Frontend «Реки»

Текущий интерфейс v7: мобильная оболочка до430px, дизайн-система Романа с ночной итерацией v8 по запросу владельца продукта, Telegram Mini App и браузерный вход. Правила: `../docs/reviews/redesign-consolidation-2026-11-13.md`; API-границы: `../docs/reviews/roman-v7-integration.md`. Старые экраны390×844 — историческая реализация. Текущая ночная тема задана в src/v7/night.css; не смешивать её со старой тёмной темой.

## Стек
- React + TypeScript, сборка Vite
- @tma.js/sdk для initData, темы и кнопок Telegram
- Бэкенд в ../backend, общаемся по REST/JSON

## Запуск
- Локально: `npm ci && npm run dev`
- Контейнер: `docker build -t reki-frontend .` и `docker run -p 8080:80 reki-frontend` (Vite собирает статику, nginx раздаёт). В общий docker-compose фронт входит этим образом.
- Новые пакеты только `npm i <pkg>@<точная версия>`: .npmrc закрепляет версии и не берёт релизы новее даты в `before`, её сдвигаем вручную.

## Структура
- `src/screens/<группа>.tsx`: экраны, ключ = код макета (`01-3_world`). `?screen=<код>` открывает экран сразу, `?gallery` показывает все.
- `src/ui/`: компоненты из макетов (Card, Button, Chip, Gift, Avatar, SkillBar, Ribbon, Whisper, Comment, Sheet…).
- `src/api/generated/reki-api.ts`: закоммиченный NSwag Fetch-клиент и wire DTO. `src/api/types.ts`: производные модели экранов, `mock.ts`: данные галереи и fallback-состояний. `client.ts`: фасад настоящего API.
- `nswag.frontend.json`: независимый frontend-конфиг существующего NSwag 14.7.1; сборка API генерирует TS после OpenAPI. Фронтенд собирается из закоммиченного клиента без .NET. Обновление: изменить API → собрать API → закоммитить TS-клиент → проверить фронт. Generated-файлы вручную не редактировать.
- Runtime config содержит `apiBaseUrl` и feature flags. Mock/backend-переключения нет; ошибка API не должна создавать фиктивный успешный результат. SSE POST не повторять автоматически; сохранять отмену и восстановление по статусу.
- `scripts/screens.sh [коды]`: скриншоты 390×844 в `shots/` для сравнения с `docs/screens/`.


## Секреты
- Перед работой один раз: `git config core.hooksPath .github/hooks`. Хук `.github/scripts/check-secrets.sh` останавливает коммит с токенами, ключами, `.env` и личными путями.
- Во фронте секретов нет и быть не может: runtime config и всё из `import.meta.env` видно в браузере.

## Как работаем
- Задача = GitHub issue со спецификацией в теле (как в Stepami/hydrascript). Ветка `issue-<N>-кратко`, PR с `Closes #N`.
- Экран = компонент в `src/screens/<код экрана>`, коды как в макетах (01-3_world, 04-1_limit…).
- Навыки и MCP агента подключаются через APM: `apm install` в этой папке, манифест apm.yml.

## Нельзя
- Репозиторий приватный, но секреты в Git не храним. Никаких токенов, ключей, .env, адресов серверов, реальных имён и переписок.
- Ключи LLM только на бэкенде, фронт их не видит.
- Не ставить пакеты с версией младше 7 дней, версии закреплены lockfile'ом.
