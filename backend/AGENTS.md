# Backend «Реки»

Эти правила действуют для всего `backend/` и дополняют корневой `AGENTS.md`. Если документы расходятся, приоритет у корневых правил, `docs/game-flow.md`, `docs/config/balance.json` и `docs/backend-spec.md`. Решения из этого файла не дают права молча менять игровые правила или публичный API.

## Технологический контракт

- .NET 10 (`net10.0`), `LangVersion=latest`, nullable reference types и warnings as errors.
- Modular monolith, Clean Architecture и DDD в четырёх production-проектах: Api, Infrastructure, Application и Domain. Один процесс и один deployable; feature/use-case boundaries задаются папками и namespaces, а не новыми проектами.
- MongoDB через `MongoDB.Driver` **3.9.0**. Entity Framework и другие ORM не добавлять.
- HTTP API реализуется только ASP.NET Core controllers с attribute routing. Minimal API handlers для прикладных маршрутов не использовать.
- Source generation: `Mediator`, `Validly`, `Mapperly`, `Vogen`, `System.Text.Json`.
- Внешние HTTP API — декларативные клиенты `WebApiClientCore`, работающие поверх настроенного `HttpClient`.
- OpenAPI генерируется встроенным ASP.NET Core OpenAPI и на build-time через `Microsoft.Extensions.ApiDescription.Server`; клиент тестов генерируется NSwag.
- Логи — Serilog через MEL; console остаётся локальным sink, OTLP provider отправляет logs, metrics и traces через Collector в OpenObserve. Прикладные логи — через `[LoggerMessage]` или экземплярный компонент с `ILogger<T>` и `BeginScope`.
- Метрики и трейсы — OpenTelemetry/OTLP, хранение и просмотр — OpenObserve.
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
    Reki.Api/                         # composition root, exception handlers, controllers
    Reki.Infrastructure/              # MongoDB, external API clients, adapters
    Reki.Application/                 # commands, queries, handlers, ports
    Reki.Domain/                      # aggregates, entities, value objects, domain events
  tests/
    Reki.Tests/                       # единственный test project
