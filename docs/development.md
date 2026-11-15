# Docker DEV: автоматический вход и настоящий API

Для агента, которому поручена проверка игры на Mini. Команды выполняются из корня собственного worktree в shell bash/zsh. Это инструкция, сверенная с исходниками master `52241df`, а не подтверждение работающего DEV. Она не разрешает занимать общий DEV, менять backend или production.

## Перед каждым запуском

1. Проверь `hostname`, `uname -m`, `pwd`, `git rev-parse HEAD`: это выделенный Mini и твой worktree, не Air и не чужой checkout. Сверь машину с назначением оркестратора.
2. Проверь `control.py status` из локального управляющего окружения и владельца своей issue/PR. При `desktop`, `handoff_pending` или неизвестном владельце остановись. Отдельно получи у оркестратора владение общим DEV и ресурсом сборки: владение issue не даёт владение DEV.
3. Проверь `df -h .`, `docker info`, `docker compose version`, `docker system df`, `docker compose ls` и `tailscale serve status`. Нужен Compose 2.30+ (`!override`, `configs.content`, `post_start`). Первый запуск скачивает модели и образы; запас диска и памяти согласуй с владельцем Mini. Не чисти чужие volumes/images/processes.
4. Зафиксируй собственное имя Compose-проекта, свободные порты и отдельную DEV-базу. Не подключай production MongoDB, production API или production volumes. Не используй имя `reki` по умолчанию: оно может принадлежать общему окружению.

Если Compose/Tailscale недоступны или ресурс занят, передай оркестратору точную блокировку; не переустанавливай окружение и не меняй общий DEV самостоятельно.

## Одноразовая настройка своего DEV

