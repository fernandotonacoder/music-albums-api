# Music Albums API

Music Albums REST API written in C# / .NET Core, with Dapper and PostgreSQL

## 🚀 Local Setup

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

The API is cloud-ready with the following features:

**Health Check Endpoints:**
- `/_health` - General health status
- `/_health/live` - Liveness probe (app is running)
- `/_health/ready` - Readiness probe (database is accessible)

**Database Resilience:**
- Automatic retry on startup (5 attempts with exponential backoff)
- Handles transient database connection failures

**Configuration via Environment Variables:**
```bash
Database__ConnectionString=Host=...;Port=5432;Database=albums;...
Jwt__Key=your-secret-key-min-32-chars
Jwt__Issuer=MusicAlbumsIdentity
Jwt__Audience=MusicAlbumsApi
ApiKey=your-admin-api-key
```

**Build Docker Image:**
```bash
docker build -t music-albums-api .
```

