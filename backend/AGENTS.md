# Backend «Реки»

Эти правила действуют для всего `backend/` и дополняют корневой `AGENTS.md`. Если документы расходятся, приоритет у корневых правил, `docs/game-flow.md`, `docs/config/balance.json` и `docs/backend-spec.md`. Решения из этого файла не дают права молча менять игровые правила или публичный API.

## Технологический контракт

- .NET 10 (`net10.0`), `LangVersion=latest`, nullable reference types и warnings as errors.
- Modular monolith, Clean Architecture и DDD в четырёх production-проектах: Api, Infrastructure, Application и Domain. Один процесс и один deployable; feature/use-case boundaries задаются папками и namespaces, а не новыми проектами.
- MongoDB через `MongoDB.Driver` **3.9.0**. Entity Framework и другие ORM не добавлять.
- Minimal API вместо controllers. Исключение требует отдельного решения в issue.
- Source generation: `Mediator`, `Validly`, `Mapperly`, `Vogen`, `System.Text.Json`.
- Внешние HTTP API — декларативные клиенты `WebApiClientCore`, работающие поверх настроенного `HttpClient`.
- OpenAPI генерируется встроенным ASP.NET Core OpenAPI и на build-time через `Microsoft.Extensions.ApiDescription.Server`; клиент тестов генерируется NSwag.
- Логи — Serilog через MEL; sinks: console и `Serilog.Sinks.MongoDB`. Прикладные логи — через `[LoggerMessage]`.
- Метрики — OpenTelemetry/OTLP, локальный просмотр — Aspire Dashboard.
- Тесты — TUnit на Microsoft.Testing.Platform v2, один проект `*.Tests`.
- Сервис обязан собираться и запускаться как Docker image.

## Структура solution и зависимости

При создании solution используй `.slnx`, Central Package Management и ровно четыре production-проекта:

```text
backend/
  Reki.slnx
  global.json
  Directory.Build.props
  Directory.Packages.props
  .editorconfig
  BannedSymbols.txt
  src/
    Reki.Api/                         # composition root, middleware, HTTP slices
    Reki.Infrastructure/              # MongoDB, external API clients, adapters
    Reki.Application/                 # commands, queries, handlers, ports
    Reki.Domain/                      # aggregates, entities, value objects, domain events
  tests/
    Reki.Tests/                       # единственный test project
```

Зависимости направлены внутрь:

- `Domain` не ссылается на ASP.NET Core, MongoDB, Serilog и инфраструктурные пакеты.
- `Application` ссылается на `Domain` и объявляет use cases и порты.
- `Infrastructure` ссылается на `Application` и `Domain` и реализует порты: MongoDB, внешние API, часы и прочий I/O.
- `Api` ссылается на `Application` и `Infrastructure`, является composition root и HTTP-адаптером; бизнес-логики в endpoints нет.
- Не создавай отдельные проекты на feature, bounded context, module, contracts или shared kernel. Новые production-проекты требуют отдельного архитектурного решения в issue.

Код внутри проектов делится по feature/use case, а не по техническим папкам `Services`, `Dtos` и `Repositories` на весь проект:

```text
Reki.Api/<Feature>/<UseCase>/Endpoint.cs, Request.cs, Response.cs
Reki.Application/<Feature>/<UseCase>/Command.cs|Query.cs, Handler.cs, Result.cs
Reki.Domain/<Feature>/... aggregates, entities, value objects, domain events
Reki.Infrastructure/<Feature>/... repositories, BSON mappings, external adapters
Reki.Tests/<Feature>/<UseCase>/... component and domain tests
```

- Один use case хранит рядом свои command/query, handler, validation, result и ports, если они не переиспользуются другим use case.
- API contracts принадлежат соответствующему endpoint slice; application contracts не зависят от HTTP.
- Domain организуется вокруг business concepts и aggregate boundaries: не дублируй aggregate только ради зеркальной папки каждого handler.
- Infrastructure-код, специфичный для use case, лежит в том же feature slice; общий repository/serializer остаётся на уровне feature.
- Логические bounded contexts изолируются namespaces и internal API. Они не читают Mongo collections друг друга напрямую; между ними используй узкие application contracts и integration events, а domain events оставляй внутри context.
- Не создавай общие `Utils`, `Helpers`, generic repository или god-service. Папка `Common` допустима только для небольших cross-cutting primitives с несколькими реальными consumers.

## Общие MSBuild-настройки и анализаторы

