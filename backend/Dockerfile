# syntax=docker/dockerfile:1

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /src

COPY TEDF.sln ./
COPY TEDF.API/TEDF.API.csproj TEDF.API/
COPY TEDF.Application/TEDF.Application.csproj TEDF.Application/
COPY TEDF.Domain/TEDF.Domain.csproj TEDF.Domain/
COPY TEDF.Infrastructure/TEDF.Infrastructure.csproj TEDF.Infrastructure/
COPY TEDF.Persistence/TEDF.Persistence.csproj TEDF.Persistence/

RUN dotnet restore TEDF.API/TEDF.API.csproj -a "$TARGETARCH"

COPY . .
WORKDIR /src/TEDF.API
RUN dotnet publish TEDF.API.csproj \
    --configuration Release \
    --output /app/publish \
    -a "$TARGETARCH" \
    --no-restore \
    --self-contained false \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

RUN apk add --no-cache curl icu-libs tzdata \
    && mkdir -p /app/wwwroot/uploads /app/logs \
    && chown -R app:app /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=build --chown=app:app /app/publish .

USER app
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=5 \
    CMD curl -fsS http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "TEDF.API.dll"]
