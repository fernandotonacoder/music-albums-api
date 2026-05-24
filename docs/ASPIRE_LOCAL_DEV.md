# Aspire Local Development

This repository uses Aspire as the local orchestrator. Postgres is managed by Aspire — there is no separate `docker-compose` step in the daily workflow.

## How the database works

The AppHost (`MusicAlbumsApi.AppHost`) declares a single PostgreSQL resource:

```csharp
var db = builder.AddPostgres("postgres", password: pgPassword, port: 5433)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("musicalbums-postgres-data")
    .AddDatabase("albums");
```

What this gives you:

- **Persistent container** — the Postgres container survives `aspire start` / stop cycles. The next time you `aspire start`, the existing container is reused, not recreated.
- **Persistent volume** — the data lives in a named Docker volume (`musicalbums-postgres-data`), separate from the container. Data survives even if the container is removed.
- **Visible in Docker Desktop** — it's a normal Docker container. You can stop, start, inspect logs, attach `psql`, or use any Postgres client (DBeaver, pgcli, …) on `localhost:5433` even when the AppHost is not running.

In the Aspire dashboard graph you'll see both resources typed:

```
postgres (server)
  └── albums (database)
```

## Start it

```bash
aspire start
```

That's it. On the first run, Aspire creates the container and the volume; on subsequent runs it just reuses them.

## First-time secrets

The AppHost reads sensitive values from its user-secrets store:

```bash
cd MusicAlbumsApi.AppHost
dotnet user-secrets set "jwt-key" "your-super-secret-32-plus-character-jwt-key"
dotnet user-secrets set "api-key" "your-api-key"
dotnet user-secrets set "pg-password" "your-local-postgres-password"
```

The `pg-password` is used by Aspire when it brings up the Postgres container. You only need to set this once — Aspire reuses it across runs.

## Resetting the database

When you want a clean database:

```bash
# Stop the AppHost first (Ctrl+C), then:
docker volume rm musicalbums-postgres-data
```

On the next `aspire start`, Aspire recreates the volume and the schema is re-initialised by `DbInitializer`.

Alternatively, use the Aspire dashboard or Docker Desktop UI to delete the volume.

## Dashboard workflow

When the AppHost is running, open the Aspire dashboard to:

- inspect resource health
- view logs and traces
- restart resources
- stop/start managed resources

## Useful endpoints

- Music Albums API Swagger: `https://localhost:5002/swagger`
- Identity API Swagger: `https://localhost:5004/swagger`
- Music Albums API HTTP endpoint: `http://localhost:5001`
- Identity API HTTP endpoint: `http://localhost:5003`
- PostgreSQL: `localhost:5433` (user `postgres`, password from `pg-password` parameter)

These ports are fixed in the AppHost so they are easy to find and predictable.

## Standalone Postgres (legacy)

The previous `docker-compose.yml`-based Postgres setup lives under [tools/local-postgres/](../tools/local-postgres/) for the rare cases where you want a Postgres instance outside Aspire. The Aspire-managed Postgres uses the same host port (5433) by default, so don't run both at the same time without overriding the port in the Compose `.env`.
