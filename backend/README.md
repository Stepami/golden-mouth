# Reki backend

MVP backend scaffold for the Reki Telegram Mini App. The solution separates API contracts from the controller host, Application, Domain, and Infrastructure projects.

## Run locally

```powershell
cd ..
docker compose up --detach mongo
cd backend
$env:TelegramMiniAppAuthorizationOptions__Token = '<telegram bot token>'
dotnet tool restore
dotnet restore Reki.slnx
dotnet run --project src/Reki.Api/Reki.Api.csproj
```

Development uses MongoDB for game data. Logs stay in the console and are not written to MongoDB outside Production. Protected game endpoints accept Telegram Mini App init data in `Authorization: TMiniApp <initData raw>`; set the bot token outside the repository before starting the API. Try:

- `GET /health/live`
- `GET /health/ready`
- `GET /openapi/v1.json`
- `GET /swagger/`

The exact local address is printed by ASP.NET Core when the service starts.

## Run in Docker

The project-wide Compose file lives in the repository root and starts the frontend, backend, MongoDB, OpenTelemetry Collector, and OpenObserve together:

```powershell
cd ..
$env:OPENOBSERVE_EMAIL = '<chosen local administrator email>'
$env:OPENOBSERVE_PASSWORD = '<chosen local administrator password>'
$env:TG_PAT = '<test Telegram bot token>'
$env:LLM_PAT = '<provider API key>'
$env:LLM_BASE_URL = '<OpenAI-compatible provider URL>'
$env:LLM_MODEL = '<model identifier>'
docker compose up --build
```

Compose passes the required `LLM_PAT`, `LLM_BASE_URL`, and `LLM_MODEL` environment variables only to the backend container. Set `LLM_TIMEOUT_SECONDS` if the provider needs more than the default 20 seconds. Do not commit provider credentials or endpoints.

The API is available at `http://localhost:8081`, and OpenObserve at `http://localhost:5080`. Set `TG_PAT`, the LLM variables, `OPENOBSERVE_EMAIL`, and `OPENOBSERVE_PASSWORD` outside the repository before starting Compose. OpenTelemetry sends logs, metrics, and traces to Collector over the internal Compose network. Product events use the `product` log stream; other application and framework logs use `backend`. Set `MONGO_CONNECTION_STRING` outside the repository to override the bundled MongoDB service.

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

Version `2.1.0` moves referral attribution and bonus usage from player profiles to independent `invites`
documents with a Vogen `InvitationId`, the two player IDs, creation time, and separate usage flags/times.
It creates a unique `InviteeId` index and an `InviterId` lookup index. Registration creates the invitation without
changing the inviter's profile. The invitee's first successful publication marks both bonuses inside the existing
publication transaction; failures and repeated moderation do not consume or duplicate them. Daily post limits read
invitation usage in the current game day and cap inviter credits at the configured maximum.

The migration preserves previously issued inviter credits and used invitee bonuses, removes their old contribution
from `DailyBonus`, and unsets the replaced profile fields. Historical first-post lookup is confined to this migration.
Missing invitation creation time defaults to the player period or Unix epoch. Inconsistent referral counters lose
the credits that cannot be reconstructed. An already-used invitee bonus without
a surviving post keeps its old daily credit until reset and cannot be granted again. Repeated migration does not
subtract credits twice.

Migration `2.4.4` drops the unused `threads` collection, including its documents and indexes. It is safe when the collection is absent or the migration is retried. Older migrations no longer create its index or backfill it. Conversations and replies remain in `posts.Comments`; publication and reply APIs are unchanged. The drop runs outside the migration transaction because MongoDB forbids dropping collections in a transaction. It cannot be rolled back.

## Test infrastructure

API component tests share one `ApiFactory`, WireMock server and MongoDB database per test session. `TestSessionFixture` owns the MongoDB container/client, WireMock and resettable clock; TUnit disposes the dependent API factory before these resources at session end. The host starts lazily for the first API test and applies startup migrations once.

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

Building the API produces an OpenAPI document. Building the test project regenerates its typed NSwag client from that document before compilation; the client reuses DTOs from `Reki.Contracts` instead of generating copies.

## Scheduled maintenance (TickerQ)

TickerQ 10.4 uses `alexjamesbrown.TickerQ.MongoDB` 0.1.0 and the application's existing Mongo client/database. The provider stores schedules and execution history in `ticker_TimeTickers`, `ticker_CronTickers` and `ticker_CronTickerOccurrences` and creates its indexes at startup. Migration `2.3.0` creates the two game-day period indexes; it does not change game documents.

