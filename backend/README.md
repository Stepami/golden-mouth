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
docker compose up --build
```

Enable Docker Model Runner once in Docker Desktop under **Settings → AI** before the first run. Pull and configure the 4-bit main model with its MTP draft once:

```powershell
docker model pull ai/gemma4:e2b-q4_K_M
docker model pull ai/gemma4:e2b-mtp
docker model configure --context-size 4096 --speculative-draft-model ai/gemma4:e2b-mtp --speculative-num-tokens 2 ai/gemma4:e2b-q4_K_M
```

The local `compose.override.yml` passes `ai/gemma4:e2b-q4_K_M` to the backend; the command above atomically configures its 4096-token context and `ai/gemma4:e2b-mtp` draft. The model is intentionally not declared through Compose `models:` because the model provider reconfigures the main model and resets speculative decoding. The override uses Docker Desktop's container endpoint at `http://model-runner.docker.internal/engines/v1/` and a 120-second local inference timeout. No external LLM or `LLM_PAT` is required locally.

The API is available at `http://localhost:8081`, and OpenObserve at `http://localhost:5080`. Set `TG_PAT`, `OPENOBSERVE_EMAIL`, and `OPENOBSERVE_PASSWORD` outside the repository before starting Compose. OpenTelemetry sends logs, metrics, and traces to Collector over the internal Compose network. Product events use the `product` log stream; other application and framework logs use `backend`. Set `MONGO_CONNECTION_STRING` outside the repository to override the bundled MongoDB service.

OTLP export is configured with the standard `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL`, and `OTEL_SERVICE_NAME` variables. MongoDB driver activities and `Microsoft.Extensions.AI` GenAI spans/metrics are enabled; prompts, responses, tool arguments, Mongo query text, tokens, and connection strings are not exported.

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
