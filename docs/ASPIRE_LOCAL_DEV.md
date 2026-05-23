# Aspire Local Development

This repository now uses Aspire as the default local orchestrator.

## Local database modes

| Mode | Start command | Best for | Dashboard hint |
| ---- | ------------- | -------- | -------------- |
| Persisted | `docker-compose up -d` + `aspire start` | Daily development, debugging, ad-hoc SQL | `db-mode = Persisted docker-compose Postgres` |
| Ephemeral | `UseManagedPostgres=true aspire start` | Quick validation, clean runs, other machines | `db-mode = Aspire-managed ephemeral Postgres` |

### IDE launch profiles

In Rider / Visual Studio you can pick these AppHost launch profiles directly:

- `Aspire - Persisted DB`
- `Aspire - Ephemeral DB`

The existing `https` / `http` profiles still work, but the explicit names are easier to choose when switching modes.

### 1) Persisted database for daily development

Use your existing `docker-compose.yml` PostgreSQL container when you want:

- stable data across restarts
- manual debugging in Docker Desktop
- ad-hoc SQL queries without the AppHost running

**Start it:**

```bash
docker-compose up -d
aspire start
```

Or run the AppHost explicitly with the persisted profile:

```bash
cd MusicAlbumsApi.AppHost
dotnet run --launch-profile "Aspire - Persisted DB"
```

The AppHost reads the persisted connection string from its own configuration:

- `ConnectionStrings:albums` in `MusicAlbumsApi.AppHost` user-secrets

Set this once in the AppHost user-secrets to point at the local Docker Compose database:

- host: `localhost`
- port: `5433`
- database: `albums`
- user: `dev`
- password: `changeme`

If you change the compose port or credentials, update the AppHost user-secrets to match:

```bash
cd MusicAlbumsApi.AppHost
dotnet user-secrets set "ConnectionStrings:albums" "Server=localhost;Port=5433;Database=albums;User ID=dev;Password=changeme;"
```

### 2) Aspire-managed ephemeral database

Use this mode when you want a disposable database for a quick checkup or a clean test run.

**Start it:**

```bash
UseManagedPostgres=true aspire start
```

Or run the AppHost explicitly with the ephemeral profile:

```bash
cd MusicAlbumsApi.AppHost
dotnet run --launch-profile "Aspire - Ephemeral DB"
```

Or pick the `managed-db` launch profile in Rider/Visual Studio.

In this mode Aspire starts its own PostgreSQL container and wires the Music Albums API to it automatically. When you stop the AppHost, the container is removed with the rest of the managed resources.

The dashboard also shows a small `db-mode` resource so you can tell at a glance which mode the AppHost is using.

## Dashboard workflow

When the AppHost is running, open the Aspire dashboard to:

- inspect resource health
- view logs and traces
- restart resources
- stop/start managed resources

For the managed PostgreSQL resource, the dashboard is the easiest place to restart or stop the container when you want a fresh database.

## Secrets

The AppHost uses user-secrets for sensitive values such as:

- `jwt-key`
- `api-key`
- `ConnectionStrings:albums`

Set them once in `MusicAlbumsApi.AppHost`:

```bash
cd MusicAlbumsApi.AppHost
dotnet user-secrets set "jwt-key" "your-super-secret-32-plus-character-jwt-key"
dotnet user-secrets set "api-key" "your-api-key"
dotnet user-secrets set "ConnectionStrings:albums" "Server=localhost;Port=5433;Database=albums;User ID=dev;Password=changeme;"
```

## Useful endpoints in Aspire mode

- Music Albums API Swagger: `https://localhost:5002/swagger`
- Identity API Swagger: `https://localhost:5004/swagger`
- Music Albums API HTTP endpoint: `http://localhost:5001`
- Identity API HTTP endpoint: `http://localhost:5003`

These ports are set in the AppHost so they are easy to find in the dashboard and predictable in the IDE.

## Recommended workflow

- **Persistent dev/debugging**: `docker-compose up -d` + `aspire start`
- **Disposable test run**: `UseManagedPostgres=true aspire start`
- **Manual SQL investigation**: keep using the Docker Desktop container from `docker-compose`


