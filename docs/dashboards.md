# Представления OpenObserve

05.10.2026. Источники: log streams `product` и `backend`, стандартные OTel metrics и traces. В issue #35 поднимается сбор и хранение; автоматическое provision дашбордов и алертов не входит в MVP.

## Конкурсные показатели

| Показатель | Как считать | Источник |
|---|---|---|
| DAU | `count(distinct player_id)` по календарному дню, `activity_kind = real` | `product` |
| Обращения / DAU | число GenAI operations за день / DAU | `gen_ai.client.operation.duration` и `product` |
| Токены | сумма input/output token usage по модели и операции | `gen_ai.client.token.usage` |
| LLM-cost / DAU | `(input_tokens × input_price + output_tokens × output_price) / DAU`; цены хранятся в версии запроса/дашборда, а не в телеметрии | GenAI metrics + прайс модели |
| Ошибки | 5xx и Error/Critical по часу, route и exception type | HTTP metrics + `backend` |
| Нагрузка | RPS, p50/p95/p99 latency, active requests, GC, allocations, exceptions, thread-pool queue | ASP.NET Core + runtime metrics |

Тестовые и отладочные обращения не должны попадать в конкурсный расчёт. Production-события имеют `activity_kind = real`; тесты проекта не экспортируют OTLP.

## Поля продуктового stream

`ProductTelemetry` пишет плоские log attributes: `event_name`, `player_id`, `event_time_unix_ms`, `received_time_unix_ms`, `session_id`, `platform`, `screen`, `previous_screen`, `world`, `age_group`, `experiment_plus_offer` и разрешённые `prop_*`. Тексты постов, сообщений, комментариев, имена, Telegram ID и `initData` не экспортируются.

Для DAU и когорт используется `player_id`, для сессий — `session_id`. Эти поля допустимы в log stream, но не должны становиться labels OTel-метрик.

## Продуктовые представления

1. Онбординг: уникальные игроки по `onboarding_step` и первому `post_publish`; время от `session_start` до `post_result`.
2. Первая сессия: число `post_publish` на сессию, доля `prop_own_words`, `prop_viral` и `prop_flop`, время `screen_leave`.
3. Retention: наличие любого события игрока в D1/D3/D7 относительно первого `player_created` или `session_start`, в разрезах `world` и `age_group`.
4. Лимит и Реки+: первое действие после `limit_reached`, `plus_interest` на показ и разрез `experiment_plus_offer`.
5. Друзья: `invite_sent` → `invite_converted` → первый пост → D1; `challenge_started` и `challenge_finished`.
6. Ошибки: severity, event name, HTTP route/status, screen и безопасный exception type; переход из лога в связанный trace по trace id.

Цели продукта остаются в [analytics.md](analytics.md). OpenObserve заменяет только хранилище и UI аналитики; контракт `POST /events` и `POST /logs` для фронтенда не меняется.
