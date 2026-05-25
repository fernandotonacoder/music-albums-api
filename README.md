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

This project is a **monolith** with a pragmatic **Layered Architecture**, organized by technical concerns:

- **`MusicAlbums.Api`** (Presentation): MVC Controllers, auth handlers, request/response mapping, health checks, and Swagger configuration.
- **`MusicAlbums.Application`** (Business & Data): Core business logic (`Services`), data access (`Repositories` & `Database`), domain models, and input validation (`Validators`).
- **`MusicAlbums.Contracts`** (HTTP Contracts): Request and Response DTOs that define the API's public interface.
- **`MusicAlbumsApi.ServiceDefaults`** (Shared Infrastructure): Cross-cutting runtime concerns — OpenTelemetry instrumentation, service discovery, HTTP client resilience. Referenced by both the API and the Identity API via `builder.AddServiceDefaults()`, and **runs in both local and cloud** — only the telemetry exporter changes (OTLP to the Aspire dashboard locally, Azure Monitor to Application Insights in production).

One additional project handles local orchestration only:

- **`MusicAlbumsApi.AppHost`** — [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) orchestrator. Declares the local dev topology (PostgreSQL, the API, the Identity API helper) and is invoked by `aspire start`. **Local development only** — not built into the Docker image, not deployed to the cloud.

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

Aspire is the local orchestrator. It brings up the API, the Identity API helper, and a persistent PostgreSQL container in one go.

**From the terminal:**

```bash
aspire start
```

**From your IDE:** F5 / Run the `MusicAlbumsApi.AppHost` project. Works in **Visual Studio**, **Rider**, and **VS Code** (with the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension) — same result as the CLI, plus integrated breakpoints across every service the AppHost orchestrates.

See [Aspire Local Dev](docs/ASPIRE_LOCAL_DEV.md) for first-time setup (user-secrets for the AppHost), the daily workflow (data persistence, reset, endpoints), and the observability story.

### Local orchestration (Aspire Resources Graph)

![Aspire Resources Graph](docs/images/aspire-resources-graph.png)

## ☁️ Cloud Deployment (Azure Container Apps)

| Dev | Prod |
|-----|------|
| ![Azure Resource Group — Dev](docs/images/azure-music-albums-rg-dev.png) | ![Azure Resource Group — Prod](docs/images/azure-music-albums-rg-prod.png) |

Create two Azure DevOps variable groups (`music-albums-dev` / `music-albums-prod`) with the required variables, then queue `.azure-pipelines/main-ci-cd.yml` with:

| Parameter | Default | Description |
|-----------|---------|-------------|
| `targetEnvironment` | `dev` | Target environment (`dev` or `prod`) |
| `deployInfra` | `false` | Deploy or update infrastructure via Bicep |
| `destroyInfra` | `false` | Delete the entire resource group (manual only) |

The `destroyInfra` flag tears down all resources in the resource group — useful for cost savings when the environment is no longer needed. Re-deploy from scratch with `deployInfra=true`.

### Identity API (optional helper)

The [Identity API](docs/IDENTITY_API.md) is a JWT token generator for testing. It is deployed into the **same resource group** as the main API and shares its Container Apps Environment. Its infrastructure is managed separately via `.azure-pipelines/optional-identity-api.yml`:

| Parameter | Default | Description |
|-----------|---------|-------------|
| `deployInfra` | `false` | Deploy the Identity API Container App |
| `destroyInfra` | `false` | Delete the Identity API Container App (manual only) |

Deploy it when you need remote testing; destroy it when done to avoid unnecessary costs.

See [Infrastructure Guide](docs/INFRASTRUCTURE.md) for the full deployment model, variable groups, dev vs prod differences, and pipelines.

## 🩺 Health endpoints

- `/_health` - general health status
- `/_health/live` - liveness probe
- `/_health/ready` - readiness probe (checks database)
