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

- `GET /api/v1/example/`
- `GET /health/live`
- `GET /health/ready`
- `GET /openapi/v1.json`
- `GET /swagger/`

The exact local address is printed by ASP.NET Core when the service starts.

## Run in Docker

The project-wide Compose file lives in the repository root and starts the frontend, backend, MongoDB, Metabase, and Aspire Dashboard together:

```powershell
cd ..
docker compose up --build
```

The API is available at `http://localhost:8081`, and Aspire Dashboard at `http://localhost:18888`. Set `TELEGRAM_BOT_TOKEN` outside the repository before starting Compose. In Production, Serilog writes every log to the console, while the MongoDB sink stores only explicitly named `Business.*` events and Error/Critical events emitted by `Reki.*` code. Development and tests never write logs to MongoDB. OpenTelemetry sends backend metrics, traces, and all logs to Aspire Dashboard over the internal Compose network. MongoDB log documents expire after 30 days. Set `MONGO_CONNECTION_STRING` outside the repository to override the bundled MongoDB service.

OpenTelemetry is configured through the validated `OpenTelemetry` options section. Set `OpenTelemetry__Enabled=true` and `OpenTelemetry__Endpoint=<absolute OTLP gRPC URI>` in the environment to enable export. Business logs that must be retained in MongoDB use a `[LoggerMessage]` event name beginning with `Business.`; framework and request-pipeline noise remains available in the console and Aspire Dashboard only.

Stop the stack without deleting its volume:

```powershell
docker compose down
```

## Verify

```powershell
dotnet build Reki.slnx --configuration Release
dotnet test --project tests/Reki.Tests/Reki.Tests.csproj --configuration Release --no-build
```

Building the API produces an OpenAPI document. Building the test project regenerates its typed NSwag client from that document before compilation; the client reuses DTOs from `Reki.Contracts` instead of generating copies.
