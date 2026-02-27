# Music Albums API

[![Build Status](https://dev.azure.com/fernandotonadev/music-albums-api/_apis/build/status%2FMusic%20Albums%20API%20Build%20and%20Deploy?branchName=main)](https://dev.azure.com/fernandotonadev/music-albums-api/_build/latest?definitionId=1&branchName=main)
[![Azure Container Apps](https://img.shields.io/badge/Azure-Container%20Apps-0078D4?logo=microsoft-azure&logoColor=white)](https://music-albums-api.calmbay-fee7a82b.westeurope.azurecontainerapps.io/swagger/index.html)
[![Docker](https://img.shields.io/badge/Docker-Container-2496ED?logo=docker&logoColor=white)](Dockerfile)
[![Bicep](https://img.shields.io/badge/Bicep-IaC-orange?logo=microsoft-azure&logoColor=white)](infra/main.bicep)

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-18-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Swagger](https://img.shields.io/badge/Swagger-OpenAPI-85EA2D?logo=swagger&logoColor=black)](https://music-albums-api.calmbay-fee7a82b.westeurope.azurecontainerapps.io/swagger/index.html)
[![JWT](https://img.shields.io/badge/JWT-Authentication-000000?logo=jsonwebtokens&logoColor=white)](#authentication)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Music Albums REST API written in C# / .NET Core, with Dapper and PostgreSQL

## 🌐 Live Demo

A live version of this API is deployed on Azure Container Apps:

**🔗 [Swagger UI](https://music-albums-api.calmbay-fee7a82b.westeurope.azurecontainerapps.io/swagger/index.html)**

> **Note:** This is a demo instance and may be scaled down to zero when not in use. The first request might take a few seconds while the container starts.

## � Documentation

- [API Testing Guide](docs/API_TESTING_GUIDE.md) - Complete collection of HTTP requests for testing all endpoints

## �🚀 Local Setup

Secrets are stored in **User Secrets** (outside the repo, never committed).

### First Time Setup:

**1. Configure application secrets (User Secrets):**

```bash
# Set your secrets for the main API
cd src/MusicAlbums.Api
dotnet user-secrets set "Database:ConnectionString" "Server=localhost;Port=5433;Database=albums;User ID=dev;Password=yourpass;"
dotnet user-secrets set "Jwt:Key" "your-secret-key-min-32-chars"
dotnet user-secrets set "ApiKey" "your-api-key"

# Set secrets for Identity API
cd ../../tools/Identity.Api
dotnet user-secrets set "Jwt:Secret" "your-secret-key-min-32-chars"
```

**2. Configure database credentials (Docker - Optional):**

Docker Compose uses these defaults: `dev` / `changeme` / `albums` on port `5433`

If you want different values, copy the example and customize:

```bash
cp .env.example .env
# Edit .env with your values
```

**Note:** Don't edit `docker-compose.yml` directly - use `.env` to override defaults.

**Keep ports in sync:** If you change `POSTGRES_PORT` in `.env`, update your connection string to match:

```bash
cd src/MusicAlbums.Api
dotnet user-secrets set "Database:ConnectionString" "Server=localhost;Port=YOUR_NEW_PORT;Database=albums;User ID=dev;Password=changeme;"
```

### Run:

```bash
docker-compose up -d  # Start database
cd src/MusicAlbums.Api
dotnet run
```

## 🔍 View Your Secrets:

```bash
# List all secrets
cd src/MusicAlbums.Api
dotnet user-secrets list

# Open secrets file directly (Windows)
code "$env:APPDATA\Microsoft\UserSecrets\<UserSecretsId>\secrets.json"

# Open secrets file directly (Linux/macOS)
code ~/.microsoft/usersecrets/<UserSecretsId>/secrets.json
```

Find `<UserSecretsId>` in `MusicAlbums.Api.csproj` or `Identity.Api.csproj`.

## ☁️ Cloud Deployment (Azure Container Apps)

### Zero-to-Hero Setup (pipeline-first)

No manual Key Vault bootstrap is required.

Create two Azure DevOps Variable Groups (regular groups, **not linked to Key Vault**):

- `music-albums-dev`
- `music-albums-prod`

In each group, define:

- `RESOURCE_GROUP` (example: `music-albums-dev-rg` / `music-albums-prod-rg`)
- `LOCATION` (optional, defaults to `westeurope`)
- `BASE_NAME` (example: `music-albums`)
- `aspNetCoreEnvironment` (`Development` for dev, `Production` for prod)
- `pg-admin-login` (**secret**)
- `pg-admin-password` (**secret**)
- `jwt-key` (**secret**)
- `api-key` (**secret**)

Then run the pipeline manually and select parameters:

- `targetEnvironment` (`dev` or `prod`)
- `deployInfra` (`false` by default)

Infrastructure deployment runs only when:

1. The pipeline run is **manual**
2. `deployInfra` is set to `true`

Resource names are generated as `<baseName>-<resource>-<suffix>` (for example: `music-albums-api-dev`, `music-albums-api-prod`).

On first run, the pipeline:

1. Ensures the Resource Group exists
2. Deploys `infra/main.bicep`
3. Creates Key Vault and writes seed secrets (`pg-admin-login`, `pg-admin-password`, `jwt-key`, `api-key`)
4. Creates derived secret `db-connection-string`
5. Deploys/updates Container App with the new image

### How it works

```
┌─────────────────────────────────────────────────────────────┐
│  Azure DevOps Pipeline                                       │
│  ├── Reads secret variables (pg/jwt/api)                     │
│  ├── Ensures Resource Group exists                           │
│  └── Deploys Bicep                                            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  music-albums-kv (Key Vault)                                │
│  ├── pg-admin-login      ← BICEP writes                     │
│  ├── pg-admin-password   ← BICEP writes                     │
│  ├── jwt-key             ← BICEP writes                     │
│  ├── api-key             ← BICEP writes                     │
│  └── db-connection-string ← BICEP writes (derived)          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Pipeline deploys: PostgreSQL, Container App, etc.         │
│  Container App reads secrets via Managed Identity          │
│  Images pulled from GitHub Container Registry (GHCR)      │
└─────────────────────────────────────────────────────────────┘
```

### Health Endpoints

- `/_health` - General health status
- `/_health/live` - Liveness probe
- `/_health/ready` - Readiness probe (checks database)

**Build Docker Image:**

```bash
docker build -t music-albums-api .
```