Все проекты наследуют эти свойства из `Directory.Build.props`; не повторяй их в `.csproj`:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <LangVersion>latest</LangVersion>
    <JsonSerializerIsReflectionEnabledByDefault>false</JsonSerializerIsReflectionEnabledByDefault>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="StyleCop.Analyzers" PrivateAssets="all" />
    <PackageReference Include="SonarAnalyzer.CSharp" PrivateAssets="all" />
    <PackageReference Include="CSharpGuidelinesAnalyzer" PrivateAssets="all" />
    <PackageReference Include="Nullable.Extended.Analyzer" PrivateAssets="all" />
    <PackageReference Include="Microsoft.VisualStudio.Threading.Analyzers" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.BannedApiAnalyzers" PrivateAssets="all" />
    <AdditionalFiles Include="$(MSBuildThisFileDirectory)BannedSymbols.txt" />
  </ItemGroup>
</Project>
```

Версии анализаторов закреплены в `Directory.Packages.props`:

```xml
<ItemGroup>
  <PackageVersion Include="StyleCop.Analyzers" Version="1.2.0-beta.556" />
  <PackageVersion Include="SonarAnalyzer.CSharp" Version="10.5.0.109200" />
  <PackageVersion Include="CSharpGuidelinesAnalyzer" Version="3.8.5" />
  <PackageVersion Include="Nullable.Extended.Analyzer" Version="1.15.6495" />
  <PackageVersion Include="Microsoft.VisualStudio.Threading.Analyzers" Version="17.12.19" />
  <PackageVersion Include="Microsoft.CodeAnalysis.BannedApiAnalyzers" Version="3.3.4" />
