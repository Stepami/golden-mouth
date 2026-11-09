# Reki backend

MVP backend scaffold for the Reki Telegram Mini App. The solution separates API contracts from the controller host, Application, Domain, and Infrastructure projects.

## Run locally

Before starting only MongoDB via Compose, set the required Compose environment variables listed under **Run in Docker** below. Compose interpolates the whole file even when only `mongo` is selected.

```powershell
cd ..
docker compose up --detach mongo
cd backend
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:8081'
$env:Auth__Issuer = 'https://localhost:5173/'
$env:Telegram__BotToken = '<telegram bot token>'
dotnet tool restore
dotnet restore Reki.slnx
dotnet run --project src/Reki.Api/Reki.Api.csproj
```

Development uses MongoDB for game data. Before starting the API outside Compose, set `Telegram__OAuthUrl` (OAuth Worker root), `Telegram__ApiUrl` (Bot API Worker root), `Auth__SigningKey` (RSA PEM), and `Auth__EncryptionKey` (32 random bytes in Base64) through environment or user secrets. The issuer above matches the local HTTPS Vite origin; Vite proxies `/api/v1` to the HTTP backend on port 8081. See [deployment key setup](../docs/deployment.md#ключи-и-настройки-входа-123). Mini App exchanges signed `Authorization: TMiniApp <initData raw>` for app tokens at `POST /auth/external/tg/miniapp`; protected game endpoints use `Authorization: Bearer <app access JWT>`. Browser login reads the public Telegram Client ID from `GET /auth/external/tg/client-id`. For local HTTPS browser login, follow the [frontend guide](../frontend/README.md#frontend). Logs stay in the console and are not written to MongoDB outside Production. Try:

- `GET /health/live`
- `GET /health/ready`
- `GET /openapi/v1.json`
- `GET /swagger/`

OpenAPI JSON and Swagger UI are available only in Development. Production does not serve these routes. Build-time OpenAPI generation for NSwag clients remains available. The exact local address is printed by ASP.NET Core when the service starts.

## Run in Docker

The project-wide Compose file lives in the repository root and starts the frontend, backend, MongoDB, OpenTelemetry Collector, and OpenObserve together:

```powershell
cd ..
$env:OPENOBSERVE_EMAIL = '<chosen local administrator email>'
$env:OPENOBSERVE_PASSWORD = '<chosen local administrator password>'
$env:TG_PAT = '<test Telegram bot token>'
$env:AUTH_ISSUER = '<local issuer URL>'
$env:AUTH_SIGNING_KEY = '<RSA private key PEM>'
$env:AUTH_ENCRYPTION_KEY = '<32-byte Base64 key>'
$env:LLM_PAT = '<provider API key>'
$env:LLM_BASE_URL = '<OpenAI-compatible provider URL>'
$env:LLM_MODEL = '<model identifier>'
docker compose up --build
```

Compose passes the required `LLM_PAT`, `LLM_BASE_URL`, and `LLM_MODEL` environment variables only to the backend container. Set `LLM_TIMEOUT_SECONDS` if the provider needs more than the default 20 seconds. Do not commit provider credentials or endpoints.

The API is available at `http://localhost:8081`, and OpenObserve at `http://localhost:5080`. Set the Telegram, auth, LLM and OpenObserve variables outside the repository before starting Compose. OpenTelemetry sends logs, metrics, and traces to Collector over the internal Compose network. Product events use the `product` log stream; other application and framework logs use `backend`. Set `MONGO_CONNECTION_STRING` outside the repository to override the bundled MongoDB service.

OTLP export is configured with the standard `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL`, and `OTEL_SERVICE_NAME` variables. MongoDB driver activities and `Microsoft.Extensions.AI` GenAI spans/metrics are enabled; prompts, responses, tool arguments, Mongo query text, tokens, and connection strings are not exported.

Stop the stack without deleting its volume:

```powershell
docker compose down
```

## MongoDB migrations

Startup runs versioned migrations before serving requests (except during build-time OpenAPI generation). Version `1.0.0`
creates the ten initial game indexes. MongoDB accepts identical existing definitions without rebuilding them, so a
deployed database with player data needs no manual baseline or migration-history entries. Missing indexes are created.

Version `1.1.0` removes `ux_challenges_active_player` when present. Gameplay index definitions are owned by migrations;
there are no additional checks for manually changed definitions.

Index DDL runs outside transactions under the migrator's database lock. Successful versions are recorded and skipped
on subsequent starts. The inherited `DownAsync` completes without changing documents or indexes.

Version `2.6.3` backfills legacy Telegram identities as `telegram:<id>` in `ExternalUserIds`, replaces the unique
Telegram index with a unique multikey external-ID index, and removes the old field. Its data updates and index changes
run outside the migration transaction under the migration lock and can be retried after interruption. Rolling back the application requires restoring a
database backup: the inherited `DownAsync` does not reverse this migration.

Version `2.1.0` moves referral attribution and bonus usage from player profiles to independent `invites`
documents with a Vogen `InvitationId`, the two player IDs, creation time, and separate usage flags/times.
It creates a unique `InviteeId` index and an `InviterId` lookup index. Registration creates the invitation without
changing the inviter's profile. The invitee's first successful publication marks both bonuses inside the existing
post transaction; unsuccessful requests do not consume them. Daily post limits read
invitation usage in the current game day and cap inviter credits at the configured maximum.

The migration preserves previously issued inviter credits and used invitee bonuses, removes their old contribution
from `DailyBonus`, and unsets the replaced profile fields. Historical first-post lookup is confined to this migration.
Missing invitation creation time defaults to the player period or Unix epoch. Inconsistent referral counters lose
the credits that cannot be reconstructed. An already-used invitee bonus without
a surviving post keeps its old daily credit until reset and cannot be granted again. Repeated migration does not
subtract credits twice.

Migration `2.4.4` drops the unused `threads` collection, including its documents and indexes. It is safe when the collection is absent or the migration is retried. Older migrations no longer create its index or backfill it. Conversations and replies remain in `posts.Comments`; publication and reply APIs are unchanged. The drop runs outside the migration transaction because MongoDB forbids dropping collections in a transaction. It cannot be rolled back.

Migration `2.4.7` drops the retired `stars_memory` and `challenges` collections, including their documents and indexes, and deletes legacy `invites` documents containing `PlayerId`. It runs after the historical Social migrations and is safe when either collection is absent or the migration is retried. The drops run outside the migration transaction and cannot be rolled back; invitation cleanup uses the migration transaction. Account deletion only matches invitations by `InviterId` or `InviteeId`; Social API stubs remain unchanged. Databases that have already applied `2.4.7` will not run its updated cleanup automatically.

## Test infrastructure

API component tests share one `ApiFactory`, WireMock server and MongoDB database per test session. The test fixture runs MongoDB Atlas Local 8.3.11 in one Testcontainers container with MongoDB and Search; production Compose uses MongoDB 9.0.4. The container health check does not guarantee that a newly created search index is ready. `TestSessionFixture` owns the MongoDB container/client, WireMock and resettable clock; TUnit disposes the dependent API factory before these resources at session end. The host starts lazily for the first API test and applies startup migrations once.

Atlas Local in this pinned image cannot update the vector index with the production command. `ApiFactory` replaces `IMigrationInstantiator` only for the API test host: the 2.6.0 adapter creates the final `vectorSearch` index with both filter fields, preserves the original embedding backfill, and waits for readiness with TUnit `WaitsFor` (60 seconds, one-second polling). The 2.6.2 adapter is a no-op because those fields already exist. All other migrations, MongoDB queries and repositories remain real; direct production migration updates are not covered by this test-host workaround.

Migration tests belong to `ParallelGroup("Migrations", Order = 0)` and use isolated temporary databases on the same MongoDB container. API tests belong to `ParallelGroup("Api", Order = 1)` and share `NotInParallel("ApiState")`, so their setup, execution and teardown do not overlap. Unit tests keep the default parallel execution. A filtered API test also works independently of the migration-test group.

`GameApiTestBase` uses separate `[After(Test)]` hooks to clear application documents and reset WireMock mappings/request logs/scenarios and the clock, including after test failures. Collections, indexes and migration history/lock collections are retained between tests. The API database is dropped only at session end. New API test classes must use:

```csharp
[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
```

## Verify

```powershell
dotnet build Reki.slnx --configuration Release
dotnet test --project tests/Reki.Tests/Reki.Tests.csproj --configuration Release --no-build
```

Building the API produces an OpenAPI document. Building the test project regenerates its typed NSwag client from that document before compilation; the client reuses request DTOs from `Reki.Api` and response DTOs from `Reki.Application` instead of generating copies.

## Scheduled maintenance (TickerQ)

TickerQ 10.4 uses `alexjamesbrown.TickerQ.MongoDB` 0.1.0 and the application's existing Mongo client/database. The provider stores schedules and execution history in `ticker_TimeTickers`, `ticker_CronTickers` and `ticker_CronTickerOccurrences` and creates its indexes at startup. Migration `2.3.0` creates the two game-day period indexes; it does not change game documents.

- `ResetGameDayJob`: `0 0 9 * * *`, time zone `Europe/Moscow` (09:00 Moscow, 06:00 UTC).
- `RefreshRegisteredPlayersJob`: `0 * * * * *`, once per minute, using MongoDB `CountDocumentsAsync` without reading profiles, worlds or invitations.

`ResetGameDayJob` and `RefreshRegisteredPlayersJob` implement `ITickerFunction` and are registered with `MapTicker`; TickerQ uses the job class names as function names, and the schedules are unchanged. Both jobs access MongoDB directly. The count job is singleton and owns the registered-player gauge. `GameDayPeriod` calculates the current period from injected `TimeProvider` and `IOptions<GameBalance>`; the reset job receives balance options through DI as well. Startup does not run maintenance jobs. The jobs run on their schedules. Reset only touches older periods. It clears player energy/daily bonus, world wants and daily star relation gains, applies the existing rested-post rule, and increments document versions. Repeated runs do not reset the same period twice. Player/world reads and writes no longer perform daily normalization. Invitation usage and daily events retain their existing behavior. Daily reset no longer grants challenge bonuses; direct messages, friends, memory and challenges are compatibility stubs.

`Jobs__ExecutionEnabled=false` disables scheduler execution (used by component tests), but persisted schedule initialization remains enabled. In production leave execution enabled. A failed maintenance execution is recorded by TickerQ; the next scheduled execution recovers an interrupted reset safely.

TickerQ request serialization uses `ApiJsonSerializerContext` through `WithJsonContext`. Runtime middleware and migrations are skipped during build-time OpenAPI generation; controller/OpenAPI routes are mapped in both modes. TickerQ execution spans (`TickerQ`) join the existing OTLP trace pipeline. The registered-player gauge remains `reki.players.registered` from `Reki.Product`. Exceptions propagate to the scheduler; its instrumentation records execution results and errors. `JobsExceptionHandler` implements `ITickerExceptionHandler` and centrally logs failed attempts and cancellations. Technical reset logs include only player/world counts and the period start, without player IDs or player text, and go to the existing `backend` stream.

### Star feed generation (#148)

Successful publications and thread replies schedule a one-off `GenerateStarFeedPostJob`. Feed reads do so with `feed.open_probability` from the balance file. The existing feed returns saved posts immediately; refresh to see new star posts. The backend uses a non-streaming chat response and the existing 14+ output checks. Player energy, audience and progression are unchanged.

Type-safe `AddAsync<GenerateStarFeedPostJob, StarFeedRequest>` uses `RekiTimeTicker` defaults: three retries at 30, 120 and 600 seconds. These defaults apply to all application TimeTickers; cron maintenance retains its existing configuration. Technical failures propagate to TickerQ; invalid content and missing worlds terminate without retry. A BSON ObjectId string is created at scheduling and stored as a typed PostId in the request alongside typed PlayerId and WorldId. Repeated executions reuse it; the post collection's unique `_id` prevents duplicate posts. Concurrent executions of the same request can still spend AI tokens before one insert wins. Failed TickerQ scheduling results are logged; unexpected scheduler exceptions are not swallowed. ScheduleAsync always schedules; ScheduleFromFeedAsync uses random < feed.open_probability. Neither scheduler method accepts a cancellation token. MongoPostRepository uses a plain InsertPostAsync and insertion exceptions propagate to TickerQ. The AI history identifies authors and marks the selected star's own posts with isOwn. There is no transactional outbox between the business commit and scheduling.

## API clients (#122)

Use the existing local NSwag 14.7.1 tool for two independent clients:

- `backend/src/Reki.Api/nswag.json` generates the ignored C# test client, using request DTOs from `Reki.Api` and response DTOs from `Reki.Application` (`generateDtoTypes=false`). The test project runs it before compilation.
- `frontend/nswag.frontend.json` generates Fetch clients and DTO interfaces in `frontend/src/api/generated/reki-api.ts`. The API project runs it after build-time OpenAPI generation. The TypeScript file is committed; OpenAPI and the C# client remain ignored.

Update workflow: **change API → build API → commit TypeScript client → check frontend**.

```sh
cd backend
dotnet tool restore
dotnet build src/Reki.Api/Reki.Api.csproj --configuration Release
cd ../frontend
npx tsc -b
npm run build
```

No running API, database, Telegram token or LLM credentials are needed for generation. Do not edit generated files by hand. Frontend build, Docker and npm commands consume the committed client without .NET. Backend CI checks the generated TypeScript diff between Build and Test.

SSE uses a binary stream schema so the C# generated client returns an open `FileResponse.Stream`; dispose the FileResponse after reading. Frontend Fetch generation returns a Blob, so its existing incremental SSE adapter retains the single POST and durable recovery behavior. Never automatically repeat mutation POSTs on disconnect.

The active-challenge endpoint returns an empty HTTP 200 body when there is no challenge. Its named schema is nullable; the Fetch client decodes the empty response as null. HTTP behavior and game mechanics are unchanged.

## Persistence

Application messages and handlers are grouped by use case inside their subdomain: `Posts/AddReplyCommand/` contains `AddReply` and `AddReplyCommandHandler`; `Events/GetTodayEvent/` contains `TodayEventQuery` and `TodayEventQueryHandler`. Helpers used by one use case live beside it, including its result models and adapter ports. Helpers shared by several use cases in one subdomain stay at the subdomain root. Shared post AI components live in `Posts/AI`, with safety checks and policies in `Posts/AI/Safety`; ML scoring lives in `Posts/CreatePostCommand`. Request context, transactions, and the shared balance configuration live in `Common`. All custom exception types live in the root `Exceptions` folder of their declaring assembly, for example `Reki.Application/Exceptions` with namespace `Reki.Application.Exceptions`.

Placement follows consumers inside Application only; API error handling, infrastructure jobs, tests, and generated code do not make a type shared. Follow service methods to their use cases when a helper is referenced indirectly. For example, `WorldPostsPage` belongs to `Posts/GetWorldPosts` because that is the only Application consumer of the repository's post-page query. All balance configuration models, including `GameBalance` and its nested configuration types, live in `Common/Balance` with namespace `Reki.Application.Common.Balance`.

Persistence ports live beside their application features. Each Mongo repository handles one entity: player, world (including templates), post, invitation, or daily event. Handlers, request context, and jobs depend directly on the ports they use. Application handlers coordinate post/reply commits and deletion of all player data through `Reki.Application.IUnitOfWork` and these repositories. Infrastructure owns MongoDB sessions and transactions; the singleton `MongoSessionProvider` stores the current session in `AsyncLocal`. Methods used only outside a unit of work call ordinary driver overloads. Transaction-only methods pass the required session directly. Shared `ReplacePlayerAsync`, `ReplaceWorldAsync` and `SaveReplyAsync` reuse one `ReplaceOneAsync` extension; `InsertPostAsync` selects the session overload directly in its single repository implementation. DAL writes are direct calls without Try results, manual exceptions or matched/modified-count checks. Write filters retain identity and ownership, without Version, publication-status or RequestId concurrency conditions; existing Version fields remain stored. Driver failures abort the unit of work. Business validation and sequential replay checks remain in Application, and component tests exercise sequential scenarios. Operations without a result return `Mediator.Unit.Value`. Player deletion reads owned world IDs and deletes related documents and the player in one transaction, sequentially on the same session. Public story world ownership is verified in the application handler before rendering.


## Disabled social features

Direct messages, friends, star conversation memory, and challenges have no Application handlers, repositories, domain implementation, or AI calls. Their controller routes and wire contracts remain: friends return an empty list, active challenge returns null (empty HTTP 200), direct-message reads return an empty DTO, sending returns a null reply and zero replies left, and other writes return empty HTTP 200 without side effects. Standard authentication and request validation remain.

Migration `2.4.5` removes the retired challenge bonus expiry field from player documents while preserving existing energy. Daily reset grants only the base energy for the player's plan. Old social collections are retained for historical migrations and transactional account deletion using raw BSON; deployment does not purge their contents. Reserved balance configuration and historical achievement definitions remain for compatibility, with no active challenge or DM behavior.

## Manual runtime balance update (#184)

After deploying #184 once, a valid replacement of `/app/config/balance.json` is reloaded by the standard .NET JSON configuration provider. `IOptionsMonitor<GameBalance>` supplies a new immutable balance for subsequent operations. Consumers obtain balance through DI and read `CurrentValue` into a method-local variable. Following review, balance is not threaded through helper parameters: a reload between the handler and a helper can therefore make them observe different values. There is no atomic snapshot across the entire confirmation and no cached balance in singleton fields. Each post uses the balance and world difficulty at creation. Existing energy is retained until the next daily reset, which uses the new balance.

Choose the backend container explicitly. Copy its current file for rollback, prepare a complete replacement, validate JSON syntax and review all changed values against the existing balance constraints before applying:

```sh
container='<backend-container>'
docker cp "$container:/app/config/balance.json" balance.previous.json
# Edit a copy named balance.next.json; retain all required sections.
python3 -m json.tool balance.next.json >/dev/null
docker cp balance.next.json "$container:/app/config/balance.next.json"
docker exec --user root "$container" sh -c 'chmod 0444 /app/config/balance.next.json && mv /app/config/balance.next.json /app/config/balance.json'
```

The image runs as `app`; replacing the file can require the explicit root connection above. Wait a few seconds for the standard file-change notification, then verify the changed parameter with the next relevant game operation and check container logs for validation errors. The process and container must remain running. JSON syntax validation alone does not validate game values: an invalid JSON file or balance can cause errors; there is no automatic rollback or last-known-good cache. To roll back, repeat the replacement using `balance.previous.json`.

The edit is lost when the container is recreated. Save the same intended values in `docs/config/balance.json` so the next image preserves them. No volume, management API, custom watcher or delivery automation is introduced. Changing `posts.reset_time_msk` requires a process restart and coordination with the existing TickerQ schedule; hot reload does not rebuild cron schedules.
