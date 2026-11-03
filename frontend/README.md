# frontend

Telegram Mini App игры «Реки». Правила для агента и людей: [AGENTS.md](AGENTS.md). Навыки агента: [apm.yml](apm.yml).

В `public/runtime-config/config.js` задаются same-origin `apiBaseUrl` и feature flags. Приложение использует настоящий API; ошибки не подменяются локальным успехом. Local Compose запускает Vite dev server и проксирует `/api` в backend.

В local Compose Vite-плагин читает общий `TG_PAT`, даёт `@tma.js/sdk` вымышленного игрока и свежий HMAC-подписанный `initData`. Compose передаёт тот же `TG_PAT` в настройку `TgMiniAppAuth` backend; отключения auth для development нет. Production build и nginx не включают эмулятор.

Для локальной проверки браузерного Telegram Login JS через HTTPS укажи пути к доверенному сертификату и ключу PEM для `localhost`, а также публичный Telegram Client ID в локальном `public/runtime-config/config.js` (не коммить локальное значение). Зарегистрируй `https://localhost:5173` в Allowed URLs BotFather. Запусти backend отдельно, затем из `frontend/`:

```powershell
$env:REKI_HTTPS_CERT_PATH = '<path-to-localhost-cert.pem>'
$env:REKI_HTTPS_KEY_PATH = '<path-to-localhost-key.pem>'
$env:REKI_BROWSER_LOGIN = '1'
npm run dev
```

Открой `https://localhost:5173`. `REKI_BROWSER_LOGIN=1` отключает только DEV-эмулятор для проверки popup; без него подписанный Mini App сценарий работает как прежде. `/api/v1` остаётся same-origin proxy в backend.

## Демо-функции (#92)

`features` в runtime config: `dm`, `friends`, `collection` (по умолчанию false). Индексы вкладок остаются постоянными при скрытии. Флаг mockFeed и генератор демо-ленты удалены.

Содержимое миров находится в `src/api/worldContent.ts`. IT-персонажи — вымышленные адаптации, тексты оригинальные. Лента получает только сохранённые посты игрока и AI-звёзд текущего мира из GET /feed; без backend она пустая. Лента загружается одним GET /feed; каждый пост открывает тред. Детали содержат authorId: под постом звезды можно начать корневой комментарий автору, под своим — ответить на реплику звезды. Флагов разрешений нет.

Шаг `01-4b_world_setup` предшествует выбору звезды. После POST /onboarding вызывается PATCH /settings. При ошибке настроек повтор не создаёт игрока заново. Для макетов можно использовать `?screen=01-4_channel&world=it`. Кэш экранов сохраняет уже загруженные данные при возврате. Приглашения и шэринг получают персональный код через GET /referral, кэш разделён по PlayerId. Telegram start_param сохраняется в sessionStorage до успешного POST /onboarding (referralCode); при недоступном storage есть fallback в памяти. Первый источник сохраняется, после создания код очищается. Кнопки отправки и копирования заблокированы до загрузки ссылки; при ошибке доступен повтор.

Проверка #92: сборка TypeScript/Vite; браузерный проход Сцены, Академии и IT; сохранение голоса/драмы; повтор settings после ошибки без дублирования onboarding; серверная лента без демо-постов и локальных демо-комментариев; пустая лента без backend; справка навыков; положительный/отрицательный итог и публикация через API. Если числовые причины ответа API не суммируются в итог, карточка показывает их текст без ошибочного разложения прироста.

Шаблоны миров, их правила и каталог звёзд приходят из `GET /worlds/templates`; данные мира игрока — из `GET /worlds/{worldId}`.

### DEV: компактные настройки и первые ходы

Расширенный редактор показывает один экран с раскрываемыми разделами; все поля существующего payload сохраняются. Роли объясняются как занятия персонажа, коллекция — как оформление достижений, голос — как стиль подсказок. Возрастное пояснение использует сохранённую возрастную группу; серверные ограничения не меняются.

Первый пост получает отдельную завязку и действие. В редакторе поста идеи и задания свёрнуты. Промежуточные просмотры заменены статусом реакции, итог показывает прирост и одну причину, полный разбор доступен отдельно. Длинные записи и комментарии раскрываются кнопкой. Подсказка первого ответа закрывается пользователем и запоминается на устройстве для его профиля; переход в ленту доступен отдельно.

### Telegram SDK и зависимости (#168)

Прямой пакет `@tma.js/sdk` закреплён на стабильной версии 3.3.0; React-обёртка не нужна, поскольку интерфейс использует методы SDK без React-хуков. Приложение и DEV-плагин импортируют один и тот же SDK, чтобы `mockTelegramEnv`, `initData` и события кнопок разделяли состояние. В production эмулятор отсутствует.

При миграции учтены `miniApp.setBgColor`, `viewport.expand` и `miniApp.close`. Возвращаемое значение `.ifAvailable()` теперь объект; используемые вызовы не разбирают прежний tuple. Сырые подписанные `initData` продолжают передаваться в API без преобразования ключей. Руководства: [SDK](https://docs.telegram-mini-apps.com/packages/legacy/tma-js-sdk/migrate-from-telegram-apps), [bridge](https://docs.telegram-mini-apps.com/packages/legacy/tma-js-bridge/migrate-from-telegram-apps).

Срез `.npmrc before=2026-10-13` выбран 20.10.2026 с запасом не менее семи дней. Lockfile использует исправленные `valibot@1.5.0` (SDK) и `source-map-js@1.2.2` (PostCSS/Vite). Для проверки: `npm ci`, `npm audit`, `npx tsc -b`, `npm run build`, `node --test tests/*.test.mjs`. После изменения lockfile в DEV нужно выполнить `npm ci` также в контейнере с существующим томом node_modules и перезапустить frontend: пересборка образа не обновляет этот том.

## API-клиент NSwag (#122)

`nswag.frontend.json` использует существующий NSwag 14.7.1. При сборке API target `GenerateFrontendApiClient` читает build-time OpenAPI и создаёт `src/api/generated/reki-api.ts`: Fetch-клиенты, DTO-интерфейсы, строковые даты и AbortSignal. TypeScript хранится в Git; сборки фронта и Docker не требуют .NET.

Обновление: **изменить API → собрать API → закоммитить TS-клиент → проверить фронт**.

```sh
cd backend
dotnet tool restore
dotnet build src/Reki.Api/Reki.Api.csproj --configuration Release
cd ../frontend
npx tsc -b
npm run build
```

Фасад `api`, `useApi`, кэш и модели экранов сохраняются. Fetch-адаптер добавляет актуальную Telegram-авторизацию, сохраняет ApiError.failure и keepalive телеметрии. SSE читается постепенно отдельным адаптером, поскольку Fetch-шаблон NSwag возвращает Blob; payload использует generated DTO. При обрыве POST автоматически не повторяется: публикация и ответ восстанавливаются по сохранённым данным. Смена сессии инвалидирует отложенные ответы.