</ItemGroup>
```

- Analyzer diagnostics являются build errors из-за `TreatWarningsAsErrors`; не добавляй глобальные `NoWarn`, `WarningsNotAsErrors` или blanket suppressions.
- Настраивай severity и style централизованно в `.editorconfig`. Локальный suppression требует комментария с причиной и минимального scope.
- `BannedSymbols.txt` хранится в корне `backend/` и содержит запрещённые API с объяснением безопасной альтернативы.
- Generated files должны иметь `<auto-generated>` marker. Исключения анализаторов для `Generated/` задаются точечно в `.editorconfig`, а не ослабляют handwritten code.

Версии NuGet хранятся только в `Directory.Packages.props`, общие MSBuild-свойства — в `Directory.Build.props`, SDK и MTP runner — в `global.json`. Все package versions закреплены; не используй `*`, floating versions или тег image `latest` и не обновляй зависимости попутно. `LangVersion=latest` — явное исключение для языка, заданное этим документом.

## DDD и application layer

- Aggregate root — граница согласованности и единственная точка изменения дочерних entities. Инварианты защищает Domain, даже если вход уже проверен Validly.
- Используй value objects и strongly typed IDs для доменных понятий; public setters у aggregate state запрещены.
- Все value objects, представимые одним underlying value, и все strongly typed IDs реализуются через Vogen: `[ValueObject<T>]` на `readonly partial struct`. Создавай их через `From`/`TryFrom`, а не через constructor, cast или `default`.
- Validation и normalization Vogen-типа находятся рядом с типом в Domain (`Validate`, при необходимости `NormalizeInput`). Не отключай Vogen analyzers и не подавляй их diagnostics без решения в issue.
- Сохраняй explicit conversion между Vogen type и primitive. Implicit conversion запрещён: он ослабляет типизацию и может скрыть validation. Primitive не должен протекать через domain API вместо value object.
- Составной value object с несколькими равноправными полями, например amount + currency, остаётся immutable `record`/`record struct`; не упаковывай его искусственно в один внутренний DTO только ради Vogen.
- Репозиторий создаётся на aggregate root, а не на каждую entity. Интерфейс находится в Domain/Application согласно направлению зависимостей, Mongo-реализация — в Infrastructure.
- Один command/query и один handler на use case. Не создавай сервисы с десятками несвязанных методов.
- Domain events отделяют побочные эффекты внутри bounded context; integration events между contexts не должны выдавать внутреннюю domain model.
- Используй `TimeProvider`, а не `DateTime.UtcNow`, и передавай `CancellationToken` во все async I/O операции.
- `Mediator.SourceGenerator` подключается в outermost executable project один раз; `Mediator.Abstractions` — в Application, где объявлены messages и handlers. Ограничивай сканирование marker-типом Application assembly. Не добавляй MediatR.
- Validly применяй для compile-time validation request/command моделей. Типы с `[Validatable]` должны быть `partial`. Ошибки границы переводятся в единый `ValidationProblem`; это не заменяет доменные инварианты.
- Mapperly применяй на границах Domain/Application/API. Mapper-классы `partial`; unmapped members не игнорируй молча. Не добавляй AutoMapper и reflection-based mapping.

## MongoDB

- `MongoClient` регистрируется singleton. Настройки подключения и имя БД берутся из typed options; строка подключения не должна попадать в Domain/Application.
- Aggregate хранится как один документ, если это укладывается в разумный размер. Ссылки между aggregates — только по ID.
- Не выставляй `IMongoCollection<T>`, `IFindFluent<,>` или `IQueryable` за пределы Infrastructure. Используй typed filters, projections и update definitions.
- BSON mapping, serializers, collection names и index creation находятся в Infrastructure. Domain не размечается Mongo-атрибутами без отдельного обоснования.
- BSON serializers для Vogen types генерируются в Infrastructure через Vogen `[BsonSerializer<Domain.Type>]` и регистрируются до первого обращения к MongoDB. Не включай `Conversions.Bson` прямо на Domain type, если это добавляет MongoDB-зависимость во внутренний слой.
- Индексы объявляются явно и создаются идемпотентно при старте/инициализации. Новый query обязан учитывать индекс и projection.
- Multi-document transactions не являются default. Сначала пересмотри aggregate boundary; необходимость транзакции зафиксируй в issue и покрой component test.
- Для конкурентных записей используй version/optimistic concurrency там, где возможна потеря обновления.
- Коллекция логов Serilog отделена от доменных коллекций. Отказ log sink не должен раскрывать секреты или менять бизнес-результат.

## HTTP API, JSON и внешние сервисы

- Endpoints группируются по feature/use case через `MapGroup`; `Program.cs` содержит только composition root и одно подключение endpoint groups.
- Каждый endpoint — именованный static method с `TypedResults` и явным union return type. Всегда задавай `.WithName()`, `.WithTags()`, `.WithSummary()` и документируй все ответы.
- Endpoint только связывает HTTP contract с command/query и переводит результат в HTTP. Domain entities наружу не возвращаются.
- Ошибки отдаются как `ProblemDetails`/`ValidationProblemDetails` с устойчивыми кодами; stack trace и внутренние сообщения клиенту не выдаются.
- Используй `System.Text.Json` без Newtonsoft.Json. Все request/response/external DTO и используемые ими Vogen types регистрируются в `JsonSerializerContext`; подключай сгенерированную Vogen converter factory. В JSON value object представляется underlying primitive, а не объектом `{ "value": ... }`; reflection fallback не включай незаметно.
- Внешний API объявляется интерфейсом WebApiClientCore и скрывается за портом Application. Base URL, timeout и credentials приходят из validated options.
- Каждый внешний client получает factory-managed `HttpClient`, timeout и resilience policy. Не создавай `new HttpClient()` и не повторяй небезопасные HTTP методы автоматически.

## OpenAPI и NSwag

- Используй `AddOpenApi`/`MapOpenApi`; Swashbuckle не добавлять. Build-time document создаёт `Microsoft.Extensions.ApiDescription.Server`.
- OpenAPI — проверяемый контракт. Изменение endpoint считается завершённым только после обновления build-time spec и успешной генерации клиента.
- `nswag.json` лежит рядом с тестируемым API project и использует только относительные пути/ MSBuild variables.
- В `Reki.Tests.csproj` должен быть custom MSBuild target, который после получения build-time OpenAPI запускает локальный tool manifest (`dotnet nswag run ...`) и генерирует typed test client до компиляции тестов.
- Generated client хранится в явно выделенной папке `Generated/`, не редактируется вручную и генерируется детерминированно. Конфигурация не должна обращаться к запущенному dev-серверу или remote URL.

## Логи и метрики

- Прикладной код зависит от `Microsoft.Extensions.Logging.ILogger<T>`, а не от статического `Serilog.Log`.
- Хост подключает Serilog через `builder.Host.UseSerilog(..., writeToProviders: true)`. `writeToProviders: true` обязателен: MEL providers, включая provider тестового раннера, должны видеть события приложения.
- Не добавляй второй MEL Console provider, если активен Serilog Console sink, иначе сообщения будут дублироваться.
- Production sinks: console и structured BSON sink `Serilog.Sinks.MongoDB`. Connection string Mongo sink приходит через configuration/env; в `appsettings*.json` допускается только несекретная структура.
- Для стабильных сообщений используй `partial` методы с `[LoggerMessage]`; без string interpolation и без destructuring больших графов.
- Никогда не логируй токены, connection strings, Telegram init data, тексты игрока, личные данные или полный HTTP body.
- OpenTelemetry настраивается для metrics с ASP.NET Core, HttpClient, runtime и custom meters и экспортирует их через OTLP. Routing задаётся стандартными `OTEL_*` variables.
- Custom metrics создаются singleton-сервисами через `IMeterFactory`. Tags только низкой кардинальности; user ID, post ID, request ID и свободный текст в tags запрещены.
- Aspire Dashboard используется как локальный OTLP backend; приложение не должно зависеть от Dashboard для запуска.

## Тесты

- Единственный проект `Reki.Tests` содержит component tests полного HTTP pipeline и небольшое число unit tests для сложных domain rules.
- Component tests используют `TUnit.AspNetCore` `TestWebApplicationFactory<Program>`/`WebApplicationTest`, реальный MongoDB в Testcontainers и сгенерированный NSwag client.
- Mongo container можно разделять на test session, но данные каждого теста изолируются уникальной БД/префиксом и очищаются. Тесты должны быть безопасны при параллельном запуске.
- Все зависимости с underlying `HttpClient` заменяются WireMock.Net. NSubstitute используется для object-level ports в unit tests, но не для `HttpClient`, Mongo driver и component-test pipeline.
- AutoFixture создаёт Vogen value objects через общие customizations и публичные `From`/`TryFrom`, никогда через reflection, uninitialized object или `default`. Не используй случайность там, где от неё зависит смысл сценария.
- Каждый тест имеет визуально разделённые `// Arrange`, `// Act`, `// Assert`; имя — `MethodName_Scenario_ExpectedBehavior` по Roy Osherove.
- Проверяй observable behavior: HTTP status/body/headers, persisted Mongo document и emitted integration event. Не проверяй внутренние вызовы без необходимости.
- Для времени используй `FakeTimeProvider`; `Task.Delay` и зависимость от текущих часов запрещены.
- Логи приложения должны быть видны в test output JetBrains Rider. Для этого используй TUnit `TestWebApplicationFactory` и его `CreateClient()` (он регистрирует/correlates TUnit MEL provider), а в приложении сохраняй `UseSerilog(..., writeToProviders: true)`. Не заменяй эту схему выводом в файл или `Console.WriteLine`.
- MTP v2 выбирается в `global.json` через runner `Microsoft.Testing.Platform`. Для точных команд/filters сначала используй навык `run-tests`; не переноси VSTest flags в MTP/TUnit.