```

Зависимости направлены внутрь:

- `Domain` не ссылается на ASP.NET Core, MongoDB, Serilog и инфраструктурные пакеты.
- `Application` ссылается на `Domain`, объявляет use cases и порты и формирует готовые публичные response DTO в обработчиках.
- `Infrastructure` ссылается на `Application` и `Domain` и реализует порты: MongoDB, внешние API, часы и прочий I/O.
- `Api` ссылается на `Application` и `Infrastructure`, является composition root и HTTP-адаптером; бизнес-логики в controllers нет.
- Не создавай отдельные проекты на feature, bounded context, module или shared kernel. Новые production-проекты требуют отдельного архитектурного решения в issue.

Код внутри проектов делится по feature/use case, а не по техническим папкам `Services`, `Dtos` и `Repositories` на весь проект:

```text
Reki.Api/Controllers/<Feature>Controller.cs
Reki.Api/Requests/<Feature>/... HTTP requests и static extension mapper
Reki.Application/<Feature>/Responses/... response DTO
Reki.Application/<Feature>/<Feature>ResponseMapper.cs
Reki.Application/Responses/... общие response DTO
Reki.Application/<Feature>/<Action>Command/<Action>.cs, <Action>CommandHandler.cs
Reki.Application/<Feature>/Get<Data>/<Data>Query.cs, <Data>QueryHandler.cs
Reki.Domain/<Feature>/... aggregates, entities, value objects, domain events
Reki.Infrastructure/Mongo/Repositories/<Entity>Repository.cs
Reki.Infrastructure/Mongo/... BSON mappings, migrations, Mongo options and health check
Reki.Infrastructure/Jobs/<JobNameWithoutSuffix>/... job-specific helpers
Reki.Tests/<Feature>/<UseCase>/... component tests
```

- Один use case хранит рядом свои command/query, handler, validation, result и ports, если они не переиспользуются другим use case.
- В Application папка команды называется `<Message>Command` (`AddReplyCommand`), папка query — `Get` + имя получаемых данных без суффикса `Query` (`TodayEventQuery` → `GetTodayEvent`). Namespace соответствует расположению файла. Вспомогательные сервисы, модели и ports одного use case находятся в его папке, даже если их также использует внешний adapter. Типы нескольких use case одного поддомена остаются в корне папки поддомена. `Common` содержит только общие для нескольких поддоменов компоненты; конфигурационные модели общего баланса и связанные с ними вложенные типы остаются вместе.
- При выборе папки учитывай только использования внутри Application. Ссылки из API (включая обработку ошибок), Infrastructure (включая jobs), тестов и generated-кода не делают тип общим. Для вспомогательных типов прослеживай прикладных потребителей через сервисы и методы: связь с общим сервисом сама по себе не расширяет область использования результата одного use case. Все конфигурационные модели баланса (`GameBalance` и вложенные типы конфигурации) сгруппированы в `Common/Balance`, namespace `Reki.Application.Common.Balance`.
- Общие AI-компоненты постов (генератор, настройки, промпты, tool schemas и модели ответа) находятся в `Posts/AI`. Проверки безопасности, политика допустимого AI-ответа и связанные решения — в `Posts/AI/Safety`. Вспомогательные типы, используемые только одним use case, остаются рядом с его сообщением и обработчиком.
- Генерация реакций, ответов в тредах и самостоятельных постов звёзд инкапсулирована в Application за `IStarTurnGenerator`. Фоновая джоба вызывает генератор, проверяет актуальность данных и сохраняет результат; промпты, сериализация AI-истории и вызовы `IChatClient` остаются внутри генерации Application.
- Все собственные типы исключений каждой сборки находятся в папке `Exceptions` в корне соответствующего проекта; namespace — `<AssemblyRootNamespace>.Exceptions` (например, `Reki.Application.Exceptions`). Это правило имеет приоритет над группировкой по поддоменам, use case и `Common`. Обработчики исключений остаются рядом с соответствующими API-адаптерами или jobs.
- Публичные request/response DTO остаются immutable и не содержат бизнес-логики. Входящие HTTP-модели и штатные MVC-атрибуты binding/validation принадлежат `Api/Requests/<Subdomain>`, исходящие сообщения — `Application/<Subdomain>/Responses`, общие исходящие модели — `Application/Responses`. Application не зависит от API requests или ASP.NET Core; самописные провайдеры метаданных вместо штатных атрибутов не добавляются.
- Domain организуется вокруг business concepts и aggregate boundaries: не дублируй aggregate только ради зеркальной папки каждого handler.
- Все Mongo-репозитории Infrastructure и `MongoGameStore` находятся в `Mongo/Repositories` без подпапок; опции — в `Mongo/Options`, остальные Mongo-компоненты, включая migrations и health check, — в `Mongo`. Джобы с собственными вспомогательными моделями, сервисами или репозиториями сгруппированы в `Jobs/<JobNameWithoutSuffix>`; джобы без таких файлов остаются в корне `Jobs`. Общие репозитории не переносятся в папку одной джобы.
- Регистрация модулей Infrastructure находится в их `DependencyInjection`: `AI.AddChatClients`, `Mongo.AddMongo`, `Jobs.AddJobs`, `RekiContext.AddRekiContext`. Корневой `DependencyInjection.AddInfrastructure` объединяет эти регистрации. Папка контекста сохраняет имя `RekiContext`.
- Реализации, регистрируемые в DI, включая обработчики Mediator, объявляются `internal`, если они используются только внутри своей сборки. Публичными остаются необходимые межсборочные контракты и типы, публичность которых требуется фреймворку. Доступ тестов к внутренним типам задаётся через `InternalsVisibleTo`, а не через `public`.
- `MongoCollections` находится в `Mongo/Repositories`. Джобы переиспользуют существующие репозитории через интерфейсы Application; при нехватке функциональности расширяются эти интерфейсы и реализации в `Mongo/Repositories`. Mongo-запросы остаются в репозиториях и используют типизированные filters и updates с выражениями к свойствам.
- Логические bounded contexts изолируются namespaces и internal API. Они не читают Mongo collections друг друга напрямую; между ними используй узкие application contracts и integration events, а domain events оставляй внутри context.
- Не создавай общие `Utils`, `Helpers`, generic repository или god-service. Папка `Common` допустима только для небольших cross-cutting primitives с несколькими реальными consumers.

## Модели сообщений и маппинг

Solution содержит четыре production-проекта: Api, Application, Infrastructure и Domain. Отдельного проекта Contracts нет.

- Исходящие сообщения, включая SSE events и вложенные response-модели, принадлежат Application: `Application/<Subdomain>/Responses`. Общие для нескольких поддоменов исходящие модели находятся в `Application/Responses`; использования только из API, Infrastructure или тестов не делают модель общей.
- Внутри `Api/Requests/<Subdomain>` и `Application/<Subdomain>/Responses` самодостаточная модель остаётся отдельным файлом. Составная модель и принадлежащие только ей вложенные модели группируются в подпапке по имени корневого класса без суффикса `Request`/`Response`; namespaces соответствуют папкам. Вложенные модели общие для нескольких корней не дублируются: самодостаточные остаются на общем уровне поддомена, составные образуют собственную группу. Полиморфная иерархия исходящих событий хранится вместе с базовой моделью; enum, принадлежащие только группе, находятся рядом с её моделями. Если внутри Requests/Responses соответствующего поддомена получается только одна папка-группа, дополнительный уровень не создаётся: её модели остаются непосредственно на уровне поддомена. Доменные типы и стандартные библиотечные типы ради группировки не переносятся.
- Маппинг исходящих сообщений находится в корне `Application/<Subdomain>` в статическом extension mapper, вне папки Responses. Существующий mapper переиспользуется и остаётся на уровне поддомена; при отсутствии создаётся новый. В `Application/<Subdomain>/Responses` находятся специфичные для поддомена модели. Корневая `Application/Responses` предназначена для общих моделей. Общий корневой mapper для всех поддоменов не создаётся.
- Входящие HTTP-модели находятся в `Api/Requests/<Subdomain>`, включая вложенные request-модели и HTTP binding/validation metadata. Статические extension mappers входящих моделей находятся в той же папке и преобразуют запросы в application commands, queries или их входные модели. Application не зависит от API request-типов или ASP.NET Core.
- Модели Responses не используются как вложенные входящие модели: для таких случаев создаются request-типы с сохранением публичного JSON-формата. Корневая папка `Api/Mapping` и DI-мэпперы не используются.
- Доменные enum и value objects переиспользуются, когда совпадают смысл и допустимые значения. HTTP enum остаются в API, прикладные типы — в своём поддомене Application. Типы не переносятся в Domain только ради общего доступа; сериализованные имена, значения и ключи словарей сохраняются.
- Единственный source-generated `ApiJsonSerializerContext` и настройка сериализации остаются в `Api/Serialization`. MVC, HTTP JSON и AI используют общую настройку; Application получает `IOptions<JsonSerializerOptions>` через DI. Новые контексты в Application/Infrastructure и reflection fallback не добавляются.
- Финальным этапом рефакторинга максимально сокращай ручной маппинг средствами Riok.Mapperly: используй генерируемые static partial extension methods, маппинг вложенных моделей, коллекций и совместимых enum. Переиспользуй типизированные преобразования; ручной код оставляй для необходимых правил, вычислений и очистки данных. Сохраняй контракт и поведение, не скрывай unmapped members blanket suppressions и не используй reflection fallback.
- `Application` зависит от `Domain`, `Infrastructure` — от `Application` и `Domain`, `Api` — от `Application`, `Infrastructure` и при необходимости `Domain`. NSwag imports/aliases и потребители используют фактические namespaces API requests и Application responses.

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
- Redundant `this.` qualification в handwritten code запрещена анализатором `IDE0003`; CI запускает общий `dotnet format style --verify-no-changes` gate без фильтра по diagnostic ID. StyleCop `SA1101` отключён как конфликтующий. Используй `this.` только когда он необходим для разрешения неоднозначности. Generated `*.g.cs` исключены через `generated_code = true`.
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
- Каждый Mongo repository работает с одной сущностью (aggregate root); интерфейс и реализация лежат в папках соответствующей feature. Потребители получают нужные repository interfaces напрямую через DI. `IGameStore` / `MongoGameStore` пока координирует транзакционные операции над несколькими сущностями: коммиты поста и ответа, а также удаление всех данных игрока. Чтение ID миров и удаление связанных документов и профиля выполняются в одной транзакции, последовательно через общую сессию. Проверки доступа между сущностями выполняются в Application.
- Один command/query и один handler на use case. Не создавай сервисы с десятками несвязанных методов.
- Имена Mediator messages: query — получаемые данные + `Query`, без префикса `Get` (`PostsQuery`, `PlayerQuery`); команда — только действие, без суффикса `Command` (`AddPost`, `UpdateProfile`). Имя обработчика — имя сообщения + тип сообщения, если его ещё нет в имени, + `Handler`: `PostsQueryHandler`, `AddPostCommandHandler`. Это правило действует и для потоковых обработчиков. Имя файла совпадает с именем объявленного типа.
- Mediator interfaces соответствуют типу сообщения: `IQuery<TResponse>` / `IQueryHandler<TQuery, TResponse>` для запросов, `ICommand` / `ICommandHandler<TCommand>` или их варианты с `TResponse` для команд. Потоковые сообщения и обработчики используют `IStreamCommand<TResponse>` / `IStreamCommandHandler<TCommand, TResponse>` либо соответствующие `IStreamQuery` interfaces для запросов. Общие `IRequest` / `IRequestHandler` и их stream-варианты для прикладных команд и запросов не использовать.
- Личка, память лички, друзья и зарубы временно отключены. Social controller и публичные DTO сохранены как пустые заглушки без Mediator, MongoDB, LLM и побочных эффектов. Не возвращай удалённые application handlers, репозитории, доменные модели и бонусы заруб без явного решения команды. `StarId`, отношения при публикации и приглашения относятся к работающим механикам.
- Коллекции `stars_memory` и `challenges` подлежат удалению из MongoDB новой миграцией: удаляй сами коллекции вместе с документами и индексами, а не только очищай документы. Миграция должна корректно обрабатывать отсутствие коллекций. Обращения к ним из рабочего кода, включая `MongoGameStore` и транзакционное удаление аккаунта, удаляются. Исторические миграции сохраняют обращения через raw BSON; новую миграцию удаления выполняй после них. Не создавай эти коллекции повторно при запуске приложения.
- Domain events отделяют побочные эффекты внутри bounded context; integration events между contexts не должны выдавать внутреннюю domain model.
- Используй `TimeProvider`, а не `DateTime.UtcNow`, и передавай `CancellationToken` во все async I/O операции.
- `Mediator.SourceGenerator` подключается в outermost executable project один раз; `Mediator.Abstractions` — в Application, где объявлены messages и handlers. Ограничивай сканирование marker-типом Application assembly. Не добавляй MediatR.
- Mediator и его handlers имеют singleton lifetime. `GameDayPeriod` также singleton. В singleton-конструкторах не должно быть scoped-зависимостей, в частности `ITelegramUserAccessor` и `IRekiContext`.
- Пользовательское состояние читается через singleton `IRekiContextAccessor`: внутри каждого `Handle` получи `Context` в локальную переменную. Конкретный `IRekiContext` зарегистрирован scoped; контекст, задачи и данные игрока не сохраняются в полях singleton-handler.
- `Player` и `Worlds` лениво переиспользуют загрузки в пределах HTTP-запроса. После LLM, когда требуется свежесть перед коммитом, вызови существующий `Reload()` и заново получи обе задачи; посты и переписку перечитывай отдельно. Проверки версий и транзакции сохраняются.
- Промежуточные `PlayerQuery`, `OwnedWorldsQuery` и `WorldQuery` не возвращать. Свой мир выбирается из `Context.Worlds` с проверкой наличия; чтение постов через repository допускается только для проверенного `WorldId`, отдельный пост ограничивается этим же миром.
- Онбординг получает проверенный `TelegramUserId` в command из HTTP-границы. Публичные карточки, соперники, приглашения и фоновые jobs читают store по явным ID и сохраняют свои проверки доступа; пользовательский контекст им не обязателен. Ежедневная подготовка состояния остаётся в jobs, не дублируется в контексте.
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
- Телеметрия не хранится в доменных Mongo-коллекциях. Отказ OTLP exporter не должен раскрывать секреты или менять бизнес-результат. Для MongoDB tracing регистрируй `MongoTelemetry.ActivitySourceName`; query text не включай.

## HTTP API, JSON и внешние сервисы

- Все controllers находятся в корневой папке проекта `Api/Controllers` без вложенных подпапок, namespace — `Reki.Api.Controllers`. Они наследуются от `ControllerBase` и помечаются `[ApiController]`. Маршруты задаются attribute routing; `Program.cs` содержит composition root и одно `MapControllers()`.
- Каждый action имеет явный HTTP-атрибут, стабильное имя route, `CancellationToken` и возвращает конкретный typed result из `Microsoft.AspNetCore.Http.HttpResults` через `TypedResults`. Для нескольких вариантов используй `Results<T1, T2, ...>`. Атрибуты `[ProducesResponseType]` не добавляй: OpenAPI получает response metadata из typed return type. Не смешивай controllers с `MapGet`/`MapPost` и самописными Minimal API endpoint groups.
- Controller только связывает HTTP-вход с command/query и возвращает результат через `TypedResults`. Domain entities и внутренние application results наружу не возвращаются; преобразование в публичные DTO и SSE events выполняется в Application. Существующие маршруты мигрируй по use case, не добавляй новые mapper-вызовы в controllers.
- Ошибки отдаются как `ProblemDetails`/`ValidationProblemDetails` с устойчивыми кодами; stack trace и внутренние сообщения клиенту не выдаются.
- Глобальные исключения обрабатываются через зарегистрированный `IExceptionHandler`. Свои application exceptions маппятся на безопасные `ProblemDetails`; неизвестные исключения дают обобщённый HTTP 500 без `Exception.Message`.
- Используй `System.Text.Json` без Newtonsoft.Json. Все request/response/external DTO и используемые ими Vogen types регистрируются в `JsonSerializerContext`; подключай сгенерированную Vogen converter factory. В JSON value object представляется underlying primitive, а не объектом `{ "value": ... }`; reflection fallback не включай незаметно.
- Внешний API объявляется интерфейсом WebApiClientCore и скрывается за портом Application. Base URL, timeout и credentials приходят из validated options.
- Каждый внешний client получает factory-managed `HttpClient`, timeout и resilience policy. Не создавай `new HttpClient()` и не повторяй небезопасные HTTP методы автоматически.

## OpenAPI и NSwag

- Используй `AddOpenApi`/`MapOpenApi`; build-time document создаёт `Microsoft.Extensions.ApiDescription.Server`. Для UI разрешён только пакет `Swashbuckle.AspNetCore.SwaggerUI`; `AddSwaggerGen` и генератор Swashbuckle не использовать.
- OpenAPI — проверяемый контракт. Изменение endpoint считается завершённым только после обновления build-time spec и успешной генерации клиента.
- `nswag.json` лежит рядом с тестируемым API project и использует только относительные пути/ MSBuild variables.
- C#-конфиг `backend/src/Reki.Api/nswag.json` не генерирует DTO (`generateDtoTypes=false`): тестовый client использует request-типы `Reki.Api` и response-типы `Reki.Application`. Это ограничение не относится к TypeScript.
- Независимый `frontend/nswag.frontend.json` генерирует Fetch-клиент и DTO-интерфейсы при сборке API после OpenAPI. TypeScript-клиент хранится в Git; OpenAPI и C#-клиент остаются ignored. Обновление: изменить API → собрать API → закоммитить TS-клиент → проверить фронт.
- В `Reki.Tests.csproj` должен быть custom MSBuild target, который после получения build-time OpenAPI запускает локальный tool manifest (`dotnet nswag run ...`) и генерирует typed test client до компиляции тестов.
- Generated client хранится в явно выделенной папке `Generated/`, не редактируется вручную и генерируется детерминированно. Конфигурация не должна обращаться к запущенному dev-серверу или remote URL.

## Логи и метрики

- Прикладной код зависит от `Microsoft.Extensions.Logging.ILogger<T>`, а не от статического `Serilog.Log`.
- Хост подключает Serilog через `builder.Host.UseSerilog(..., writeToProviders: true)`. `writeToProviders: true` обязателен: MEL providers, включая provider тестового раннера, должны видеть события приложения.
- Не добавляй второй MEL Console provider, если активен Serilog Console sink, иначе сообщения будут дублироваться.
- Production logging: Serilog пишет в console и через `writeToProviders: true` передаёт записи OTel provider. OTLP endpoint задаётся стандартными `OTEL_*` variables; приложение не знает credentials хранилища.
- Каждый класс, который пишет прикладной лог, должен быть `partial`, получать `ILogger<TClass>` через primary constructor и объявлять свои нестатические `partial` методы с `[LoggerMessage]` внутри этого же класса. Вызывай их как instance methods; не создавай статические `*Logs` helper-классы и не передавай `ILogger` параметром в logging methods.
- Для стабильных сообщений не используй string interpolation и destructuring больших графов. До построения DI host используй локально принадлежащий `ILoggerFactory`; static `Serilog.Log` запрещён и для bootstrap-сообщений.
- Никогда не логируй токены, connection strings, Telegram init data, тексты игрока, личные данные или полный HTTP body.
- OpenTelemetry настраивается для metrics с ASP.NET Core, HttpClient, runtime и custom meters и экспортирует их через OTLP. Routing задаётся стандартными `OTEL_*` variables.
- Custom metrics создаются singleton-сервисами через `IMeterFactory`. Tags только низкой кардинальности; user ID, post ID, request ID и свободный текст в tags запрещены. В обезличенных product logs `player_id` разрешён для DAU/retention.
- Продуктовые log streams разделяй экземплярным `ILogger.BeginScope` с `reki.stream`; маршрутизацию выполняет Collector. Не создавай static logging helpers и отдельные sink/enricher-фабрики.
- OpenObserve используется как self-hosted OTLP backend; приложение не должно зависеть от OpenObserve или Collector для запуска. GenAI content capture всегда выключен.

## Тесты

- Единственный проект `Reki.Tests` содержит компонентные тесты бизнес-сценариев полного HTTP pipeline и интеграционные проверки Mongo-миграций. Unit-тесты доменных типов, handlers, serializers, парсинга и инфраструктурных wrappers не создавай.
- Все Mongo-миграции проверяются в `Persistence/GameIndexMigrationTests.cs` с общим lifecycle БД/session/cleanup; отдельные классы на миграцию не создавай.
- Мелкий рефакторинг без изменения бизнес-поведения не требует новых тестов. Сначала используй существующий компонентный сценарий; расширяй его только для нового наблюдаемого бизнес-результата или найденной регрессии. Не дублируй сценарии и не тестируй генераторы/конвертеры ради технического покрытия.
- Component tests используют `TUnit.AspNetCore` `TestWebApplicationFactory<Program>`/`WebApplicationTest`, реальный MongoDB в Testcontainers и сгенерированный NSwag client.
- Mongo container можно разделять на test session, но данные каждого теста изолируются уникальной БД/префиксом и очищаются. Тесты должны быть безопасны при параллельном запуске.
- Все внешние зависимости с underlying `HttpClient` заменяются WireMock.Net. MongoDB и HTTP pipeline настоящие; handlers и Mediator не подменяются моками. NSubstitute остаётся доступным для точечных подмен внешних object-level зависимостей через DI, если этого требует бизнес-сценарий.
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
| ASP.NET Core controllers | `dotnet-webapi` |
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
2. Запустить `dotnet format Reki.slnx style --verify-no-changes --no-restore --severity warn`.
3. Собрать solution в Release; build обязан также создать OpenAPI и test client без diff от ручных правок.
4. Запустить весь `Reki.Tests` на MTP v2 и убедиться, что component tests реально выполнились.
5. Собрать Docker image из чистого build context и проверить `/health/live` после запуска без root.
6. Запустить корневой `.github/scripts/check-secrets.sh --all`.

Не объявляй задачу завершённой, если build, tests, OpenAPI/NSwag generation или Docker build не проверены. Если Docker daemon, Rider MCP или внешний tool недоступен, назови конкретную непроверенную часть.

## Запрещено

- Ручные primitive wrappers вместо Vogen для одно-значных value objects/IDs; EF Core, Minimal API для прикладных маршрутов, MediatR, AutoMapper, Newtonsoft.Json, runtime-reflection serialization/mapping без отдельного решения.
- Дополнительные production-проекты без отдельного решения; прямой cross-feature доступ к чужим Mongo collections.
- Бизнес-логика в controllers, Mongo repositories или mapping profiles.
- Реальные секреты, `.env`, адреса окружений, личные пути и персональные данные в коде, fixtures, logs, snapshots, Docker layers и OpenAPI examples.
- Ручное редактирование generated OpenAPI/NSwag client.