Получи согласованные DEV-секреты через локальное хранилище вне Git и загрузи в окружение процесса без вывода значений. Обязательные входы Compose: `TG_PAT`, `AUTH_SIGNING_KEY`, `AUTH_ENCRYPTION_KEY`, `LLM_PAT`, `LLM_BASE_URL`, `LLM_MODEL`, `OPENOBSERVE_EMAIL`, `OPENOBSERVE_PASSWORD`. Для auth нужны отдельный DEV RSA PEM и 32 случайных байта в Base64; формат описан в [deployment.md](deployment.md#ключи-и-настройки-входа-123). Не копируй production-ключи. Сохраняй DEV auth-ключи между запусками для восстановления сессии.

`TG_PAT` должен совпадать у Vite и backend: Compose передаёт его в оба контейнера. В `vite.config.ts` плагин `tma` создаёт свежий HMAC-подписанный `initData` вымышленного игрока для `@tma.js/sdk`. Backend проверяет подпись штатным `TgMiniAppAuth`; это не отключение validation. Не публикуй токен или `initData` в логах/отчёте. Файл `frontend/dev/telegram-emulator-state.json` хранит локальную идентичность эмулятора: не удаляй его между проверками сохранения прогресса и не добавляй в коммит.

Назначь значения под свой ресурс (примеры ниже не резервируют порты):

```bash
export COMPOSE_PROJECT_NAME=reki-dev-task
export FRONTEND_PORT=18080 BACKEND_PORT=18081 MONGO_PORT=27018 OPENOBSERVE_PORT=15080
export MONGO_CONNECTION_STRING='mongodb://mongo:27017/?replicaSet=rs0'
export MONGO_DATABASE_NAME=reki_dev_task
unset REKI_BROWSER_LOGIN
```

`REKI_BROWSER_LOGIN=1` отключает эмулятор для отдельной проверки браузерного OAuth; здесь он не нужен. В Compose этот флаг сейчас не передаётся, но проверь дополнительные локальные override и Vite `.env` без публикации их содержимого.

### Закрытый доступ с другого устройства

Используй согласованный маршрут Tailscale Serve только внутри tailnet, без Funnel. Сначала проверь существующие маршруты и владельца HTTPS-порта. Если маршрут уже занят, не заменяй его — верни вопрос оркестратору.

Для своего свободного HTTPS-порта 443 после согласования:

```bash
tailscale serve --bg --https=443 "http://127.0.0.1:${FRONTEND_PORT}"
tailscale serve status
```

Сохрани выданный HTTPS origin локально; `AUTH_ISSUER` задай равным выбранному DEV origin с завершающим `/`. На клиентском устройстве нужны Tailscale и разрешённый доступ к Mini. Реальные hostname и адреса в Git не сохраняй.

Для разрешения точного hostname Vite создай локальный Compose override **вне Git** и укажи его абсолютный путь в `DEV_ACCESS_OVERRIDE`. Пример содержимого (замени placeholder своим hostname без схемы и порта):

```yaml
services:
  frontend:
    environment:
      __VITE_ADDITIONAL_SERVER_ALLOWED_HOSTS: '<DEV_HOSTNAME>'
```

Этот файл добавляется третьим `-f` к командам ниже. Без удалённого доступа используй первые два файла и локальный `http://localhost:<FRONTEND_PORT>`; выбери соответствующий `AUTH_ISSUER`. Не разрешай все hosts. HTTPS завершается на Serve, Vite внутри остаётся HTTP; `REKI_HTTPS_CERT_PATH`/`REKI_HTTPS_KEY_PATH` для этого не нужны.

HMR должен подключаться по WebSocket через тот же HTTPS origin. В DevTools проверь соединение `wss` и обновление страницы после своей правки. Ошибка HMR отдельно от ошибки API: проверь proxy WebSocket и фактический порт Vite 5173. Если нужна настройка `server.hmr`/`server.ws`, передай её владельцу DEV; текущий `vite.config.ts` не читает произвольные `HMR_HOST`/`HMR_PORT` из environment.

Справка: [Tailscale Serve](https://tailscale.com/docs/reference/tailscale-cli/serve), [Vite hosts и WebSocket](https://vite.dev/config/server-options).

## Повторный запуск

Повтори предварительные проверки и загрузку того же DEV environment. Сохрани те же имя проекта, базу, auth-ключи и идентичность эмулятора.

```bash
docker compose -f compose.yml -f compose.override.yml -f "$DEV_ACCESS_OVERRIDE" config --quiet
docker compose -f compose.yml -f compose.override.yml -f "$DEV_ACCESS_OVERRIDE" up -d --build
docker compose -f compose.yml -f compose.override.yml -f "$DEV_ACCESS_OVERRIDE" ps
curl --fail "http://127.0.0.1:${BACKEND_PORT}/health/ready"
```

Не публикуй полный `docker compose config`: он раскрывает environment. Локально проверь итоговые настройки без сохранения сырых данных: frontend target `development`, порт контейнера 5173, backend `ASPNETCORE_ENVIRONMENT=Development`, MongoDB только своего проекта. Явные два `-f` обязательны: один `compose.yml` собирает nginx со статикой без эмулятора. Не добавляй `compose.production.yml`.

Первый старт Infinity может занять до 15 минут. После изменения lockfile существующий `frontend-node-modules` требует обновления зависимостей: той же Compose-командой выполни `exec frontend npm ci`, затем `restart frontend`. Не удаляй общие volumes.

`frontend/public/runtime-config/config.js` должен сохранять `apiBaseUrl: '/api/v1'`. Vite проксирует этот путь в `http://backend:8080` и убирает префикс. Не подставляй внешний production URL. Галерея, `?screen=...`, демо-карточки и старое описание `apiMode: mock` в README не подтверждают работу API: текущий frontend не имеет такого переключателя.

## Браузерный smoke и критерий результата

Открой корень DEV origin без `?gallery`/`?screen` в отдельном тестовом профиле браузера.

1. Автоматический вход: в Network есть `POST /api/v1/auth/external/tg/miniapp`. Для нового игрока `registration_required` переводит в онбординг; заверши создание канала, затем проверь успешный обмен токенов и `GET /api/v1/me`.
2. Выбери/создай мир: переход открывает мир игрока с данными backend. Эти действия задают контекст канала и поста.
3. Напиши и отправь пост: публикация должна вызвать настоящий API и получить завершённый ответ/оценку. Проверь успешный серверный результат и появление поста в ленте; отрисованный экран или один HTTP 200 без завершённого результата недостаточны. При ошибке не засчитывай локальные данные как успех и не повторяй POST вслепую.
4. Обнови страницу на том же origin. Восстановленная сессия и API должны вернуть тот же канал, мир, пост и прогресс. Ничего не очищай между публикацией и refresh.

В отчёте укажи SHA, этапы PASS/FAIL/не проверено, HTTP-статусы и наблюдаемый результат без токенов, личных текстов и сырых логов. Готовность инструкции и успешный живой smoke — разные результаты.

## Если входа нет

| Симптом | Что проверить |
|---|---|
| Обычный экран входа, нет Mini App запроса | Development target вместо nginx, отсутствие `REKI_BROWSER_LOGIN=1`, загрузка Vite-плагина, непустой `initData`. После явного выхода используй новый тестовый профиль/вкладку: `reki:logged-out` блокирует auto-login в текущей сессии. |
| 401 при Mini App обмене | Один и тот же `TG_PAT` у frontend/backend, свежее `initData`, часы Mini; перезагрузи страницу. Не отключай серверную проверку. |
| `registration_required` | Это новый игрок: закончи онбординг, проверь последующий обмен и `/me`. |
| 502, отказ proxy, backend unhealthy | `ps`, `/health/ready`, готовность Mongo/Search/Infinity, target proxy `http://backend:8080`. |
| Host blocked, HTTPS/HMR не подключается | Точный hostname в локальном override, правильный Serve origin/порт, доступ tailnet и WebSocket. |
| После refresh другой игрок/нет прогресса | Тот же origin/профиль, сохранённые refresh-токен, auth-ключи, state эмулятора, имя проекта и DEV Mongo volume. |

## Остановка

Останавливай только свой проект с теми же `-f` и `COMPOSE_PROJECT_NAME`: `docker compose ... down` (вместо `...` подставь список файлов из запуска). Без `-v`: данные сохраняются. Собственный маршрут Serve на 443 выключается `tailscale serve --https=443 off`; сначала повторно проверь, что он всё ещё твой. Не используй глобальный Serve reset, Docker prune и не останавливай чужие контейнеры. Освобождение DEV передай оркестратору.
