# Продуктовая аналитика

03.10.2026. Зачем: понять, как играют, где бросают и что тянет вернуться, чтобы решать по данным, а не по симуляции. Числа баланса крутим в [config/balance.json](config/balance.json), эксперименты включаем флагами. Журнал ошибок отдельно: `POST /logs` ([backend-spec.md](backend-spec.md), раздел 9).

## Вопросы, на которые отвечаем

| Вопрос | Как смотрим |
|---|---|
| Где бросают онбординг? | воронка `onboarding_step`: age → world → channel → done (звезда) → первый `post_publish` |
| Доходят ли до первого поста и итога за первую минуту? | время от `session_start` до первого `post_result` |
| Затягивает ли ход? Сколько постов за сессию, на каком бросают? | `post_publish` по номеру поста за день; последний экран перед `app_hidden` |
| Пишут ли сами или берут «Подскажи»? Когда переходят на своё? | доля `own_words` по номеру поста и дню игры |
| Что делают на лимите? | после `limit_reached`: `invite_sent` / `plus_interest` / лента / уход |
| Возвращаются ли? Что держит? | D1, D3, D7 по когортам дня старта; что было в последней сессии у вернувшихся и у ушедших (нить, залёт, хейт, пустой день) |
| Работают ли друзья? | K: приглашённых, сделавших первый пост, на одного игрока; заруба и дуэль у пар против одиночек |
| Какой мир выбирают и где лучше удержание? | `onboarding_step: world` и D7 по миру |
| Не давит ли хейт? | D1 после дня с хейт-волной против без; переключения градуса драмы |
| Сколько хотят Реки+ и какой оффер лучше? | `plus_interest` на тех, кто увидел 04-2, по флагу `plus_offer` (10 / 20) |
| Какие экраны непонятны? | время на экране (`screen_leave.ms`) и возвраты «назад» сразу после входа |

## Что шлёт фронт

`POST /events`, пачкой раз в 10 секунд и при закрытии через `fetch` с `keepalive: true` (`frontend/src/app/analytics.ts`). Запрос проходит обычную TMA-аутентификацию:

```json
{ "session": "uuid", "platform": "ios|android|tdesktop|web",
  "experiments": { "plus_offer": "10" },
  "events": [{ "name": "screen_view", "t": 1759500000000, "screen": "02-3_result", "prev": "02-2_live", "props": {} }] }
```

`player` не принимается от клиента как доказательство личности. Бэкенд получает Telegram `user.id` только из подписанного `initData`, находит соответствующего игрока и при записи сам добавляет его обезличенный внутренний id. Нет валидного Telegram `user` — событие не принимается. Telegram ID и `initData` в аналитику не записываются.

| Событие | Когда | Свойства |
|---|---|---|
| `session_start`, `app_hidden`, `app_visible` | открытие, сворачивание, возврат | platform, ms_on_screen |
| `screen_view`, `screen_leave` | смена экрана | screen, prev, ms (время на экране) |
| `tap` | вкладка, главная / вторая кнопка экрана | id (`tab`, `cta_main`, `cta_second`), to |
| `onboarding_step` | шаги онбординга | step (age / world / channel / done), ok, world, address, star |
| `post_publish`, `post_result` | пост и его итог | len, own_words; delta, viral, flop |
| `limit_reached` | упёрся в лимит | |
| `plus_interest`, `plus_bonus_only` | 04-2 | |
| `invite_sent`, `share` | позвать, поделиться | to (chat / story) |
| `event_answered`, `challenge_post` | событие дня, пост в зарубу | kind, own |
| `report`, `crisis_shown` | жалоба, кризис | reason, adult |

## Что пишет бэкенд сам

`player_created` (мир, группа возраста, пришёл по ссылке или нет), `invite_converted`, `post_scored` (три шкалы, quality, номер поста за день), `level_up`, `card_earned`, `stage_reached`, `hate_wave`, `challenge_started`, `challenge_finished`, `notification_sent`, `notification_opened`, `plus_shown`. Сервер видит то, чего не видит фронт: оценки модели, конверсию приглашений, уведомления.

## Метрики теста

D1 ≥ 40%, D7 ≥ 15%; K (честный: приглашённый дожил до D1) ≥ 0,3; постов за сессию; доля своих слов к 3-му дню; конверсия лимит → приглашение; `plus_interest` на показ 04-2.

## Где хранить и смотреть

| Что | Где хранится | Где смотрим |
|---|---|---|
| События поведения (`POST /events`) | OTel logs, OpenObserve stream `product`; бэкенд добавляет `player_id` и `reki.stream=product` | OpenObserve, представления из [dashboards.md](dashboards.md) |
| Ошибки фронта (`POST /logs`) | OTel logs, OpenObserve stream `backend` | OpenObserve по severity, event, screen, status и path |
| Ошибки бэкенда | Console + OTel logs в stream `backend`, связанные с trace/span | OpenObserve Logs и Traces |
| HTTP, runtime, MongoDB, LLM | Стандартные OTel metrics/traces | OpenObserve Metrics и Traces |

Почему так: один self-hosted OpenObserve принимает все три сигнала OTel и позволяет считать продуктовые показатели SQL-запросами. Отдельный Metabase в MVP дублировал бы хранение, доступы и эксплуатацию. Сторонние сервисы за рубежом не используем: аудитория 14+, 152-ФЗ.

`player_id` — созданный бэкендом обезличенный идентификатор. Он допустим в продуктовых логах во внутренней Compose-сети, но запрещён в metric labels из-за высокой кардинальности. После удаления профиля связь идентификатора с игроком исчезает. Срок хранения stream `product` — 90 дней, `backend` — 30 дней; политику retention настраивает команда в OpenObserve. Доступ только у команды.

## Нельзя собирать

Тексты постов, ответов, лички и описаний мира; ники и имена; Telegram ID и initData в событиях; точное время рождения или возраст (только группа 14-17 / 18+); геолокацию.
