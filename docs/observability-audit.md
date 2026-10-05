# Аудит наблюдаемости для Sber500 × Disrupt

05.10.2026. Аудит синхронизирован с issue #35, конкурсными материалами и текущей архитектурой проекта.

## Что нужно доказать по условиям конкурса

Для MVP обязательны работающий сервис, логи и метрики, отчёт нагрузки от 10 RPS, DAU, частотность обращений и оценка LLM-cost / DAU. Формальная методика считает DAU как число уникальных реальных пользователей за сутки, а «обращения / DAU» — как сумму вызовов LLM и других динамических компонентов на одного DAU. Тестовые, отладочные и обслуживающие обращения не учитываются. Для следующего этапа указаны пороги DAU ≥ 1000 и обращений / DAU ≥ 10, а технический аудит проверяет воспроизводимость метрик по логам и источникам трафика.

Отсюда главное требование к телеметрии: не максимальное число датчиков, а проверяемая связь реальной пользовательской активности, вызовов модели, стоимости и технического состояния.

## Решение

Путь данных: `backend → OTLP/gRPC → OpenTelemetry Collector → OpenObserve`.

- OpenObserve заменяет Aspire Dashboard и Metabase: один self-hosted backend принимает logs, metrics и traces и подходит для технических и продуктовых запросов MVP.
- Collector разделяет log streams по одному scoped-атрибуту `reki.stream`: `product` для событий аналитики и `backend` для остальных логов. В MEL это обычный экземплярный `ILogger<T>` с `BeginScope`; отдельные static helpers, custom sinks и logger factories не нужны.
- Фронтенд продолжает отправлять те же `POST /events` и `POST /logs`. Смена хранилища полностью скрыта за бэкендом; runtime config фронтенда остаётся без изменений.
- Для OpenObserve достаточно `OPENOBSERVE_EMAIL` и `OPENOBSERVE_PASSWORD`. Та же пара используется Collector внутри закрытой Compose-сети; отдельный ingestion token и bootstrap service account отложены до появления нескольких отправителей или внешней сети.

## Что собираем

| Сигнал | Источник | Для чего |
|---|---|---|
| Product logs | `ProductTelemetry` через `ILogger.BeginScope` | DAU, сессии, воронки, retention, `player_id` |
| Backend logs | Serilog → MEL provider → OTLP | ошибки API/фронта и переход к trace |
| HTTP metrics/traces | ASP.NET Core instrumentation | RPS, latency, status, route, ошибки |
| Outbound HTTP | HttpClient instrumentation | задержки и ошибки LLM endpoint |
| Runtime metrics | .NET runtime instrumentation | GC, allocations, exceptions, thread pool |
| MongoDB traces | `MongoTelemetry.ActivitySourceName` драйвера 3.9 | операции и задержки БД без query text |
| GenAI metrics/traces | `Microsoft.Extensions.AI` OpenTelemetry client | calls, duration, model/provider, input/output tokens без prompt/response content |

`player_id` создаёт бэкенд и не принимает от клиента как доказательство личности. Его можно передавать во внутренней сети и хранить в product logs: это необходимо для DAU и retention. В OTel metric attributes его добавлять нельзя — высокая кардинальность ухудшит стоимость и агрегацию. Корреляция с техническими spans сохраняется через trace id связанного лог-события.

Стоимость не записывается отдельной постоянно устаревающей метрикой. OpenTelemetry даёт token usage по модели; стоимость вычисляется в запросе по версионированному прайсу. Так исходные измерения остаются воспроизводимыми при изменении тарифов.

## Что сознательно не входит в #35

- автоматический provision dashboards и Telegram alerts;
- отдельный Metabase и дублирование продуктовых событий в MongoDB;
- Mongo pool/SDAM custom metrics поверх уже доступных driver traces;
- service account и ротация ingestion token при единственном внутреннем Collector;
- изменения frontend runtime config или API contract.

Это не мешает конкурсному аудиту: сырые сигналы и методика расчёта есть, а визуальные панели можно сохранить в OpenObserve после первого production smoke. Следующий шаг после появления реального трафика — зафиксировать запросы DAU, calls/DAU, cost/DAU и error rate как dashboards/alerts отдельной задачей.

## Ссылки на технические источники

- [MongoDB C# Driver: встроенный MongoTelemetry](https://github.com/mongodb/mongo-csharp-driver/blob/v3.9.0/src/MongoDB.Driver/MongoTelemetry.cs#L65)
- [Microsoft.Extensions.AI: OpenTelemetry для GenAI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)
- [Стандартная наблюдаемость .NET через OpenTelemetry](https://learn.microsoft.com/dotnet/core/diagnostics/observability-with-otel)
- [OpenObserve: OTLP ingestion](https://openobserve.ai/docs/ingestion/logs/otlp/)
- [OpenTelemetry Collector: routing connector](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/connector/routingconnector)
