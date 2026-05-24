# Music Albums API

[![Build Status](https://dev.azure.com/fernandotonadev/music-albums-api/_apis/build/status%2FMusic%20Albums%20API%20Build%20and%20Deploy?branchName=main)](https://dev.azure.com/fernandotonadev/music-albums-api/_build/latest?definitionId=1&branchName=main)
[![Azure Container Apps](https://img.shields.io/badge/Azure-Container%20Apps-0078D4?logo=microsoft-azure&logoColor=white)](https://music-albums-07-api-dev.redflower-b6906ccc.swedencentral.azurecontainerapps.io/swagger/index.html)
[![Docker](https://img.shields.io/badge/Docker-Container-2496ED?logo=docker&logoColor=white)](Dockerfile)
[![Bicep](https://img.shields.io/badge/Bicep-IaC-orange?logo=microsoft-azure&logoColor=white)](infra/main/main.bicep)

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Aspire](https://img.shields.io/badge/Aspire-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/aspire/)
[![Dapper](https://img.shields.io/badge/Dapper-Micro%20ORM-2496ED)](https://github.com/DapperLib/Dapper)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-18-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![OpenTelemetry](https://img.shields.io/badge/OpenTelemetry-observability-F5A800?logo=opentelemetry&logoColor=white)](https://opentelemetry.io/)
[![Swagger](https://img.shields.io/badge/Swagger-OpenAPI-85EA2D?logo=swagger&logoColor=black)](https://music-albums-07-api-dev.redflower-b6906ccc.swedencentral.azurecontainerapps.io/swagger/index.html)
[![JWT](https://img.shields.io/badge/JWT-Authentication-000000?logo=jsonwebtokens&logoColor=white)](docs/IDENTITY_API.md)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Music Albums REST API written in C# / .NET, using Dapper, PostgreSQL, and Aspire for local orchestration.

## 🌐 Live Demo

- **🔗 [Music Albums API — Swagger](https://music-albums-07-api-dev.redflower-b6906ccc.swedencentral.azurecontainerapps.io/swagger/index.html)**
- **🔗 [Identity API — Swagger](https://id-api-music-albums-07-dev.redflower-b6906ccc.swedencentral.azurecontainerapps.io/swagger/index.html)** (helper for generating JWTs)

> Development environment. Demo may scale to zero when idle — the first request can take a few seconds.

## 📚 Documentation

- [Aspire Local Dev](docs/ASPIRE_LOCAL_DEV.md) - database, dashboard workflow, and startup commands
- [API Testing Guide](docs/API_TESTING_GUIDE.md) - copy-pastable requests for all endpoints
- [Infrastructure](docs/INFRASTRUCTURE.md) - Bicep modules and Azure deployment
- [Identity API](docs/IDENTITY_API.md) - JWT token generator (helper tool)
- [Standalone Postgres](tools/local-postgres/README.md) - legacy `docker-compose` Postgres, kept for non-Aspire workflows

## 🚀 Local Development

Aspire is the local orchestrator. It brings up the API, the Identity API helper, and a persistent PostgreSQL container in one command:

```bash
aspire start
```

See [Aspire Local Dev](docs/ASPIRE_LOCAL_DEV.md) for the full workflow (data persistence, reset, endpoints).

### Architecture

![Aspire Resources Graph](docs/images/aspire-resources-graph.png)

### First-time secrets (AppHost)

```bash
cd MusicAlbumsApi.AppHost
dotnet user-secrets set "jwt-key" "your-secret-key-min-32-chars"
dotnet user-secrets set "api-key" "your-api-key"
dotnet user-secrets set "pg-password" "your-local-postgres-password"
```

The `pg-password` is what Aspire uses to bring up the local Postgres container; set it once and Aspire reuses it across runs.

### View your secrets

```bash
# List AppHost secrets
cd MusicAlbumsApi.AppHost
dotnet user-secrets list

# Open the secrets file directly (Windows)
code "$env:APPDATA\Microsoft\UserSecrets\<UserSecretsId>\secrets.json"

# Open the secrets file directly (Linux/macOS)
code ~/.microsoft/usersecrets/<UserSecretsId>/secrets.json
```

Find `<UserSecretsId>` in `MusicAlbumsApi.AppHost.csproj`.

## ☁️ Cloud Deployment (Azure Container Apps)

Create two Azure DevOps variable groups (`music-albums-dev` / `music-albums-prod`) with the required variables, then queue `.azure-pipelines/main-ci-cd.yml` with:

- `targetEnvironment`: `dev` or `prod`
- `deployInfra`: `false` by default (set to `true` to deploy/update infrastructure)

See [Infrastructure Guide](docs/INFRASTRUCTURE.md) for the full deployment model, variable groups, dev vs prod differences, and pipelines.

## 🩺 Health endpoints

- `/_health` - general health status
- `/_health/live` - liveness probe
- `/_health/ready` - readiness probe (checks database)

## 🐳 Build the Docker image

```bash
docker build -t music-albums-api .
```
