# Frontend «Реки»

Telegram Mini App: веб-страница, которая открывается внутри Telegram. Мобильная вёрстка 390×844, тёмная тема.

## Стек
- React + TypeScript, сборка Vite
- @telegram-apps/sdk для initData, темы и кнопок Telegram
- Бэкенд в ../backend, общаемся по REST/JSON

## Запуск
- Локально: `npm ci && npm run dev`
- Контейнер: `docker build -t reki-frontend .` и `docker run -p 8080:80 reki-frontend` (Vite собирает статику, nginx раздаёт). В общий docker-compose фронт входит этим образом.
- Новые пакеты только `npm i <pkg>@<точная версия>`: .npmrc закрепляет версии и не берёт релизы новее даты в `before`, её сдвигаем вручную.

## Структура
- `src/screens/<группа>.tsx`: экраны, ключ = код макета (`01-3_world`). `?screen=<код>` открывает экран сразу, `?gallery` показывает все.
- `src/ui/`: компоненты из макетов (Card, Button, Chip, Gift, Avatar, SkillBar, Ribbon, Whisper, Comment, Sheet…).
- `src/api/types.ts`: контракт с бэкендом (#10), `mock.ts`: тестовые данные из макетов, `client.ts`: без `VITE_API_URL` работает на mock.
- `scripts/screens.sh [коды]`: скриншоты 390×844 в `shots/` для сравнения с `docs/screens/`.

## Секреты
- Перед работой один раз: `git config core.hooksPath .github/hooks`. Хук `.github/scripts/check-secrets.sh` останавливает коммит с токенами, ключами, `.env` и личными путями.
- Во фронте секретов нет и быть не может: всё из `import.meta.env` видно в браузере.

## Как работаем
- Задача = GitHub issue со спецификацией в теле (как в Stepami/hydrascript). Ветка `issue-<N>-кратко`, PR с `Closes #N`.
- Экран = компонент в `src/screens/<код экрана>`, коды как в макетах (01-3_world, 04-1_limit…).
- Навыки и MCP агента подключаются через APM: `apm install` в этой папке, манифест apm.yml.

## Нельзя
- Репозиторий ПУБЛИЧНЫЙ. Никаких токенов, ключей, .env, адресов серверов, реальных имён и переписок.
- Ключи LLM только на бэкенде, фронт их не видит.
- Не ставить пакеты с версией младше 7 дней, версии закреплены lockfile'ом.