- `ResetGameDayJob`: `0 0 9 * * *`, time zone `Europe/Moscow` (09:00 Moscow, 06:00 UTC).
- `RefreshRegisteredPlayersJob`: `0 * * * * *`, once per minute, using MongoDB `CountDocumentsAsync` without reading profiles, worlds or invitations.

`ResetGameDayJob` and `RefreshRegisteredPlayersJob` implement `ITickerFunction` and are registered with `MapTicker`; TickerQ uses the job class names as function names, and the schedules are unchanged. Both jobs access MongoDB directly. The count job is singleton and owns the registered-player gauge. `GameDayPeriod` calculates the current period from injected `TimeProvider` and `IOptions<GameBalance>`; the reset job receives balance options through DI as well. Startup does not run maintenance jobs. The jobs run on their schedules. Reset only touches older periods. It clears player energy/daily bonus, world wants and daily star relation gains, applies the existing rested-post rule, and increments document versions. Repeated runs do not reset the same period twice. Player/world reads and writes no longer perform daily normalization. Invitation usage, direct messages, daily events and challenge lifecycle retain their existing behavior.

`Jobs__ExecutionEnabled=false` disables scheduler execution (used by component tests), but persisted schedule initialization remains enabled. In production leave execution enabled. A failed maintenance execution is recorded by TickerQ; the next scheduled execution recovers an interrupted reset safely.

TickerQ request serialization uses `ApiJsonSerializerContext` through `WithJsonContext`. Runtime middleware and migrations are skipped during build-time OpenAPI generation; controller/OpenAPI routes are mapped in both modes. TickerQ execution spans (`TickerQ`) join the existing OTLP trace pipeline. The registered-player gauge remains `reki.players.registered` from `Reki.Product`. Exceptions propagate to the scheduler; its instrumentation records execution results and errors. `JobsExceptionHandler` implements `ITickerExceptionHandler` and centrally logs failed attempts and cancellations. Technical reset logs include only player/world counts and the period start, without player IDs or player text, and go to the existing `backend` stream.

### Star feed generation (#148)

Successful publications and thread replies schedule a one-off `GenerateStarFeedPostJob`. Feed reads do so with `feed.open_probability` from the balance file. The existing feed returns saved posts immediately; refresh to see new star posts. The backend uses a non-streaming chat response and the existing 14+ output checks. Player energy, audience and progression are unchanged.

Type-safe `AddAsync<GenerateStarFeedPostJob, StarFeedRequest>` uses `RekiTimeTicker` defaults: three retries at 30, 120 and 600 seconds. These defaults apply to all application TimeTickers; cron maintenance retains its existing configuration. Technical failures propagate to TickerQ; invalid content and missing worlds terminate without retry. A BSON ObjectId string is created at scheduling and stored as a typed PostId in the request alongside typed PlayerId and WorldId. Repeated executions reuse it; the post collection's unique `_id` prevents duplicate posts. Concurrent executions of the same request can still spend AI tokens before one insert wins. Failed TickerQ scheduling results are logged; unexpected scheduler exceptions are not swallowed. ScheduleAsync always schedules; ScheduleFromFeedAsync uses random < feed.open_probability. Neither scheduler method accepts a cancellation token. MongoPostRepository uses a plain InsertPostAsync and insertion exceptions propagate to TickerQ. The AI history identifies authors and marks the selected star's own posts with isOwn. There is no transactional outbox between the business commit and scheduling.

## API clients (#122)

Use the existing local NSwag 14.7.1 tool for two independent clients:

- `backend/src/Reki.Api/nswag.json` generates the ignored C# test client, using DTOs from `Reki.Contracts` (`generateDtoTypes=false`). The test project runs it before compilation.
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

Application messages and handlers are grouped by use case inside their subdomain: `Posts/AddReplyCommand/` contains `AddReply` and `AddReplyCommandHandler`; `Events/GetTodayEvent/` contains `TodayEventQuery` and `TodayEventQueryHandler`. Helpers used by one use case live beside it, including its result models and adapter ports. Helpers shared by several use cases in one subdomain stay at the subdomain root. Cross-subdomain AI generation and safety components, request context, transactions, and the shared balance configuration live in `Common`.

Persistence ports live beside their application features. Each Mongo repository handles one entity: player, world (including templates), post, publication, invitation, star conversation, daily event, or challenge. Handlers, request context, and jobs depend directly on the ports they use. `IGameStore` / `MongoGameStore` retains multi-entity transactions for post/reply commits and deletion of all player data. Player deletion reads owned world IDs and deletes related documents and the player in one transaction; operations run sequentially on the same session. Public story world ownership is verified in the application handler before rendering.