## Docker и секреты

- Поддерживай multi-stage `Dockerfile`: build/publish в `mcr.microsoft.com/dotnet/sdk:10.0`, runtime в `mcr.microsoft.com/dotnet/aspnet:10.0`. Runtime image не содержит SDK и исходников.
- Контейнер запускается non-root (`USER app`), слушает container port 8080 и корректно обрабатывает SIGTERM. Добавь `.dockerignore` для `.git`, IDE, `bin/`, `obj/`, test results и локальных секретов.
- Образ собирается из чистого checkout командой из `backend/`; restore layer сначала копирует project/props files, затем source, чтобы сохранялся Docker cache.
- API предоставляет отдельные `/health/live` и `/health/ready`; readiness проверяет критичные зависимости. Probe настраивается оркестратором/Compose, не запуском второй копии приложения внутри `HEALTHCHECK`.
- Конфигурация приходит через typed Options с `ValidateOnStart`. В контейнере переменные окружения используют стандартное отображение `Section__Property`.
- Секреты передаются только в runtime через environment variables или secret store оркестратора, который проецирует их в environment. Не передавай секреты через Docker `ARG`, `ENV` в Dockerfile, build secrets, image labels или committed Compose values.
- Не коммить `.env`, реальные значения, connection strings, токены и адреса окружений. В Compose/CI допускаются только имена variables и references на защищённые secrets; реальные значения задаются вне репозитория.
- Не печатай environment/configuration целиком, не включай секреты в exception messages и health responses. Отсутствующий обязательный secret должен останавливать startup через validation без вывода значения.

## Навыки и MCP

