# Local PostgreSQL (standalone)

## Background

Before this repository adopted Aspire, **this Compose file was how the local PostgreSQL ran for development** — you'd `docker-compose up -d` from the repo root, and the API would connect to it.

After the migration to Aspire, the AppHost manages its own persistent Postgres container (`aspire start` handles everything), and this Compose file became unnecessary for the day-to-day workflow. It was kept here, under `tools/local-postgres/`, for the niche cases where running Postgres without the AppHost is still useful.

## When to use this

You generally don't need it. The Aspire AppHost (`aspire start`) brings up Postgres as a persistent container — including across AppHost stops — so it covers ad-hoc query workflows on its own (connect with psql/pgcli/DBeaver to the port shown in the dashboard).

Reach for this Compose file only when:

- You want a Postgres instance **completely outside** the Aspire toolchain (e.g., a machine where the Aspire CLI isn't installed, or you don't want a dashboard running).
- You want a **separate** Postgres alongside the Aspire-managed one for experimentation. In this case, change `POSTGRES_PORT` in `.env` to something other than 5433 to avoid clashing with Aspire's container.
- A script, CI step, or demo expects the legacy `docker-compose up -d` flow.

⚠️ Both this Compose container and the Aspire-managed container default to host port `5433`. **Don't run both with default settings at the same time** — change `POSTGRES_PORT` in `.env` if you really need both up.

## Usage

From this directory:

```bash
# Optional: override defaults by copying .env.example
cp .env.example .env
# Edit .env to change POSTGRES_USER / POSTGRES_PASSWORD / POSTGRES_PORT / POSTGRES_DB

docker-compose up -d
```

Defaults:

- host: `localhost`
- port: `5433`
- database: `albums`
- user: `dev`
- password: `changeme`

## Stop / clean up

```bash
docker-compose down            # stop, keep volume
docker-compose down -v         # stop and wipe data
```
