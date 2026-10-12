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
creates the ten current game indexes. MongoDB accepts identical existing definitions without rebuilding them, so a
deployed database with player data needs no manual baseline or migration-history entries. Missing indexes are created;
conflicting definitions or duplicate values for a unique index fail startup without changing player documents.

Version `1.1.0` removes `ux_challenges_active_player` only after checking its original unique `PlayerId` definition and
the active-participants replacement. It verifies the old index is absent after removal. An absent legacy index is
accepted; an unexpected definition is preserved and stops startup for investigation.

Index DDL runs outside transactions under the migrator's database lock. Each step can be retried after interruption or
an operator resolves a conflict; successful versions are recorded and skipped on subsequent starts. These migrations
reject rollback because adopted indexes and current player data cannot safely be reverted to the old uniqueness
constraints.

Version `2.1.0` moves referral attribution and bonus usage from player profiles to independent `invites`
documents with a Vogen `InvitationId`, the two player IDs, creation time, and separate usage flags/times.
It creates a unique `InviteeId` index and an `InviterId` lookup index. Registration creates the invitation without
changing the inviter's profile. The invitee's first successful publication marks both bonuses inside the existing
publication transaction; failures and repeated moderation do not consume or duplicate them. Daily post limits read
invitation usage in the current game day and cap inviter credits at the configured maximum.

The migration preserves previously issued inviter credits and used invitee bonuses, removes their old contribution
from `DailyBonus`, and unsets the replaced profile fields. Historical first-post lookup is confined to this migration.
Incomplete legacy attribution stops migration before removing source fields. An already-used invitee bonus without
a surviving post keeps its old daily credit until reset and cannot be granted again. Repeated migration does not
subtract credits twice. Rollback requires restoring a database backup; removed profile state cannot be reconstructed.

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