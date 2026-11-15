# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:11.0@sha256:65f3c7c6a25a239ab3e6719f2896147e7b68242ff2d8818dd70498bde6ef8bea AS restore
WORKDIR /src/backend

COPY backend/Directory.Build.props backend/Directory.Packages.props backend/global.json ./
COPY backend/src/Reki.Domain/Reki.Domain.csproj src/Reki.Domain/
COPY backend/src/Reki.Application/Reki.Application.csproj src/Reki.Application/
COPY backend/src/Reki.Infrastructure/Reki.Infrastructure.csproj src/Reki.Infrastructure/
COPY backend/src/Reki.Api/Reki.Api.csproj src/Reki.Api/
RUN dotnet restore src/Reki.Api/Reki.Api.csproj

FROM restore AS publish
COPY backend/. .
COPY docs/config/balance.json /src/docs/config/balance.json
RUN apt-get update \
    && apt-get install --yes --no-install-recommends fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*
RUN dotnet publish src/Reki.Api/Reki.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false \
    /p:OpenApiGenerateDocuments=false \
    /p:OpenApiGenerateDocumentsOnBuild=false \
    && mkdir --parents /app/publish/fonts \
    && cp /usr/share/fonts/truetype/dejavu/DejaVuSans.ttf /app/publish/fonts/ \
    && cp /usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf /app/publish/fonts/

FROM mcr.microsoft.com/dotnet/aspnet:11.0@sha256:bc0d6653979efba491ab3d6f589d691f96c309c3c3ef81325efa6978e7b7f242 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app

ENTRYPOINT ["dotnet", "Reki.Api.dll"]
