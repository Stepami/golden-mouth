# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble@sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317 AS restore
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/Reki.Domain/Reki.Domain.csproj src/Reki.Domain/
COPY src/Reki.Application/Reki.Application.csproj src/Reki.Application/
COPY src/Reki.Contracts/Reki.Contracts.csproj src/Reki.Contracts/
COPY src/Reki.Infrastructure/Reki.Infrastructure.csproj src/Reki.Infrastructure/
COPY src/Reki.Api/Reki.Api.csproj src/Reki.Api/
RUN dotnet restore src/Reki.Api/Reki.Api.csproj

FROM restore AS publish
COPY . .
RUN dotnet publish src/Reki.Api/Reki.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false \
    /p:OpenApiGenerateDocuments=false \
    /p:OpenApiGenerateDocumentsOnBuild=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble@sha256:222759b391a1aaf241166672c8f99b2d4ada452e7b5319f3c6e8f265a37b5ad4 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app

ENTRYPOINT ["dotnet", "Reki.Api.dll"]
