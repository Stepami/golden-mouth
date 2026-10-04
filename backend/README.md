# Reki backend

MVP backend scaffold for the Reki Telegram Mini App. The solution separates API contracts from the controller host, Application, Domain, and Infrastructure projects.

## Run locally

```powershell
dotnet tool restore
dotnet restore Reki.slnx
dotnet run --project src/Reki.Api/Reki.Api.csproj
```

Development mode uses console logging and does not require MongoDB. Try:

- `GET /api/v1/example/`
- `GET /health/live`
- `GET /health/ready`
- `GET /openapi/v1.json`
- `GET /swagger/`

The exact local address is printed by ASP.NET Core when the service starts.

## Run in Docker

Docker Compose deliberately requires the MongoDB connection string to come from the environment rather than source control. For the bundled MongoDB service:

```powershell
$env:MONGO_CONNECTION_STRING = 'mongodb://mongo:27017'
docker compose up --build
```

The API is available at `http://localhost:8080`. Compose sets `ASPNETCORE_ENVIRONMENT=Production`; in Production, Serilog writes structured logs to both the console and MongoDB. MongoDB log documents expire after 30 days.

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