Используй навыки из marketplace [`dotnet/skills`](https://github.com/dotnet/skills) и [`JetBrains/rider-skills`](https://github.com/JetBrains/rider-skills). Перед соответствующей задачей прочитай `SKILL.md` каждого выбранного навыка полностью и следуй ему. Выбирай минимальный набор под текущую задачу; не загружай весь marketplace «на всякий случай».

### dotnet/skills

| Задача | Навыки |
|---|---|
| Создание solution и первичный scaffold | `dotnet-init`, `project-setup`, `project-structure` |
| Современный C# и общие code conventions | `modern-csharp` |
| Проектирование и проверка границ архитектуры | `clean-architecture`, `ddd`, `arch-check` |
| ASP.NET Core API и Minimal API | `dotnet-webapi`, `minimal-api` |
| OpenAPI, build-time contract и versioning | `openapi`; `api-versioning` только при реальном введении новой версии API |
| ProblemDetails, validation и exception pipeline | `error-handling` |
| Liveness/readiness и dependency checks | `health-check` |
| Authentication/authorization | `authentication`, `configure-auth` |
| Typed configuration и secrets | `configuration` |
| DI registrations и lifetime audit | `dependency-injection` |
| Внешние HTTP API | `httpclient-factory`, `resilience` |
| Structured logging и metrics | `serilog`, `opentelemetry` |
| Aspire Dashboard/AppHost | `aspire` |
| Docker image | `docker` |
| CI/CD | `ci-cd` |
| Security и проверка зависимостей/секретов | `security-scan` |
| Обычная ошибка build/compile | `build-fix` |
| Неочевидная MSBuild ошибка | `binlog-generation`, затем `binlog-failure-analysis` |
| MSBuild/solution modernization | `msbuild-modernization` |
| Проверка устаревших NuGet packages | `outdated` — обновлять только в отдельной задаче |
| Проектирование тестовой стратегии | `testing` |
| Написание или расширение тестов | `code-testing-agent` |
| Определение runner/platform | `platform-detection` |
| Запуск и фильтрация тестов | `run-tests` |
| Анализ coverage и пробелов | `coverage-analysis`, `test-gap-analysis` |
| Аудит качества тестов | `assertion-quality`, `test-anti-patterns` |
| Performance review и benchmark | `analyzing-dotnet-performance`, `microbenchmarking` |
| Runtime performance trace | `dotnet-trace-collect` |
| Финальная проверка изменений | `code-review`, `verify`, `wrap-up` |

### JetBrains/rider-skills

Эти навыки применяй только когда solution открыт в Rider и Rider MCP доступен:

| Задача | Навык |
|---|---|
| Найти usages, implementations и перейти по semantic symbol | `navigating-code` |
| Rename/move/safe-delete/extract/change-signature | `refactoring-code` |
| Воспроизвести runtime bug, поставить breakpoint и исследовать state | `debugging-code` |
| Найти существующие C# tests через IDE coverage data перед изменением тестов | `finding-tests` |

Semantic refactoring выполняй через Rider, а не глобальной текстовой заменой. Rider-навыки сами должны найти live schema MCP tools; не угадывай имена или arguments. Если Rider MCP недоступен, используй безопасный CLI/file fallback и перечисли пропущенные IDE-backed проверки.

Когда solution уже создан:

- `cwm_roslyn_navigator` MCP используй для symbol navigation, diagnostics, dependency/project graph, endpoint map и проверки NuGet references. Он предпочтительнее догадок по текстовому поиску, когда нужен semantic answer.
- Rider/JetBrains MCP используй для IDE diagnostics, semantic navigation/refactoring, debugger, coverage data и Rider test/build integration. Не выдумывай имя сервера или tool: сначала найди доступный MCP tool.
- `binlog` MCP используй для неочевидных MSBuild/restore/source-generator/custom-target ошибок после создания `.binlog`; сначала `binlog_overview`, затем `binlog_diagnose`.
- Если подходящий MCP недоступен, продолжай через CLI и явно укажи, какая IDE/semantic проверка не выполнена.

## Проверка перед PR

Из `backend/`:

1. Восстановить local tools и locked NuGet dependencies.
2. Собрать solution в Release; build обязан также создать OpenAPI и test client без diff от ручных правок.
3. Запустить весь `Reki.Tests` на MTP v2 и убедиться, что component tests реально выполнились.
4. Собрать Docker image из чистого build context и проверить `/health/live` после запуска без root.
5. Запустить корневой `.github/scripts/check-secrets.sh --all`.

Не объявляй задачу завершённой, если build, tests, OpenAPI/NSwag generation или Docker build не проверены. Если Docker daemon, Rider MCP или внешний tool недоступен, назови конкретную непроверенную часть.

## Запрещено

- Ручные primitive wrappers вместо Vogen для одно-значных value objects/IDs; EF Core, controllers, MediatR, AutoMapper, Newtonsoft.Json, runtime-reflection serialization/mapping без отдельного решения.
- Дополнительные production-проекты без отдельного решения; прямой cross-feature доступ к чужим Mongo collections.
- Бизнес-логика в endpoints, Mongo repositories или mapping profiles.
- Реальные секреты, `.env`, адреса окружений, личные пути и персональные данные в коде, fixtures, logs, snapshots, Docker layers и OpenAPI examples.
- Ручное редактирование generated OpenAPI/NSwag client.
