# Aspire Local Development

This repository uses Aspire as the local orchestrator. Postgres is managed by Aspire — there is no separate `docker-compose` step in the daily workflow.

## Prerequisites

Aspire uses a container runtime to manage PostgreSQL and the integration test containers. You can use whatever you prefer: **Docker** or **Podman**.

> **Tip**: Looking for a lightweight, native installation without the commercial restrictions or overhead of Docker Desktop? 
> - [Linux: Native Docker Engine + Portainer](DOCKER_QUICK_SETUP_LINUX.md)
> - [Windows: Podman + Portainer](PODMAN_QUICK_SETUP_WINDOWS.md)

By default, Aspire assumes Docker. If you choose Podman instead, you just need to tell Aspire to use it:

**Linux/macOS:**
```bash
export ASPIRE_CONTAINER_RUNTIME=podman
```

**Windows:**
```powershell
[Environment]::SetEnvironmentVariable("ASPIRE_CONTAINER_RUNTIME", "podman", "User")
# Close and reopen your terminal, then verify:
$env:ASPIRE_CONTAINER_RUNTIME  # should print: podman
```

## How the database works

The AppHost (`MusicAlbums.AppHost`) declares a single PostgreSQL resource:

```csharp
var db = builder.AddPostgres("musicalbums-postgres", password: pgPassword, port: 5433)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("musicalbums-postgres-data")
    .AddDatabase("albums");
```

What this gives you:

- **Persistent container** — the Postgres container survives `aspire start` / stop cycles. The next time you `aspire start`, the existing container is reused, not recreated.
- **Persistent volume** — the data lives in a named Docker volume (`musicalbums-postgres-data`), separate from the container. Data survives even if the container is removed.
- **Visible in your container UI** — it's a standard container (viewable in the Aspire dashboard, Docker Desktop, Podman Desktop, Portainer, or IDE extensions). You can stop, start, inspect logs, attach `psql`, or use any Postgres client (DBeaver, pgAdmin, pgcli, …) on `localhost:5433` even when the AppHost is not running.

In the Aspire dashboard graph you'll see both resources typed:

```
musicalbums-postgres (server)
  └── albums (database)
```

## Start it

You can either use the CLI or run from your IDE — both do the same thing:

```bash
aspire start
```

Or, in your IDE: open the solution and **F5 / Run** the `MusicAlbums.AppHost` project (set it as the startup project if needed). Supported in:

- **Visual Studio** — native Aspire tooling, no extra setup
- **JetBrains Rider** — native Aspire support since 2024.2
- **VS Code** — install the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension

The IDE flow has the same effect as `aspire start`, plus integrated debugging (breakpoints, watch, step-through) across every service the AppHost orchestrates.

On the first run, Aspire creates the Postgres container and the data volume; on subsequent runs it reuses them.

## First-time secrets

The AppHost reads sensitive values from its user-secrets store:

```bash
cd src/MusicAlbums.AppHost
dotnet user-secrets set "jwt-key" "your-super-secret-32-plus-character-jwt-key"
dotnet user-secrets set "api-key" "your-api-key"
dotnet user-secrets set "pg-password" "your-local-postgres-password"
```

The `pg-password` is used by Aspire when it brings up the Postgres container. You only need to set this once — Aspire reuses it across runs.

### Inspecting the stored secrets

```bash
# List values from the AppHost user-secrets store
cd src/MusicAlbums.AppHost
dotnet user-secrets list

# Open the underlying secrets file directly
# Windows:
code "$env:APPDATA\Microsoft\UserSecrets\<UserSecretsId>\secrets.json"
# Linux/macOS:
code ~/.microsoft/usersecrets/<UserSecretsId>/secrets.json
```

`<UserSecretsId>` is declared in `MusicAlbums.AppHost.csproj`.

## Resetting the database

When you want a clean database:

```bash
# Stop the AppHost first (Ctrl+C), then:
docker volume rm musicalbums-postgres-data   # or: podman volume rm musicalbums-postgres-data
```

On the next `aspire start`, Aspire recreates the volume and the schema is re-initialised by `DbInitializer`.

Alternatively, delete the volume from your preferred container management UI (Docker Desktop, Podman Desktop, Portainer, or IDE extensions) under the **Volumes** section.

## Dashboard workflow

When the AppHost is running, open the Aspire dashboard to:

- inspect resource health
- view logs and traces
- restart resources
- stop/start managed resources

## Useful endpoints

- Music Albums API docs (Scalar): `https://localhost:5002/scalar/v1`
- Identity API docs (Scalar): `https://localhost:5004/scalar/v1`
- Music Albums API HTTP endpoint: `http://localhost:5001`
- Identity API HTTP endpoint: `http://localhost:5003`
- PostgreSQL: `localhost:5433` (user `postgres`, password from `pg-password` parameter)

These ports are fixed in the AppHost so they are easy to find and predictable.

## Observability

Telemetry (traces, metrics, logs) is configured in `MusicAlbums.ServiceDefaults` (`AddServiceDefaults()`) and behaves differently depending on the environment:

| Environment | How it works |
| ----------- | ------------ |
| **Local (Aspire)** | The AppHost sets `OTEL_EXPORTER_OTLP_ENDPOINT` automatically. All OTel data from both the API and the Identity API flows to the **Aspire dashboard** — visible under Traces and Metrics without any extra setup. |
| **Cloud (Azure)** | The infra injects `APPLICATIONINSIGHTS_CONNECTION_STRING`. The app exports via the Azure Monitor OpenTelemetry exporter directly to **Application Insights**. The Identity API is not wired to App Insights (its Bicep does not inject the connection string), since it's a helper tool. |

Both exporters are gated by environment variables — only one is active at a time, so there is no duplication.

Health probe requests to the main API (`/_health/live`, `/_health/ready`) are excluded from traces to avoid polluting Application Insights with noise from Container Apps probes (which run every ~10 seconds). The Identity API does not expose health endpoints — it's a helper tool, not a probed service.

## Standalone Postgres (legacy)

The previous `docker-compose.yml`-based Postgres setup lives under [tools/local-postgres/](../tools/local-postgres/) for the rare cases where you want a Postgres instance outside Aspire. The Aspire-managed Postgres uses the same host port (5433) by default, so don't run both at the same time without overriding the port in the Compose `.env`.
