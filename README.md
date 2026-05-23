# Music Albums API
[![Build Status](https://dev.azure.com/fernandotonadev/music-albums-api/_apis/build/status%2FMusic%20Albums%20API%20Build%20and%20Deploy?branchName=main)](https://dev.azure.com/fernandotonadev/music-albums-api/_build/latest?definitionId=1&branchName=main)
[![Azure Container Apps](https://img.shields.io/badge/Azure-Container%20Apps-0078D4?logo=microsoft-azure&logoColor=white)](https://music-albums-api.calmbay-fee7a82b.westeurope.azurecontainerapps.io/swagger/index.html)
[![Docker](https://img.shields.io/badge/Docker-Container-2496ED?logo=docker&logoColor=white)](Dockerfile)
[![Bicep](https://img.shields.io/badge/Bicep-IaC-orange?logo=microsoft-azure&logoColor=white)](infra/main/main.bicep)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-18-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Swagger](https://img.shields.io/badge/Swagger-OpenAPI-85EA2D?logo=swagger&logoColor=black)](https://music-albums-api.calmbay-fee7a82b.westeurope.azurecontainerapps.io/swagger/index.html)
[![JWT](https://img.shields.io/badge/JWT-Authentication-000000?logo=jsonwebtokens&logoColor=white)](#authentication)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
Music Albums REST API written in C# / .NET, using Dapper, PostgreSQL, and Aspire for local orchestration.
## 🌐 Live Demo
**🔗 [Swagger UI](https://music-albums-05-api-dev.orangeforest-b7d25f37.swedencentral.azurecontainerapps.io/swagger/index.html)**
> This demo may scale to zero when idle. The first request can take a few seconds.
## 📚 Documentation
- [Aspire Local Dev](docs/ASPIRE_LOCAL_DEV.md) - database modes, dashboard workflow, and startup commands
- [API Testing Guide](docs/API_TESTING_GUIDE.md) - copy-pastable requests for all endpoints
- [Infrastructure](docs/INFRASTRUCTURE.md) - Bicep modules and Azure deployment
- [Identity API](docs/IDENTITY_API.md) - JWT token generator (helper tool)
## 🚀 Local Development
Aspire is the recommended local orchestrator.
### Choose your database mode
- **Persisted**: `docker-compose up -d` + `aspire start`
- **Disposable**: `UseManagedPostgres=true aspire start`
See [Aspire Local Dev](docs/ASPIRE_LOCAL_DEV.md) for the full workflow.
### First-time secrets (AppHost)
```bash
cd MusicAlbumsApi.AppHost
dotnet user-secrets set "jwt-key" "your-secret-key-min-32-chars"
dotnet user-secrets set "api-key" "your-api-key"
dotnet user-secrets set "ConnectionStrings:albums" "Server=localhost;Port=5433;Database=albums;User ID=dev;Password=changeme;"
```
If you change the Docker Compose port or credentials, update `ConnectionStrings:albums` in the AppHost user-secrets so it matches. The compose defaults are in `.env.example` — copy to `.env` and edit if you need to override.
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
