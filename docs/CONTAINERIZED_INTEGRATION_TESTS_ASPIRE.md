# Containerized Integration Tests (Aspire)

> **Work in progress:** This document covers the basics. More detailed guidance on writing and extending integration tests will be added in the future.

This project's integration tests boot a real instance of the Aspire AppHost via `DistributedApplicationTestingBuilder`, spinning up actual containers (PostgreSQL) to run tests against real infrastructure — no mocks, no in-memory fakes.

## Prerequisites

A container runtime must be installed and running. See:
- [Linux: Native Docker Engine + Portainer](DOCKER_QUICK_SETUP_LINUX.md)
- [Windows: Podman + Portainer](PODMAN_QUICK_SETUP_WINDOWS.md)

## How it works

Tests use a single shared AppHost instance across the entire test run, managed by `DistributedAppFixture` (which implements `IAsyncLifetime`).

1. **Bootstraps the AppHost:** Calls `DistributedApplicationTestingBuilder.CreateAsync` with `--TestMode=true` to build the app and start the orchestrated containers (PostgreSQL).
2. **Health checks:** Waits for resources to be healthy via `WaitForResourceHealthyAsync` before any test runs.
3. **HTTP clients:** Creates pre-configured `HttpClient` instances bound to each service (`MusicAlbumsApiClient`, `IdentityApiClient`).
4. **Database reset:** Resets the schema between test runs via [Respawner](https://github.com/jbogard/Respawn) — no container restarts needed, just a fast schema wipe.

## Running the tests

From **Visual Studio / Rider**: Open the Test Explorer and run anything under `MusicAlbums.Tests.Integration`.

From the **CLI**:

```bash
dotnet test tests/MusicAlbums.Tests.Integration/MusicAlbums.Tests.Integration.csproj
```

## Further reading

- [Aspire — Testing overview](https://aspire.dev/testing/overview/)
- [Aspire integration testing — best practices for distributed applications](https://antondevtips.com/blog/dotnet-aspire-integration-testing-best-practices-for-distributed-applications)
